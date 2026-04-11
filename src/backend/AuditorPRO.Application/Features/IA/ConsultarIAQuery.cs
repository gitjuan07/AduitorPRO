using AuditorPRO.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AuditorPRO.Application.Features.IA;

public record ConsultarIAQuery(string Pregunta, string? ContextoAdicional = null) : IRequest<IAResponseDto>;

public class ConsultarIAValidator : AbstractValidator<ConsultarIAQuery>
{
    public ConsultarIAValidator()
    {
        RuleFor(x => x.Pregunta).NotEmpty().MaximumLength(2000);
    }
}

public class IAResponseDto
{
    public string Respuesta { get; set; } = string.Empty;
    public string? FuentesConsultadas { get; set; }
    public bool UsoBaseConocimiento { get; set; }
    public DateTime GeneradoAt { get; set; } = DateTime.UtcNow;
}

public class ConsultarIAHandler : IRequestHandler<ConsultarIAQuery, IAResponseDto>
{
    private readonly IAzureOpenAIService _iaService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLoggerService _auditLogger;
    private readonly IIngestorDocumentosService _ingestor;

    private const string ContextoOrganizacional = """
        Eres el Agente Auditor Preventivo de AuditorPRO TI para ILG Logistics.
        Tu rol es asistir a auditores internos y administradores de TI a identificar debilidades de control,
        evaluar riesgos de segregación de funciones, revisar políticas de seguridad y sugerir planes de acción.
        Baséate en marcos normativos: ISO 27001, COBIT 2019, COSO, SOX, NIST CSF.
        Responde siempre en español, con base en evidencia y fundamento normativo.
        Cuando tengas documentos de la base de conocimiento, cítalos explícitamente.
        """;

    public ConsultarIAHandler(
        IAzureOpenAIService iaService,
        ICurrentUserService currentUser,
        IAuditLoggerService auditLogger,
        IIngestorDocumentosService ingestor)
    {
        _iaService = iaService;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _ingestor = ingestor;
    }

    public async Task<IAResponseDto> Handle(ConsultarIAQuery request, CancellationToken cancellationToken)
    {
        // 1. Buscar contexto relevante en la base de conocimiento (RAG)
        var docsRelevantes = await _ingestor.BuscarAsync(request.Pregunta, topK: 4, cancellationToken);
        var hayContextoRAG = docsRelevantes.Count > 0;

        var contextoRAG = string.Empty;
        var fuentes = new List<string>();
        if (hayContextoRAG)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n=== DOCUMENTOS DE AUDITORÍA RELEVANTES (Base de Conocimiento) ===");
            foreach (var doc in docsRelevantes)
            {
                var fragmento = doc.TextoCompleto.Length > 800
                    ? doc.TextoCompleto[..800] + "..."
                    : doc.TextoCompleto;
                sb.AppendLine($"\n[Fuente: {doc.NombreArchivo} | Dominio: {doc.DominioDetectado ?? "General"}]");
                sb.AppendLine(fragmento);
                fuentes.Add(doc.NombreArchivo);
            }
            sb.AppendLine("=== FIN DOCUMENTOS ===\n");
            sb.AppendLine("Usa los documentos anteriores como evidencia real al responder.");
            contextoRAG = sb.ToString();
        }

        // 2. Construir prompt enriquecido
        var contextoCompleto = ContextoOrganizacional + contextoRAG;
        if (!string.IsNullOrWhiteSpace(request.ContextoAdicional))
            contextoCompleto += $"\n\nContexto adicional:\n{request.ContextoAdicional}";

        // 3. Llamar al modelo
        var respuesta = await _iaService.ConsultarAsync(request.Pregunta, contextoCompleto, cancellationToken);

        await _auditLogger.LogAsync(_currentUser.UserId, _currentUser.Email,
            "CONSULTA_IA", "AgentIA", null, ct: cancellationToken);

        return new IAResponseDto
        {
            Respuesta = respuesta,
            FuentesConsultadas = fuentes.Count > 0
                ? string.Join(", ", fuentes)
                : null,
            UsoBaseConocimiento = hayContextoRAG
        };
    }
}
