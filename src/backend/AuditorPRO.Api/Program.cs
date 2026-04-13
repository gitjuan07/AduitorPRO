using AuditorPRO.Application.DependencyInjection;
using AuditorPRO.Domain.Interfaces;
using AuditorPRO.Api.Middleware;
using AuditorPRO.Api.Services;
using AuditorPRO.Infrastructure;
using AuditorPRO.Infrastructure.Persistence;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Key Vault — carga secretos en producción usando Managed Identity
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri) && !builder.Environment.IsDevelopment())
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

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

    // Aceptar tokens v1 (aud=clientId) y v2 (aud=api://clientId) — ambos formatos válidos
    var clientId = builder.Configuration["AzureAd:ClientId"] ?? "";
    builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        opts.TokenValidationParameters.ValidAudiences = new[]
        {
            clientId,
            $"api://{clientId}",
        };
        // No requerir el claim 'roles' — permite cualquier usuario autenticado del tenant
        opts.TokenValidationParameters.RoleClaimType = "roles";
    });
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

// CORS — AllowedOrigins puede ser una lista separada por espacios o comas
var allowedOrigins = (builder.Configuration["AllowedOrigins"] ?? "https://localhost:5173")
    .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
builder.Services.AddCors(opts => opts.AddPolicy("FrontendPolicy", policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Aumentar límites para cargas masivas SAP (archivos >5000 filas pueden superar defaults)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit  = 52_428_800; // 50 MB
    opts.ValueLengthLimit          = 52_428_800;
    opts.MultipartHeadersLengthLimit = 131_072;  // 128 KB
});
builder.WebHost.ConfigureKestrel(k =>
{
    k.Limits.MaxRequestBodySize = 52_428_800; // 50 MB
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AuditorPRO TI API", Version = "v1" });
});

// Health checks — SQL Server agregado como degraded (no falla startup si BD no está lista)
var healthConnStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var hcBuilder = builder.Services.AddHealthChecks();
if (!healthConnStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
    hcBuilder.AddSqlServer(healthConnStr, name: "sql",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

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
// Producción: las migraciones se aplican vía script SQL en Azure Portal
// (ver infra/scripts/sql/02-migrations-initial.sql)

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
