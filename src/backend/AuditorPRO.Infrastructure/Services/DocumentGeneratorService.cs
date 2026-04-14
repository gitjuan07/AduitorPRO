using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using AuditorPRO.Domain.Interfaces;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;

namespace AuditorPRO.Infrastructure.Services;

public class DocumentGeneratorService : IDocumentGeneratorService
{
    private readonly ISimulacionRepository _simulacionRepo;
    private readonly IHallazgoRepository   _hallazgoRepo;
    private readonly ILogger<DocumentGeneratorService> _logger;

    // ── Colores corporativos ─────────────────────────────────────────────────
    private const string AzulOscuro  = "1F3864";
    private const string AzulMedio   = "2E75B6";
    private const string AzulClaro   = "D6E4F0";
    private const string GrisClaro   = "F2F2F2";
    private const string RojoRiesgo  = "C00000";
    private const string Amarillo    = "FFB900";
    private const string Verde       = "375623";
    private const string Blanco      = "FFFFFF";

    // ── Reglas del motor ────────────────────────────────────────────────────
    private static readonly (string Key, string Titulo, string Descripcion, string Riesgo, string Referencia)[] Reglas =
    [
        ("R01_SOD", "R01 – Segregación de Funciones (SoD)",
         "Identifica usuarios SAP con roles que combinan funciones incompatibles (p.ej. crear y aprobar pagos).",
         "Fraude financiero, alteración de registros, omisión de controles internos.",
         "COSO II – Actividades de Control; ISO 27001:2022 A.5.15"),

        ("R02_ACCESO_EX_EMPLEADO", "R02 – Accesos de Ex-Empleados Activos",
         "Detecta cuentas SAP activas correspondientes a personas que ya no pertenecen a la organización según Nómina.",
         "Acceso no autorizado, fuga de información, incumplimiento de política de bajas.",
         "ISO 27001:2022 A.5.18; SOX Sección 404"),

        ("R03_ROL_NO_AUTORIZADO_MATRIZ", "R03 – Roles Fuera de Matriz de Puestos",
         "Contrasta cada rol SAP asignado contra la Matriz de Puestos aprobada. Los roles no contemplados en la matriz o sin caso SE Suite vigente se marcan como hallazgo.",
         "Privilegio excesivo, violación de principio de mínimo privilegio.",
         "ISO 27001:2022 A.5.15; COBIT 5 – APO13"),

        ("R04_USUARIO_SIN_ENTRA_ID", "R04 – Usuarios SAP sin Cuenta Entra ID",
         "Usuarios SAP activos que no tienen cuenta corporativa Microsoft Entra ID (Azure AD) activa asociada.",
         "Imposibilidad de aplicar MFA y políticas de acceso condicional.",
         "ISO 27001:2022 A.5.17; NIST SP 800-53 IA-2"),

        ("R05_ROL_SIN_TRANSACCIONES", "R05 – Roles sin Uso de Transacciones",
         "Roles asignados a usuarios que no registran ejecución de ninguna transacción SAP en el período auditado.",
         "Superficie de ataque innecesaria; incumplimiento del principio de mínimo privilegio.",
         "ISO 27001:2022 A.8.2; CIS Control 6"),

        // aliases sin prefijo R0x
        ("SOD",                    "Segregación de Funciones (SoD)",                 "", "", ""),
        ("ACCESO_EX_EMPLEADO",     "Accesos de Ex-Empleados Activos",                "", "", ""),
        ("ROL_NO_AUTORIZADO_MATRIZ","Roles Fuera de Matriz de Puestos",              "", "", ""),
        ("USUARIO_SIN_ENTRA_ID",   "Usuarios SAP sin Cuenta Entra ID",               "", "", ""),
        ("ROL_SIN_TRANSACCIONES",  "Roles sin Uso de Transacciones",                 "", "", ""),
    ];

    private static (string Titulo, string Descripcion, string Riesgo, string Referencia) GetRegla(string key)
    {
        var r = Reglas.FirstOrDefault(x => x.Key == key);
        return r == default
            ? (key, "Regla personalizada.", "", "")
            : (r.Titulo, r.Descripcion, r.Riesgo, r.Referencia);
    }

    public DocumentGeneratorService(
        ISimulacionRepository simulacionRepo,
        IHallazgoRepository   hallazgoRepo,
        ILogger<DocumentGeneratorService> logger)
    {
        _simulacionRepo = simulacionRepo;
        _hallazgoRepo   = hallazgoRepo;
        _logger         = logger;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // WORD — Informe profesional de auditoría
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<byte[]> GenerateWordReportAsync(Guid simulacionId, CancellationToken ct = default)
    {
        var sim = await _simulacionRepo.GetWithResultadosAsync(simulacionId, ct)
            ?? throw new KeyNotFoundException($"Simulación {simulacionId} no encontrada.");

        var hallazgos = (await _hallazgoRepo.GetBySimulacionAsync(simulacionId, ct)).ToList();

        var totalH   = hallazgos.Count;
        var criticos = hallazgos.Count(h => h.Criticidad == Criticidad.CRITICA);
        var medios   = hallazgos.Count(h => h.Criticidad == Criticidad.MEDIA);
        var bajos    = hallazgos.Count(h => h.Criticidad == Criticidad.BAJA);

        var porRegla = hallazgos
            .GroupBy(h => h.TipoHallazgo ?? "SIN_CLASIFICAR")
            .OrderByDescending(g => g.Count())
            .ToList();

        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Estilos y configuración de página
            var stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylePart.Styles = BuildWordStyles();
            AddHeaderFooter(mainPart, sim.Nombre);

            // ── PORTADA ───────────────────────────────────────────────────────
            WordBlankLine(body, 4);
            WordPara(body, "CONFIDENCIAL", null, centered: true, color: RojoRiesgo, bold: true, size: 18);
            WordBlankLine(body);
            WordPara(body, "ILG LOGISTICS", null, centered: true, color: AzulOscuro, bold: true, size: 28);
            WordBlankLine(body);
            WordPara(body, "INFORME DE AUDITORÍA DE CONTROLES DE ACCESO SAP", "Title", centered: true);
            WordBlankLine(body);
            WordPara(body, sim.Nombre, "Subtitle", centered: true);
            WordBlankLine(body, 3);

            // Tabla de portada
            var tPortada = body.AppendChild(new Table());
            tPortada.AppendChild(TableProps(centered: true));
            PortadaFila(tPortada, "Período auditado",   $"{sim.PeriodoInicio:dd/MM/yyyy} — {sim.PeriodoFin:dd/MM/yyyy}");
            PortadaFila(tPortada, "Fecha del informe",  DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-CR")));
            PortadaFila(tPortada, "Generado por",       "Sistema AuditorPRO TI — ILG Logistics");
            PortadaFila(tPortada, "Estado simulación",  sim.Estado.ToString());
            if (sim.CompletadaAt.HasValue)
                PortadaFila(tPortada, "Completado el", sim.CompletadaAt.Value.ToString("dd/MM/yyyy HH:mm"));

            WordPageBreak(body);

            // ── 1. INTRODUCCIÓN Y ALCANCE ─────────────────────────────────────
            WordPara(body, "1. Introducción y Alcance", "Heading1");
            WordPara(body, $"El presente informe documenta los resultados de la auditoría de controles de acceso al sistema SAP ejecutada mediante el Motor de Control Cruzado de la plataforma AuditorPRO TI. La auditoría abarcó el período comprendido entre el {sim.PeriodoInicio:dd/MM/yyyy} y el {sim.PeriodoFin:dd/MM/yyyy}, y fue completada el {sim.CompletadaAt?.ToString("dd/MM/yyyy") ?? "N/A"}.");
            WordBlankLine(body);
            WordPara(body, "1.1 Objetivo", "Heading2");
            if (!string.IsNullOrWhiteSpace(sim.Objetivo))
                WordPara(body, sim.Objetivo);
            else
                WordPara(body, "Evaluar el cumplimiento de los controles de acceso SAP con respecto a la Matriz de Puestos aprobada, la nómina vigente y las políticas de identidad corporativa (Entra ID), con el fin de identificar riesgos de acceso no autorizado, conflictos de segregación de funciones y brechas de cumplimiento normativo.");
            WordBlankLine(body);
            WordPara(body, "1.2 Fuentes de Datos", "Heading2");
            WordPara(body, "La auditoría se realizó sobre los datos cargados en la plataforma mediante los módulos de ingesta correspondientes:");
            WordBullet(body, "Roles y usuarios SAP (extracción directa del sistema SAP de la compañía).");
            WordBullet(body, "Nómina de empleados (sistema de RRHH).");
            WordBullet(body, "Matriz de Puestos aprobada por la Gerencia General.");
            WordBullet(body, "Directorio corporativo Microsoft Entra ID (Azure Active Directory).");
            if (sim.FechaReferenciaDatos.HasValue)
            {
                WordBlankLine(body);
                WordPara(body, $"Fecha de referencia de los datos: {sim.FechaReferenciaDatos.Value:dd/MM/yyyy HH:mm}");
            }
            WordPageBreak(body);

            // ── 2. MARCO METODOLÓGICO ─────────────────────────────────────────
            WordPara(body, "2. Marco Metodológico", "Heading1");
            WordPara(body, "El Motor de Control Cruzado aplica cinco reglas de control automatizadas que comparan de forma tridimensional (usuario SAP ↔ empleado nómina ↔ Matriz de Puestos) para detectar desviaciones de acceso. A continuación se describen las reglas ejecutadas:");
            WordBlankLine(body);

            var tReglas = body.AppendChild(new Table());
            tReglas.AppendChild(TableProps());
            var hReglas = tReglas.AppendChild(new TableRow());
            HeaderCell(hReglas, "Regla");
            HeaderCell(hReglas, "Descripción");
            HeaderCell(hReglas, "Riesgo Asociado");
            HeaderCell(hReglas, "Referencia");

            bool altRegla = false;
            foreach (var gr in porRegla)
            {
                var (titulo, desc, riesgo, referencia) = GetRegla(gr.Key);
                if (string.IsNullOrEmpty(desc)) continue;
                var rowR = tReglas.AppendChild(new TableRow());
                DataCell(rowR, titulo,     altRegla, bold: true);
                DataCell(rowR, desc,       altRegla);
                DataCell(rowR, riesgo,     altRegla, color: RojoRiesgo);
                DataCell(rowR, referencia, altRegla, size: 18);
                altRegla = !altRegla;
            }
            WordBlankLine(body);

            WordPara(body, "2.1 Criterios de Criticidad", "Heading2");
            var tCrit = body.AppendChild(new Table());
            tCrit.AppendChild(TableProps());
            var hCrit = tCrit.AppendChild(new TableRow());
            HeaderCell(hCrit, "Nivel");
            HeaderCell(hCrit, "Definición");
            HeaderCell(hCrit, "Acción requerida");
            DataCellColor(tCrit.AppendChild(new TableRow()), new[]
            {
                ("CRÍTICO",  "FF0000"),
                ("Hallazgo que expone un riesgo de fraude, fuga de información o incumplimiento regulatorio inmediato.", "000000"),
                ("Atención inmediata — máx. 5 días hábiles.", "000000"),
            });
            DataCellColor(tCrit.AppendChild(new TableRow()), new[]
            {
                ("MEDIO",    "FF8C00"),
                ("Hallazgo con riesgo significativo pero controlable en el corto plazo.",    "000000"),
                ("Resolución en 30 días hábiles.", "000000"),
            });
            DataCellColor(tCrit.AppendChild(new TableRow()), new[]
            {
                ("BAJO",     "375623"),
                ("Hallazgo de mejora de cumplimiento sin riesgo operativo inmediato.", "000000"),
                ("Resolución en el siguiente ciclo (90 días).", "000000"),
            });
            WordPageBreak(body);

            // ── 3. RESUMEN EJECUTIVO ──────────────────────────────────────────
            WordPara(body, "3. Resumen Ejecutivo", "Heading1");

            // Narrativa ejecutiva
            var nivelRiesgo = criticos > 0 ? "ALTO" : medios > 100 ? "MEDIO" : "BAJO";
            WordPara(body, $"Como resultado de la auditoría de control cruzado, se identificaron un total de {totalH:N0} hallazgos de acceso en el sistema SAP. El nivel de riesgo global de la auditoría es calificado como {nivelRiesgo}.");
            WordBlankLine(body);
            WordPara(body, $"Se detectaron {criticos} hallazgo(s) de criticidad CRÍTICA, {medios:N0} de criticidad MEDIA y {bajos} de criticidad BAJA. Todos los hallazgos requieren acción correctiva por parte de los responsables de TI y Gestión Humana en los plazos indicados en la sección de recomendaciones.");
            WordBlankLine(body);

            // Tabla resumen ejecutivo
            var tResumen = body.AppendChild(new Table());
            tResumen.AppendChild(TableProps());
            var hRes = tResumen.AppendChild(new TableRow());
            HeaderCell(hRes, "Regla de Control");
            HeaderCell(hRes, "Total");
            HeaderCell(hRes, "Críticos");
            HeaderCell(hRes, "Medios");
            HeaderCell(hRes, "Bajos");

            bool altRes = false;
            foreach (var gr in porRegla)
            {
                var (titulo, _, _, _) = GetRegla(gr.Key);
                var rowRes = tResumen.AppendChild(new TableRow());
                DataCell(rowRes, titulo, altRes, bold: true);
                DataCell(rowRes, gr.Count().ToString("N0"), altRes, bold: true, centered: true);
                DataCell(rowRes, gr.Count(h => h.Criticidad == Criticidad.CRITICA).ToString(), altRes, color: gr.Any(h => h.Criticidad == Criticidad.CRITICA) ? RojoRiesgo : "000000", centered: true);
                DataCell(rowRes, gr.Count(h => h.Criticidad == Criticidad.MEDIA).ToString(),   altRes, centered: true);
                DataCell(rowRes, gr.Count(h => h.Criticidad == Criticidad.BAJA).ToString(),    altRes, centered: true);
                altRes = !altRes;
            }
            // Fila totales
            var rowTot = tResumen.AppendChild(new TableRow());
            DataCell(rowTot, "TOTAL", false, bold: true, bgColor: AzulClaro);
            DataCell(rowTot, totalH.ToString("N0"), false, bold: true, bgColor: AzulClaro, centered: true);
            DataCell(rowTot, criticos.ToString(), false, bold: true, bgColor: AzulClaro, centered: true, color: criticos > 0 ? RojoRiesgo : "000000");
            DataCell(rowTot, medios.ToString("N0"), false, bold: true, bgColor: AzulClaro, centered: true);
            DataCell(rowTot, bajos.ToString(), false, bold: true, bgColor: AzulClaro, centered: true);
            WordPageBreak(body);

            // ── 4. HALLAZGOS DETALLADOS POR REGLA ────────────────────────────
            WordPara(body, "4. Hallazgos Detallados por Regla de Control", "Heading1");

            int secNum = 1;
            foreach (var gr in porRegla)
            {
                var (titulo, desc, riesgo, referencia) = GetRegla(gr.Key);
                var gList = gr.OrderByDescending(h => h.Criticidad).ToList();
                int gCrit = gList.Count(h => h.Criticidad == Criticidad.CRITICA);
                int gMed  = gList.Count(h => h.Criticidad == Criticidad.MEDIA);
                int gBaj  = gList.Count(h => h.Criticidad == Criticidad.BAJA);

                WordPara(body, $"4.{secNum}. {titulo}", "Heading2");
                if (!string.IsNullOrEmpty(desc))
                {
                    WordPara(body, $"Descripción: {desc}");
                    if (!string.IsNullOrEmpty(riesgo))
                        WordPara(body, $"Riesgo: {riesgo}", color: RojoRiesgo);
                    if (!string.IsNullOrEmpty(referencia))
                        WordPara(body, $"Marco de referencia: {referencia}", size: 18);
                    WordBlankLine(body);
                }

                WordPara(body, $"Se identificaron {gList.Count:N0} hallazgos en esta regla: {gCrit} crítico(s), {gMed:N0} medio(s) y {gBaj} bajo(s).");
                WordBlankLine(body);

                // Tabla de hallazgos
                var tH = body.AppendChild(new Table());
                tH.AppendChild(TableProps());
                var hH = tH.AppendChild(new TableRow());
                HeaderCell(hH, "#");
                HeaderCell(hH, "Usuario SAP");
                HeaderCell(hH, "Cédula");
                HeaderCell(hH, "Rol Afectado");
                HeaderCell(hH, "Criticidad");
                HeaderCell(hH, "Descripción del hallazgo");

                int rowN = 1;
                bool alt = false;
                foreach (var h in gList.Take(500))
                {
                    var rowH = tH.AppendChild(new TableRow());
                    DataCell(rowH, rowN.ToString(), alt, centered: true, size: 18);
                    DataCell(rowH, h.UsuarioSAP ?? "—", alt, bold: true, mono: true);
                    DataCell(rowH, h.Cedula     ?? "—", alt, mono: true);
                    DataCell(rowH, h.RolAfectado ?? "—", alt, size: 18);
                    CriticidadCell(rowH, h.Criticidad);
                    DataCell(rowH, TruncateDescription(h.Descripcion, 160), alt, size: 18);
                    rowN++;
                    alt = !alt;
                }

                if (gList.Count > 500)
                    WordPara(body, $"* Se presentan los primeros 500 de {gList.Count:N0} hallazgos ordenados por criticidad. El listado completo está disponible en la plataforma AuditorPRO TI.", size: 18, color: AzulMedio);

                WordBlankLine(body);
                secNum++;
            }
            WordPageBreak(body);

            // ── 5. PLAN DE ACCIÓN Y RECOMENDACIONES ──────────────────────────
            WordPara(body, "5. Plan de Acción y Recomendaciones", "Heading1");
            WordPara(body, "Con base en los hallazgos identificados, se formulan las siguientes recomendaciones ordenadas por prioridad:");
            WordBlankLine(body);

            var tRec = body.AppendChild(new Table());
            tRec.AppendChild(TableProps());
            var hRec = tRec.AppendChild(new TableRow());
            HeaderCell(hRec, "#");
            HeaderCell(hRec, "Recomendación");
            HeaderCell(hRec, "Prioridad");
            HeaderCell(hRec, "Responsable");
            HeaderCell(hRec, "Plazo");

            var recomendaciones = new (string Rec, string Pri, string Resp, string Plazo)[]
            {
                ("Revocar de forma inmediata todos los accesos SAP de ex-empleados identificados en R02, validando previamente con RRHH la lista final de bajas.", "CRÍTICA", "TI / RRHH", "5 días hábiles"),
                ("Revisar y depurar los roles SAP identificados como fuera de Matriz (R03): validar con Jefaturas si el acceso es requerido; si no, revocar.", "ALTA", "TI / Jefaturas", "30 días hábiles"),
                ("Corregir todos los conflictos de Segregación de Funciones (R01), asegurando que ningún usuario acumule funciones incompatibles según política de SoD.", "ALTA", "TI / Contraloría", "30 días hábiles"),
                ("Provisionar cuentas Entra ID corporativas para todos los usuarios SAP activos sin cuenta (R04) y activar la autenticación multifactor (MFA).", "ALTA", "TI / Seguridad", "15 días hábiles"),
                ("Eliminar los roles SAP sin uso de transacciones en el período auditado (R05), aplicando el principio de mínimo privilegio.", "MEDIA", "TI", "60 días hábiles"),
                ("Actualizar la Matriz de Puestos para reflejar los roles efectivamente requeridos por cada puesto, y obtener aprobación formal de Gerencia.", "MEDIA", "Contraloría / RRHH", "90 días"),
                ("Establecer un proceso de revisión periódica de accesos SAP con frecuencia mínima trimestral, documentando las aprobaciones.", "MEDIA", "TI / Auditoría", "Próximo trimestre"),
                ("Integrar el proceso de baja de empleados (RRHH) con la revocación automática de accesos SAP en un plazo máximo de 24 horas.", "MEDIA", "TI / RRHH", "90 días"),
            };

            bool altRec = false;
            int recN = 1;
            foreach (var (rec, pri, resp, plazo) in recomendaciones)
            {
                var rowRec = tRec.AppendChild(new TableRow());
                DataCell(rowRec, recN.ToString(), altRec, centered: true, bold: true, size: 18);
                DataCell(rowRec, rec, altRec);
                DataCell(rowRec, pri, altRec, color: pri == "CRÍTICA" ? RojoRiesgo : (pri == "ALTA" ? "C55A11" : "000000"), bold: true, centered: true, size: 18);
                DataCell(rowRec, resp,  altRec, size: 18, centered: true);
                DataCell(rowRec, plazo, altRec, size: 18, centered: true);
                altRec = !altRec;
                recN++;
            }
            WordBlankLine(body);
            WordPageBreak(body);

            // ── 6. CONCLUSIÓN ─────────────────────────────────────────────────
            WordPara(body, "6. Conclusión", "Heading1");
            WordPara(body, $"La auditoría de controles de acceso SAP para el período {sim.PeriodoInicio:dd/MM/yyyy} — {sim.PeriodoFin:dd/MM/yyyy} revela la existencia de {totalH:N0} brechas de control de acceso que requieren atención. " +
                $"El hallazgo de mayor impacto corresponde a la Regla {porRegla.FirstOrDefault()?.Key ?? "R03"} con {porRegla.FirstOrDefault()?.Count() ?? 0:N0} casos.");
            WordBlankLine(body);
            WordPara(body, "Se recomienda que la Gerencia General y la Dirección de TI implementen las acciones correctivas detalladas en este informe dentro de los plazos indicados, y que se establezca un mecanismo de seguimiento formal que permita verificar el cierre de cada hallazgo mediante evidencia documentada.");
            WordBlankLine(body);
            WordPara(body, "Este informe fue generado automáticamente por la plataforma AuditorPRO TI. Los hallazgos reflejan el estado de los datos al momento de la ejecución y deben ser validados con los responsables operativos antes de proceder con las acciones correctivas.", size: 18, color: "595959");
            WordBlankLine(body, 3);

            // Bloque de firmas
            WordPara(body, "Aprobaciones", "Heading2");
            var tFirmas = body.AppendChild(new Table());
            tFirmas.AppendChild(TableProps());
            var hFirmas = tFirmas.AppendChild(new TableRow());
            HeaderCell(hFirmas, "Rol");
            HeaderCell(hFirmas, "Nombre");
            HeaderCell(hFirmas, "Firma / Fecha");
            FirmaFila(tFirmas, "Auditor TI");
            FirmaFila(tFirmas, "Director de TI");
            FirmaFila(tFirmas, "Gerencia General");

            // Configuración de página (A4, márgenes)
            var sectPr = body.AppendChild(new SectionProperties());
            sectPr.AppendChild(new PageSize { Width = 11906, Height = 16838, Orient = PageOrientationValues.Portrait });
            sectPr.AppendChild(new PageMargin { Top = 1440, Right = 1080, Bottom = 1440, Left = 1080, Header = 709, Footer = 709, Gutter = 0 });

            doc.Save();
        }

        return ms.ToArray();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PPT — Presentación ejecutiva para auditor externo
    // ═════════════════════════════════════════════════════════════════════════
    public async Task<byte[]> GeneratePptSummaryAsync(Guid simulacionId, CancellationToken ct = default)
    {
        var sim = await _simulacionRepo.GetWithResultadosAsync(simulacionId, ct)
            ?? throw new KeyNotFoundException($"Simulación {simulacionId} no encontrada.");

        var hallazgos = (await _hallazgoRepo.GetBySimulacionAsync(simulacionId, ct)).ToList();

        var totalH   = hallazgos.Count;
        var criticos = hallazgos.Count(h => h.Criticidad == Criticidad.CRITICA);
        var medios   = hallazgos.Count(h => h.Criticidad == Criticidad.MEDIA);
        var bajos    = hallazgos.Count(h => h.Criticidad == Criticidad.BAJA);
        var nivelRiesgo = criticos > 0 ? "ALTO" : medios > 100 ? "MEDIO" : "BAJO";
        var colorNivel  = criticos > 0 ? "FF0000" : medios > 100 ? "FF8C00" : "375623";

        var porRegla = hallazgos
            .GroupBy(h => h.TipoHallazgo ?? "SIN_CLASIFICAR")
            .OrderByDescending(g => g.Count())
            .ToList();

        using var ms = new MemoryStream();
        using (var ppt = PresentationDocument.Create(ms, PresentationDocumentType.Presentation))
        {
            var presPart = ppt.AddPresentationPart();
            presPart.Presentation = new P.Presentation();

            var slideIdList = new P.SlideIdList();
            presPart.Presentation.Append(
                slideIdList,
                new P.SlideSize { Cx = 9144000, Cy = 5143500, Type = P.SlideSizeValues.Screen16x9 },
                new P.NotesSize { Cx = 6858000, Cy = 9144000 });

            uint slideId = 256;

            // ── Slide 1: Portada ──────────────────────────────────────────────
            AddSlide(presPart, slideIdList, ref slideId, bgColor: AzulOscuro, shapes: b =>
            {
                PptTextBox(b, 1, "CONFIDENCIAL",
                    x: 457200, y: 200000, cx: 8229600, cy: 400000,
                    size: 1400, bold: false, color: "FF4444", centered: true);
                PptTextBox(b, 2, "ILG LOGISTICS",
                    x: 457200, y: 700000, cx: 8229600, cy: 600000,
                    size: 3600, bold: true, color: Blanco, centered: true);
                PptTextBox(b, 3, "Informe de Auditoría de Controles de Acceso SAP",
                    x: 457200, y: 1400000, cx: 8229600, cy: 700000,
                    size: 2200, bold: false, color: "ADD8E6", centered: true);
                PptTextBox(b, 4, sim.Nombre,
                    x: 457200, y: 2200000, cx: 8229600, cy: 500000,
                    size: 1800, bold: true, color: Blanco, centered: true);
                PptTextBox(b, 5,
                    $"Período: {sim.PeriodoInicio:dd/MM/yyyy} — {sim.PeriodoFin:dd/MM/yyyy}     |     Generado: {DateTime.Now:dd/MM/yyyy}",
                    x: 457200, y: 2900000, cx: 8229600, cy: 400000,
                    size: 1400, bold: false, color: "AAAAAA", centered: true);
                PptTextBox(b, 6, "AuditorPRO TI — Sistema de Auditoría Preventiva",
                    x: 457200, y: 4600000, cx: 8229600, cy: 350000,
                    size: 1200, bold: false, color: "888888", centered: true);
            });

            // ── Slide 2: Índice ───────────────────────────────────────────────
            AddSlide(presPart, slideIdList, ref slideId, bgColor: Blanco, shapes: b =>
            {
                SlideTitle(b, "Contenido de la Presentación");
                PptTextBox(b, 2,
                    "1.  Resumen Ejecutivo\n2.  Nivel de Riesgo Global\n3.  Alcance y Metodología\n4.  Hallazgos por Regla de Control\n5.  Recomendaciones y Plan de Acción\n6.  Próximos Pasos",
                    x: 600000, y: 1400000, cx: 7944000, cy: 3200000,
                    size: 1800, bold: false, color: "333333", centered: false);
            });

            // ── Slide 3: Resumen ejecutivo ────────────────────────────────────
            AddSlide(presPart, slideIdList, ref slideId, bgColor: Blanco, shapes: b =>
            {
                SlideTitle(b, "Resumen Ejecutivo");
                // Tres cuadros de KPI
                KpiBox(b, 2, "TOTAL HALLAZGOS", totalH.ToString("N0"), "000000", x: 457200,  y: 1200000);
                KpiBox(b, 3, "CRÍTICOS",         criticos.ToString(),    "FF0000", x: 3200000, y: 1200000);
                KpiBox(b, 4, "MEDIOS",            medios.ToString("N0"), "FF8C00", x: 5940000, y: 1200000);
                PptTextBox(b, 5,
                    $"Reglas ejecutadas: {porRegla.Count}     |     Nivel de riesgo global: {nivelRiesgo}     |     Bajos: {bajos}",
                    x: 457200, y: 2800000, cx: 8229600, cy: 400000,
                    size: 1600, bold: false, color: "555555", centered: true);
                PptTextBox(b, 6,
                    $"La auditoría identificó {totalH:N0} hallazgos de control de acceso SAP. " +
                    $"El nivel de riesgo global es calificado como {nivelRiesgo}. " +
                    $"Se requiere atención inmediata de {criticos + medios:N0} hallazgos (críticos + medios).",
                    x: 457200, y: 3350000, cx: 8229600, cy: 1200000,
                    size: 1600, bold: false, color: "333333", centered: false);
            });

            // ── Slide 4: Nivel de riesgo ──────────────────────────────────────
            AddSlide(presPart, slideIdList, ref slideId, bgColor: Blanco, shapes: b =>
            {
                SlideTitle(b, "Nivel de Riesgo Global");
                // Gran indicador central
                PptTextBox(b, 2, nivelRiesgo,
                    x: 2800000, y: 1400000, cx: 3500000, cy: 900000,
                    size: 5400, bold: true, color: colorNivel, centered: true);
                PptTextBox(b, 3,
                    $"Críticos: {criticos} ({Pct(criticos, totalH)}%)     Medios: {medios:N0} ({Pct(medios, totalH)}%)     Bajos: {bajos} ({Pct(bajos, totalH)}%)",
                    x: 457200, y: 2500000, cx: 8229600, cy: 500000,
                    size: 1800, bold: false, color: "555555", centered: true);
                PptTextBox(b, 4,
                    "La clasificación se basa en el impacto potencial sobre la seguridad, el cumplimiento regulatorio y la continuidad operativa.",
                    x: 457200, y: 3200000, cx: 8229600, cy: 600000,
                    size: 1600, bold: false, color: "777777", centered: true);
            });

            // ── Slide 5: Alcance y metodología ────────────────────────────────
            AddSlide(presPart, slideIdList, ref slideId, bgColor: GrisClaro, shapes: b =>
            {
                SlideTitle(b, "Alcance y Metodología");
                PptTextBox(b, 2,
                    $"Período auditado: {sim.PeriodoInicio:dd/MM/yyyy} — {sim.PeriodoFin:dd/MM/yyyy}\n" +
                    "Fuentes de datos: Roles SAP  ·  Nómina RRHH  ·  Matriz de Puestos  ·  Entra ID\n\n" +
                    "Metodología — Motor de Control Cruzado (5 reglas automatizadas):\n" +
                    "  R01  Segregación de Funciones (SoD) — roles incompatibles en un mismo usuario\n" +
                    "  R02  Ex-empleados con acceso SAP activo\n" +
                    "  R03  Roles fuera de Matriz de Puestos aprobada\n" +
                    "  R04  Usuarios SAP sin cuenta Entra ID corporativa\n" +
                    "  R05  Roles sin ejecución de transacciones en el período",
                    x: 457200, y: 1300000, cx: 8229600, cy: 3500000,
                    size: 1600, bold: false, color: "222222", centered: false);
            });

            // ── Slides por regla ──────────────────────────────────────────────
            foreach (var gr in porRegla)
            {
                var (titulo, desc, riesgo, _) = GetRegla(gr.Key);
                var gCrit = gr.Count(h => h.Criticidad == Criticidad.CRITICA);
                var gMed  = gr.Count(h => h.Criticidad == Criticidad.MEDIA);
                var gBaj  = gr.Count(h => h.Criticidad == Criticidad.BAJA);
                var top5  = gr.OrderByDescending(h => h.Criticidad).Take(5).ToList();

                var ejemplos = top5.Select(h =>
                    $"  • {(h.UsuarioSAP ?? "?"),-12} | {(h.Cedula ?? "?"),-12} | {TruncateDescription(h.RolAfectado ?? "?", 35),-35} [{h.Criticidad}]")
                    .ToList();

                AddSlide(presPart, slideIdList, ref slideId, bgColor: Blanco, shapes: b =>
                {
                    SlideTitle(b, titulo);
                    PptTextBox(b, 2,
                        $"Total: {gr.Count():N0}   |   Críticos: {gCrit}   |   Medios: {gMed:N0}   |   Bajos: {gBaj}",
                        x: 457200, y: 1100000, cx: 8229600, cy: 400000,
                        size: 1800, bold: true, color: gCrit > 0 ? RojoRiesgo : AzulMedio, centered: false);
                    if (!string.IsNullOrEmpty(desc))
                        PptTextBox(b, 3, $"Riesgo: {riesgo}",
                            x: 457200, y: 1600000, cx: 8229600, cy: 350000,
                            size: 1400, bold: false, color: RojoRiesgo, centered: false);
                    PptTextBox(b, 4,
                        "Ejemplos representativos (ordenados por criticidad):\n" + string.Join("\n", ejemplos),
                        x: 457200, y: 2050000, cx: 8229600, cy: 2700000,
                        size: 1500, bold: false, color: "333333", centered: false);
                });
            }

            // ── Slide: Recomendaciones ────────────────────────────────────────
            AddSlide(presPart, slideIdList, ref slideId, bgColor: AzulOscuro, shapes: b =>
            {
                SlideTitle(b, "Recomendaciones Principales", color: Blanco);
                PptTextBox(b, 2,
                    "1.  Revocar accesos SAP de ex-empleados — Plazo: 5 días hábiles [CRÍTICO]\n" +
                    "2.  Depurar roles fuera de Matriz de Puestos (R03) — Plazo: 30 días [ALTO]\n" +
                    "3.  Corregir conflictos de SoD (R01) — Plazo: 30 días [ALTO]\n" +
                    "4.  Provisionar Entra ID + MFA a usuarios SAP (R04) — Plazo: 15 días [ALTO]\n" +
                    "5.  Eliminar roles sin uso de transacciones (R05) — Plazo: 60 días [MEDIO]\n" +
                    "6.  Actualizar y aprobar formalmente la Matriz de Puestos — Plazo: 90 días",
                    x: 457200, y: 1200000, cx: 8229600, cy: 3400000,
                    size: 1700, bold: false, color: Blanco, centered: false);
            });

            // ── Slide: Próximos pasos ─────────────────────────────────────────
            AddSlide(presPart, slideIdList, ref slideId, bgColor: AzulClaro, shapes: b =>
            {
                SlideTitle(b, "Próximos Pasos");
                PptTextBox(b, 2,
                    "INMEDIATO (0-5 días)\n" +
                    "  Validar lista de ex-empleados con RRHH y revocar accesos SAP\n\n" +
                    "CORTO PLAZO (15-30 días)\n" +
                    "  Resolver conflictos de SoD · Provisionar Entra ID + MFA · Depurar roles R03\n\n" +
                    "MEDIANO PLAZO (60-90 días)\n" +
                    "  Eliminar roles R05 · Actualizar Matriz de Puestos · Establecer revisión trimestral\n\n" +
                    "SEGUIMIENTO\n" +
                    "  Registro de evidencias en AuditorPRO TI para cierre formal de cada hallazgo",
                    x: 457200, y: 1200000, cx: 8229600, cy: 3500000,
                    size: 1700, bold: false, color: "222222", centered: false);
            });

            // ── Slide final: Cierre ───────────────────────────────────────────
            AddSlide(presPart, slideIdList, ref slideId, bgColor: AzulOscuro, shapes: b =>
            {
                PptTextBox(b, 1, "ILG LOGISTICS",
                    x: 457200, y: 1500000, cx: 8229600, cy: 700000,
                    size: 3200, bold: true, color: Blanco, centered: true);
                PptTextBox(b, 2, "Auditoría de Controles de Acceso SAP",
                    x: 457200, y: 2300000, cx: 8229600, cy: 500000,
                    size: 2000, bold: false, color: "ADD8E6", centered: true);
                PptTextBox(b, 3, "Este documento es confidencial y de uso exclusivo del equipo de auditoría.",
                    x: 457200, y: 3200000, cx: 8229600, cy: 400000,
                    size: 1400, bold: false, color: "888888", centered: true);
                PptTextBox(b, 4, "AuditorPRO TI  —  " + DateTime.Now.Year.ToString(),
                    x: 457200, y: 4600000, cx: 8229600, cy: 350000,
                    size: 1200, bold: false, color: "666666", centered: true);
            });

            presPart.Presentation.Save();
        }

        return ms.ToArray();
    }

    public async Task<byte[]> GenerateHallazgosExcelAsync(IEnumerable<Guid> hallazgoIds, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // WORD — helpers
    // ═════════════════════════════════════════════════════════════════════════
    private static void WordPara(Body body, string text, string? styleId = null,
        bool centered = false, string? color = null, bool bold = false, int size = 22)
    {
        var para = body.AppendChild(new Paragraph());
        var pp = para.AppendChild(new ParagraphProperties());
        if (styleId != null) pp.AppendChild(new ParagraphStyleId { Val = styleId });
        if (centered)        pp.AppendChild(new Justification { Val = JustificationValues.Center });
        var run = para.AppendChild(new Run());
        var rp = new RunProperties();
        if (bold)           rp.AppendChild(new Bold());
        if (color != null)  rp.AppendChild(new Color { Val = color });
        if (size != 22)     rp.AppendChild(new FontSize { Val = (size).ToString() });
        run.AppendChild(rp);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static void WordBullet(Body body, string text)
    {
        var para = body.AppendChild(new Paragraph());
        var pp = para.AppendChild(new ParagraphProperties());
        pp.AppendChild(new Indentation { Left = "720" });
        var run = para.AppendChild(new Run());
        run.AppendChild(new Text("• " + text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static void WordBlankLine(Body body, int count = 1)
    {
        for (int i = 0; i < count; i++)
            body.AppendChild(new Paragraph());
    }

    private static void WordPageBreak(Body body)
    {
        var para = body.AppendChild(new Paragraph());
        para.AppendChild(new Run()).AppendChild(new Break { Type = BreakValues.Page });
    }

    private static TableProperties TableProps(bool centered = false)
    {
        var props = new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder    { Val = BorderValues.Single, Size = 4, Color = "AAAAAA" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "AAAAAA" },
                new LeftBorder   { Val = BorderValues.Single, Size = 4, Color = "AAAAAA" },
                new RightBorder  { Val = BorderValues.Single, Size = 4, Color = "AAAAAA" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" }
            ));
        if (centered)
            props.AppendChild(new TableJustification { Val = TableRowAlignmentValues.Center });
        return props;
    }

    private static void HeaderCell(TableRow row, string text)
    {
        var cell = row.AppendChild(new TableCell());
        cell.AppendChild(new TableCellProperties(
            new Shading { Fill = AzulOscuro, Color = "auto", Val = ShadingPatternValues.Clear }));
        var para = cell.AppendChild(new Paragraph());
        para.AppendChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        var run = para.AppendChild(new Run());
        run.AppendChild(new RunProperties(new Bold(), new Color { Val = Blanco }, new FontSize { Val = "18" }));
        run.AppendChild(new Text(text));
    }

    private static void DataCell(TableRow row, string text, bool alt,
        bool bold = false, bool centered = false, bool mono = false,
        string? color = null, string? bgColor = null, int size = 20)
    {
        var cell = row.AppendChild(new TableCell());
        var cp = cell.AppendChild(new TableCellProperties());
        var bg = bgColor ?? (alt ? GrisClaro : Blanco);
        cp.AppendChild(new Shading { Fill = bg, Color = "auto", Val = ShadingPatternValues.Clear });
        cp.AppendChild(new TableCellMargin(
            new TopMargin    { Width = "60",  Type = TableWidthUnitValues.Dxa },
            new BottomMargin { Width = "60",  Type = TableWidthUnitValues.Dxa },
            new LeftMargin   { Width = "108", Type = TableWidthUnitValues.Dxa },
            new RightMargin  { Width = "108", Type = TableWidthUnitValues.Dxa }));
        var para = cell.AppendChild(new Paragraph());
        if (centered) para.AppendChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        var run = para.AppendChild(new Run());
        var rp = new RunProperties();
        if (bold)       rp.AppendChild(new Bold());
        if (color != null) rp.AppendChild(new Color { Val = color });
        if (mono)       rp.AppendChild(new RunFonts { Ascii = "Courier New", HighAnsi = "Courier New" });
        rp.AppendChild(new FontSize { Val = size.ToString() });
        run.AppendChild(rp);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static void DataCellColor(TableRow row, (string text, string color)[] cells)
    {
        bool first = true;
        foreach (var (text, color) in cells)
        {
            var cell = row.AppendChild(new TableCell());
            var para = cell.AppendChild(new Paragraph());
            var run  = para.AppendChild(new Run());
            var rp = new RunProperties(new Color { Val = color }, new FontSize { Val = "20" });
            if (first) { rp.AppendChild(new Bold()); first = false; }
            run.AppendChild(rp);
            run.AppendChild(new Text(text));
        }
    }

    private static void CriticidadCell(TableRow row, Criticidad crit)
    {
        var (label, fg, bg) = crit switch
        {
            Criticidad.CRITICA => ("CRÍTICO", Blanco,    RojoRiesgo),
            Criticidad.MEDIA   => ("MEDIO",   "000000",  "FFD966"),
            _                  => ("BAJO",    Blanco,    Verde),
        };
        var cell = row.AppendChild(new TableCell());
        cell.AppendChild(new TableCellProperties(
            new Shading { Fill = bg, Color = "auto", Val = ShadingPatternValues.Clear }));
        var para = cell.AppendChild(new Paragraph());
        para.AppendChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        var run = para.AppendChild(new Run());
        run.AppendChild(new RunProperties(new Bold(), new Color { Val = fg }, new FontSize { Val = "16" }));
        run.AppendChild(new Text(label));
    }

    private static void PortadaFila(Table table, string label, string value)
    {
        var row = table.AppendChild(new TableRow());
        var c1 = row.AppendChild(new TableCell());
        c1.AppendChild(new TableCellProperties(
            new Shading { Fill = AzulOscuro, Color = "auto", Val = ShadingPatternValues.Clear }));
        var p1 = c1.AppendChild(new Paragraph());
        var r1 = p1.AppendChild(new Run());
        r1.AppendChild(new RunProperties(new Bold(), new Color { Val = Blanco }));
        r1.AppendChild(new Text(label));

        var c2 = row.AppendChild(new TableCell());
        var p2 = c2.AppendChild(new Paragraph());
        var r2 = p2.AppendChild(new Run());
        r2.AppendChild(new RunProperties(new FontSize { Val = "22" }));
        r2.AppendChild(new Text(value));
    }

    private static void FirmaFila(Table table, string rol)
    {
        var row = table.AppendChild(new TableRow());
        DataCell(row, rol,         false, bold: true);
        DataCell(row, "",          false);
        DataCell(row, "_____ / ________________", false, centered: true, color: "777777");
    }

    private static void AddHeaderFooter(MainDocumentPart mainPart, string simNombre)
    {
        // Header
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        headerPart.Header = new Header(
            new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId { Val = "Normal" },
                    new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" })),
                new Run(
                    new RunProperties(new Color { Val = "888888" }, new FontSize { Val = "16" }),
                    new Text($"ILG Logistics — {simNombre} — CONFIDENCIAL") { Space = SpaceProcessingModeValues.Preserve })));
        headerPart.Header.Save();

        // Footer con número de página
        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(
            new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Right },
                    new ParagraphBorders(new TopBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" })),
                new Run(
                    new RunProperties(new Color { Val = "888888" }, new FontSize { Val = "16" }),
                    new Text("Página ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
                new Run(new FieldCode(" PAGE ")),
                new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
                new Run(
                    new RunProperties(new Color { Val = "888888" }, new FontSize { Val = "16" }),
                    new Text(" — AuditorPRO TI") { Space = SpaceProcessingModeValues.Preserve })));
        footerPart.Footer.Save();

        // Vincular al documento
        var hRef  = mainPart.GetIdOfPart(headerPart);
        var fRef  = mainPart.GetIdOfPart(footerPart);
        var sectPr = mainPart.Document.Body!.GetFirstChild<SectionProperties>()
                  ?? mainPart.Document.Body!.AppendChild(new SectionProperties());
        sectPr.AppendChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = hRef });
        sectPr.AppendChild(new FooterReference { Type = HeaderFooterValues.Default, Id = fRef });
    }

    private static Styles BuildWordStyles()
    {
        var styles = new Styles();
        styles.AppendChild(new DocDefaults(
            new RunPropertiesDefault(new RunPropertiesBaseStyle(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                new FontSize { Val = "22" }))));
        styles.AppendChild(MakeStyle("Normal",   "Normal",   22, false, "000000"));
        styles.AppendChild(MakeStyle("Title",    "Title",    48, true,  AzulOscuro));
        styles.AppendChild(MakeStyle("Subtitle", "Subtitle", 28, false, AzulMedio));
        styles.AppendChild(MakeStyle("Heading1", "Heading 1", 28, true, AzulOscuro, spaceBefore: 320, spaceAfter: 160));
        styles.AppendChild(MakeStyle("Heading2", "Heading 2", 22, true, AzulMedio,  spaceBefore: 200, spaceAfter: 100));
        return styles;
    }

    private static Style MakeStyle(string id, string name, int halfPts, bool bold, string hexColor,
        int spaceBefore = 0, int spaceAfter = 0)
    {
        var style = new Style { Type = StyleValues.Paragraph, StyleId = id };
        style.Append(new StyleName { Val = name });
        if (spaceBefore > 0 || spaceAfter > 0)
        {
            style.Append(new StyleParagraphProperties(
                new SpacingBetweenLines { Before = spaceBefore.ToString(), After = spaceAfter.ToString() }));
        }
        style.Append(new StyleRunProperties(
            new Bold { Val = bold },
            new Color { Val = hexColor },
            new FontSize { Val = halfPts.ToString() }));
        return style;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PPT — helpers
    // ═════════════════════════════════════════════════════════════════════════
    private static void AddSlide(PresentationPart presPart, P.SlideIdList slideIdList,
        ref uint slideId, string bgColor, Action<P.ShapeTree> shapes)
    {
        var slidePart = presPart.AddNewPart<SlidePart>();
        var shapeTree = new P.ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.GroupShapeProperties(new D.TransformGroup()));

        // Fondo de color sólido
        var csd = new P.CommonSlideData { Name = "" };
        csd.AppendChild(new P.Background(
            new P.BackgroundProperties(
                new D.SolidFill(
                    new D.RgbColorModelHex { Val = bgColor }))));
        csd.AppendChild(shapeTree);

        var slide = new P.Slide(csd);
        shapes(shapeTree);
        slide.Save(slidePart);

        slideIdList.AppendChild(new P.SlideId
        {
            Id = slideId++,
            RelationshipId = presPart.GetIdOfPart(slidePart)
        });
    }

    private static void SlideTitle(P.ShapeTree tree, string text, string color = AzulOscuro)
    {
        PptTextBox(tree, 1, text,
            x: 457200, y: 200000, cx: 8229600, cy: 820000,
            size: 2800, bold: true, color: color, centered: false);

        // Línea decorativa bajo el título
        tree.AppendChild(new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = 99, Name = "Line" },
                new P.NonVisualShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = 457200, Y = 1050000 },
                    new D.Extents { Cx = 8229600, Cy = 50000 }),
                new D.PresetGeometry { Preset = D.ShapeTypeValues.Rectangle },
                new D.SolidFill(new D.RgbColorModelHex { Val = AzulMedio })),
            new P.TextBody(new D.BodyProperties(), new D.ListStyle())));
    }

    private static void KpiBox(P.ShapeTree tree, uint id, string label, string value, string valueColor,
        long x, long y)
    {
        long w = 2400000, h = 1400000;
        // Fondo de la caja
        tree.AppendChild(new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id * 10, Name = $"KpiBg{id}" },
                new P.NonVisualShapeDrawingProperties(),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(new D.Offset { X = x, Y = y }, new D.Extents { Cx = w, Cy = h }),
                new D.PresetGeometry { Preset = D.ShapeTypeValues.RoundRectangle },
                new D.SolidFill(new D.RgbColorModelHex { Val = GrisClaro })),
            new P.TextBody(
                new D.BodyProperties { Anchor = D.TextAnchoringTypeValues.Center },
                new D.ListStyle(),
                PptPara(label, 1400, false, "888888", centered: true),
                PptPara(value, 4400, true,  valueColor, centered: true))));
    }

    private static void PptTextBox(P.ShapeTree tree, uint id, string text,
        long x, long y, long cx, long cy,
        int size, bool bold, string color, bool centered)
    {
        var txBody = new P.TextBody(
            new D.BodyProperties { Wrap = D.TextWrappingValues.Square, Anchor = D.TextAnchoringTypeValues.Top },
            new D.ListStyle());

        foreach (var line in text.Split('\n'))
            txBody.AppendChild(PptPara(line, size, bold, color, centered));

        tree.AppendChild(new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = id, Name = $"txt{id}" },
                new P.NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
                new P.ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(new D.Offset { X = x, Y = y }, new D.Extents { Cx = cx, Cy = cy }),
                new D.PresetGeometry { Preset = D.ShapeTypeValues.Rectangle },
                new D.NoFill()),
            txBody));
    }

    private static D.Paragraph PptPara(string text, int size, bool bold, string color, bool centered = false)
    {
        var para = new D.Paragraph();
        if (centered)
            para.AppendChild(new D.ParagraphProperties { Alignment = D.TextAlignmentTypeValues.Center });

        var rp = new D.RunProperties { Language = "es-CR", FontSize = size, Bold = bold, Dirty = false };
        rp.AppendChild(new D.SolidFill(new D.RgbColorModelHex { Val = color }));

        var run = new D.Run();
        run.AppendChild(rp);
        run.AppendChild(new D.Text(text));
        para.AppendChild(run);
        return para;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Comunes
    // ═════════════════════════════════════════════════════════════════════════
    private static string TruncateDescription(string text, int maxLen)
        => string.IsNullOrEmpty(text) ? "—"
           : text.Length <= maxLen ? text
           : text[..maxLen] + "…";

    private static int Pct(int value, int total)
        => total == 0 ? 0 : (int)Math.Round((double)value / total * 100);
}
