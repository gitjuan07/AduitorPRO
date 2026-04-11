using AuditorPRO.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditorPRO.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "AuditorPRO.Admin,AuditorPRO.Auditor,AuditorPRO.Gerente")]
public class ExportarController : ControllerBase
{
    private readonly IDocumentGeneratorService _docGen;
    private readonly ISimulacionRepository _simRepo;

    public ExportarController(IDocumentGeneratorService docGen, ISimulacionRepository simRepo)
    { _docGen = docGen; _simRepo = simRepo; }

    [HttpGet("simulacion/{id:guid}/word")]
    public async Task<IActionResult> ExportarWord(Guid id, CancellationToken ct)
    {
        var sim = await _simRepo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Simulación {id} no encontrada.");

        var bytes = await _docGen.GenerateWordReportAsync(id, ct);
        var nombre = $"Informe_Auditoria_{sim.Nombre.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.docx";

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            nombre);
    }

    [HttpGet("simulacion/{id:guid}/ppt")]
    public async Task<IActionResult> ExportarPpt(Guid id, CancellationToken ct)
    {
        var sim = await _simRepo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Simulación {id} no encontrada.");

        var bytes = await _docGen.GeneratePptSummaryAsync(id, ct);
        var nombre = $"Presentacion_{sim.Nombre.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pptx";

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            nombre);
    }
}
