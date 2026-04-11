using AuditorPRO.Application.DependencyInjection;
using AuditorPRO.Domain.Interfaces;
using AuditorPRO.Api.Middleware;
using AuditorPRO.Api.Services;
using AuditorPRO.Infrastructure;
using AuditorPRO.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Auth — bypass in dev, Entra ID in production
var bypassAuth = builder.Configuration.GetValue<bool>("DevMode:BypassAuth");
if (bypassAuth && builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("DevBypass")
        .AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>("DevBypass", _ => { });
    builder.Services.AddAuthorization(opts =>
        opts.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("DevBypass")
            .RequireAuthenticatedUser().Build());
}
else
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");
}

// Application layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Current user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("per_user", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});

// CORS
builder.Services.AddCors(opts => opts.AddPolicy("FrontendPolicy", policy =>
    policy.WithOrigins(
            builder.Configuration["AllowedOrigins"] ?? "https://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AuditorPRO TI API", Version = "v1" });
});

// Health checks — SQL Server only in non-SQLite environments
var healthConnStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var hcBuilder = builder.Services.AddHealthChecks();
if (!healthConnStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
    hcBuilder.AddSqlServer(healthConnStr, name: "sql");

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<ActiveUserMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Auto-migrate/create + seed in development
    if (builder.Configuration.GetValue<bool>("DevMode:AutoMigrate"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // SQLite (local dev): EnsureCreated — no migration tracking needed
        // SQL Server (staging/prod): MigrateAsync — apply pending migrations
        var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
        if (connStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();

        if (builder.Configuration.GetValue<bool>("DevMode:SeedData"))
        {
            await AuditorPRO.Infrastructure.Persistence.SeedData.SeedAsync(db);
        }
    }
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
