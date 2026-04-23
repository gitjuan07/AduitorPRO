using AuditorPRO.Domain.Entities;
using AuditorPRO.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuditorPRO.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedSociedadesAsync(db);

        if (await db.Dominios.AnyAsync()) return;

        // ─────────────────────────────────────────────────────────────────────
        // DOMINIOS DE AUDITORÍA
        // ─────────────────────────────────────────────────────────────────────
        var dominios = new[]
        {
            new DominioAuditoria { Id = 1, Codigo = "ID",  Nombre = "Accesos e Identidad",           Descripcion = "Gestión de usuarios, roles, privilegios y autenticación en todos los sistemas TI." },
            new DominioAuditoria { Id = 2, Codigo = "CFG", Nombre = "Configuración de Sistemas",      Descripcion = "Hardening, configuraciones base y parámetros de sistemas críticos." },
            new DominioAuditoria { Id = 3, Codigo = "CHG", Nombre = "Cambios y Parchado",             Descripcion = "Control de cambios en infraestructura, aplicaciones y base de datos." },
            new DominioAuditoria { Id = 4, Codigo = "BCK", Nombre = "Respaldo y Recuperación",        Descripcion = "Políticas de backup, pruebas de restauración y continuidad operativa." },
            new DominioAuditoria { Id = 5, Codigo = "NET", Nombre = "Seguridad en Red",               Descripcion = "Perímetro, segmentación, firewall rules y monitoreo de tráfico." },
            new DominioAuditoria { Id = 6, Codigo = "CMP", Nombre = "Cumplimiento Normativo",         Descripcion = "Alineación con ISO 27001, COBIT 2019, NIST CSF y regulaciones aplicables." },
            new DominioAuditoria { Id = 7, Codigo = "DOC", Nombre = "Documentación y Procedimientos", Descripcion = "Existencia, vigencia y actualización de políticas, manuales y procedimientos TI." },
        };

        db.Dominios.AddRange(dominios);
        await db.SaveChangesAsync();

        // ─────────────────────────────────────────────────────────────────────
        // PUNTOS DE CONTROL — Accesos e Identidad (ID)
        // ─────────────────────────────────────────────────────────────────────
        var controles = new List<PuntoControl>
        {
            new() {
                Id = 1, DominioId = 1, Codigo = "ID-001",
                Nombre = "Usuarios sin actividad en 90+ días",
                Descripcion = "Identifica cuentas de usuario que no han tenido actividad en los últimos 90 días y permanecen activas.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.9.2.1 / COBIT DSS06.03",
                CondicionVerde   = "0 cuentas inactivas ≥ 90 días",
                CondicionAmarillo = "1–5 cuentas inactivas",
                CondicionRojo    = "Más de 5 cuentas inactivas o alguna pertenece a ex-empleado",
                EvidenciaRequerida = "Listado de usuarios con última fecha de acceso; reporte de cuentas activas vs nómina."
            },
            new() {
                Id = 2, DominioId = 1, Codigo = "ID-002",
                Nombre = "Empleados dados de baja con acceso activo",
                Descripcion = "Verifica que no existan cuentas habilitadas asociadas a personal que ya no está en planilla.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.9.2.6 / NIST PR.AC-1",
                CondicionVerde   = "0 empleados de baja con acceso activo",
                CondicionAmarillo = "No aplica — cualquier caso es crítico",
                CondicionRojo    = "≥ 1 empleado de baja conserva acceso activo",
                EvidenciaRequerida = "Cruce entre nómina (BAJA_PROCESADA) y directorio activo/SE Suite."
            },
            new() {
                Id = 3, DominioId = 1, Codigo = "ID-003",
                Nombre = "Usuarios con privilegios excesivos (SoD)",
                Descripcion = "Detecta usuarios que acumulan roles incompatibles según la Matriz de Segregación de Funciones.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "COBIT APO01.02 / SOX Section 404",
                CondicionVerde   = "0 conflictos SoD activos",
                CondicionAmarillo = "1–3 conflictos de bajo riesgo documentados y aprobados",
                CondicionRojo    = "Conflictos SoD no documentados o de riesgo crítico/alto",
                EvidenciaRequerida = "Reporte de matriz puestos-roles; aprobaciones de excepciones vigentes."
            },
            new() {
                Id = 4, DominioId = 1, Codigo = "ID-004",
                Nombre = "Cuentas de servicio con contraseña sin expiración",
                Descripcion = "Revisa cuentas de servicio/aplicación que tengan la política 'contraseña nunca expira' habilitada sin justificación documentada.",
                TipoEvaluacion = TipoEvaluacion.SEMI_AUTOMATICO, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "CIS Controls v8 / ISO 27001 A.9.4.3",
                CondicionVerde   = "Todas las cuentas de servicio tienen excepción documentada y revisada",
                CondicionAmarillo = "1–2 cuentas sin documentación de excepción",
                CondicionRojo    = "≥ 3 cuentas sin documentar o con credenciales compartidas",
                EvidenciaRequerida = "Inventario de cuentas de servicio con política de contraseñas y justificaciones."
            },
            new() {
                Id = 5, DominioId = 1, Codigo = "ID-005",
                Nombre = "MFA habilitado en accesos remotos y VPN",
                Descripcion = "Confirma que todos los accesos remotos (VPN, RDP, Azure, correo externo) requieran autenticación multifactor.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "NIST SP 800-63B / ISO 27001 A.9.4.2",
                CondicionVerde   = "MFA activo al 100% en accesos remotos",
                CondicionAmarillo = "MFA activo > 90% con plan de remediación",
                CondicionRojo    = "MFA activo < 90% o ausente en accesos críticos",
                EvidenciaRequerida = "Reporte de políticas de acceso condicional; pantallas de configuración MFA."
            },

            // ─────────────────────────────────────────────────────────────────
            // Configuración de Sistemas (CFG)
            // ─────────────────────────────────────────────────────────────────
            new() {
                Id = 6, DominioId = 2, Codigo = "CFG-001",
                Nombre = "Parches de seguridad críticos pendientes",
                Descripcion = "Verifica si existen parches críticos o de alta severidad sin aplicar en servidores de producción con más de 30 días de retraso.",
                TipoEvaluacion = TipoEvaluacion.SEMI_AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "CIS Controls v8 #7 / NIST PR.IP-12",
                CondicionVerde   = "0 parches críticos pendientes > 30 días",
                CondicionAmarillo = "Parches críticos pendientes 15–30 días con plan",
                CondicionRojo    = "Parches críticos pendientes > 30 días sin plan documentado",
                EvidenciaRequerida = "Reporte de WSUS / SCCM / Azure Defender; lista de sistemas con versión de OS/software."
            },
            new() {
                Id = 7, DominioId = 2, Codigo = "CFG-002",
                Nombre = "Hardening de servidores según línea base",
                Descripcion = "Compara la configuración de servidores críticos contra los benchmarks CIS para el SO correspondiente.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "CIS Benchmarks / ISO 27001 A.12.6.1",
                CondicionVerde   = "≥ 90% de controles CIS cumplidos en servidores críticos",
                CondicionAmarillo = "75–89% de controles CIS cumplidos",
                CondicionRojo    = "< 75% de controles CIS cumplidos o controles de nivel 1 fallando",
                EvidenciaRequerida = "Reporte de evaluación CIS; capturas de configuración de SO."
            },
            new() {
                Id = 8, DominioId = 2, Codigo = "CFG-003",
                Nombre = "Inventario de activos TI actualizado",
                Descripcion = "Confirma que el inventario de activos (hardware y software) esté completo y actualizado dentro de los últimos 90 días.",
                TipoEvaluacion = TipoEvaluacion.SEMI_AUTOMATICO, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "CIS Controls v8 #1-2 / COBIT BAI09",
                CondicionVerde   = "Inventario actualizado hace ≤ 30 días",
                CondicionAmarillo = "Inventario actualizado hace 31–90 días",
                CondicionRojo    = "Inventario desactualizado > 90 días o inexistente",
                EvidenciaRequerida = "Exportación del CMDB / inventario; fecha de última actualización."
            },

            // ─────────────────────────────────────────────────────────────────
            // Cambios y Parchado (CHG)
            // ─────────────────────────────────────────────────────────────────
            new() {
                Id = 9, DominioId = 3, Codigo = "CHG-001",
                Nombre = "Cambios en producción sin ticket aprobado",
                Descripcion = "Detecta cambios en entornos de producción (BD, servidor, aplicación) sin la correspondiente orden de cambio aprobada en el sistema ITSM.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ITIL v4 Change Enablement / COBIT BAI06",
                CondicionVerde   = "0 cambios sin ticket aprobado",
                CondicionAmarillo = "1–2 cambios de emergencia documentados retroactivamente",
                CondicionRojo    = "≥ 1 cambio sin documentar o aprobado post-facto sin revisión",
                EvidenciaRequerida = "Log de cambios del ITSM vs registros de auditoría del sistema."
            },
            new() {
                Id = 10, DominioId = 3, Codigo = "CHG-002",
                Nombre = "Procedimiento de rollback documentado y probado",
                Descripcion = "Confirma que cada cambio mayor en producción cuente con un plan de reversión documentado y haya sido probado en QA.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.14.2.2 / COBIT BAI06.02",
                CondicionVerde   = "100% de cambios mayores con rollback probado",
                CondicionAmarillo = "Rollback documentado pero no probado en QA",
                CondicionRojo    = "Cambios mayores sin plan de rollback",
                EvidenciaRequerida = "Tickets de cambio con sección de rollback; resultados de pruebas en QA."
            },
            new() {
                Id = 11, DominioId = 3, Codigo = "CHG-003",
                Nombre = "Segregación de ambientes DEV / QA / PRD",
                Descripcion = "Verifica que los ambientes de desarrollo, pruebas y producción estén segregados y que los accesos a producción sean restringidos.",
                TipoEvaluacion = TipoEvaluacion.SEMI_AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.12.1.4 / NIST PR.IP-1",
                CondicionVerde   = "Ambientes segregados; acceso a PRD solo via pipeline aprobado",
                CondicionAmarillo = "Segregación existe pero hay desarrolladores con acceso a PRD",
                CondicionRojo    = "Sin segregación formal o acceso directo a producción generalizado",
                EvidenciaRequerida = "Diagrama de ambientes; políticas de acceso; evidencia de pipeline CI/CD."
            },

            // ─────────────────────────────────────────────────────────────────
            // Respaldo y Recuperación (BCK)
            // ─────────────────────────────────────────────────────────────────
            new() {
                Id = 12, DominioId = 4, Codigo = "BCK-001",
                Nombre = "Respaldos de BD ejecutados y verificados",
                Descripcion = "Confirma que los respaldos de bases de datos críticas se ejecuten según el plan (mínimo diario) y que se verifique su integridad.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.12.3.1 / NIST PR.IP-4",
                CondicionVerde   = "Respaldos ejecutados al 100% en los últimos 7 días con verificación OK",
                CondicionAmarillo = "≤ 1 fallo de respaldo en últimos 7 días con reintento exitoso",
                CondicionRojo    = "≥ 2 fallos o último respaldo exitoso > 48 horas",
                EvidenciaRequerida = "Log de jobs de respaldo; checksums de archivos de backup; alertas de monitoreo."
            },
            new() {
                Id = 13, DominioId = 4, Codigo = "BCK-002",
                Nombre = "Prueba de restauración dentro de los últimos 90 días",
                Descripcion = "Verifica que se haya realizado al menos una prueba de restauración completa de sistemas críticos en los últimos 90 días.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.17.1.3 / COBIT DSS04.07",
                CondicionVerde   = "Prueba de restauración exitosa documentada en los últimos 90 días",
                CondicionAmarillo = "Prueba realizada hace 91–180 días",
                CondicionRojo    = "No hay evidencia de prueba de restauración en los últimos 180 días",
                EvidenciaRequerida = "Acta o reporte de prueba de restauración con RTO/RPO medidos."
            },
            new() {
                Id = 14, DominioId = 4, Codigo = "BCK-003",
                Nombre = "Respaldos almacenados fuera del sitio principal",
                Descripcion = "Confirma que al menos una copia de los respaldos críticos se almacene en ubicación geográficamente distinta al datacenter primario.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.11.2.5 / NIST PR.IP-9",
                CondicionVerde   = "Respaldos replicados a almacenamiento offsite o nube con cifrado",
                CondicionAmarillo = "Respaldos offsite pero sin cifrado o sin verificación",
                CondicionRojo    = "Solo respaldos locales sin copia offsite",
                EvidenciaRequerida = "Configuración de replicación; capturas de almacenamiento secundario."
            },

            // ─────────────────────────────────────────────────────────────────
            // Seguridad en Red (NET)
            // ─────────────────────────────────────────────────────────────────
            new() {
                Id = 15, DominioId = 5, Codigo = "NET-001",
                Nombre = "Reglas de firewall revisadas y depuradas",
                Descripcion = "Verifica que el conjunto de reglas de firewall haya sido revisado en los últimos 90 días y no contenga reglas 'any-to-any' o reglas obsoletas.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "CIS Controls v8 #12 / ISO 27001 A.13.1.1",
                CondicionVerde   = "Revisión documentada ≤ 90 días; sin reglas any-to-any",
                CondicionAmarillo = "Revisión hace 91–180 días o reglas con scope amplio documentadas",
                CondicionRojo    = "Sin revisión documentada o reglas any-to-any activas en producción",
                EvidenciaRequerida = "Export de reglas de firewall; acta de revisión; ticket de derogación de reglas."
            },
            new() {
                Id = 16, DominioId = 5, Codigo = "NET-002",
                Nombre = "Segmentación de red y microsegmentación crítica",
                Descripcion = "Confirma que los sistemas PCI, OT y servidores críticos estén en segmentos de red aislados con controles de acceso estrictos.",
                TipoEvaluacion = TipoEvaluacion.SEMI_AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "PCI DSS 4.0 Req. 1 / NIST PR.AC-5",
                CondicionVerde   = "Segmentación implementada para todos los sistemas críticos",
                CondicionAmarillo = "Segmentación parcial con sistemas críticos aislados pero otros expuestos",
                CondicionRojo    = "Sin segmentación o sistemas críticos en red plana",
                EvidenciaRequerida = "Diagrama de red actualizado; configuración de VLANs; ACLs de routers."
            },
            new() {
                Id = 17, DominioId = 5, Codigo = "NET-003",
                Nombre = "Monitoreo y alertas de seguridad activos",
                Descripcion = "Verifica que exista un SIEM o herramienta de monitoreo de seguridad activa con alertas configuradas para eventos críticos.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.12.4.1 / NIST DE.CM-1",
                CondicionVerde   = "SIEM activo con alertas para brute force, privilegios, anomalías",
                CondicionAmarillo = "Monitoreo activo pero sin alertas automáticas para eventos críticos",
                CondicionRojo    = "Sin monitoreo de seguridad centralizado",
                EvidenciaRequerida = "Dashboard del SIEM; configuración de alertas; últimas 24h de eventos."
            },
            new() {
                Id = 18, DominioId = 5, Codigo = "NET-004",
                Nombre = "Escaneo de vulnerabilidades trimestral",
                Descripcion = "Confirma la realización de escaneos de vulnerabilidades en la infraestructura expuesta a internet y sistemas internos críticos al menos trimestralmente.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "PCI DSS 4.0 Req. 11.3 / NIST DE.CM-8",
                CondicionVerde   = "Escaneo completado en los últimos 90 días con hallazgos críticos remediados",
                CondicionAmarillo = "Escaneo completado hace 91–180 días o hallazgos críticos en proceso de remediación",
                CondicionRojo    = "Sin escaneo en los últimos 180 días o hallazgos críticos abiertos > 30 días",
                EvidenciaRequerida = "Reporte de escaneo (Nessus/Qualys/Defender); plan de remediación."
            },

            // ─────────────────────────────────────────────────────────────────
            // Cumplimiento Normativo (CMP)
            // ─────────────────────────────────────────────────────────────────
            new() {
                Id = 19, DominioId = 6, Codigo = "CMP-001",
                Nombre = "Declaración de Aplicabilidad ISO 27001 vigente",
                Descripcion = "Verifica que la Declaración de Aplicabilidad (SoA) de la organización esté vigente, revisada y aprobada por la alta dirección.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001:2022 Cláusula 6.1.3",
                CondicionVerde   = "SoA aprobada y revisada en los últimos 12 meses",
                CondicionAmarillo = "SoA vigente pero no revisada en 12–24 meses",
                CondicionRojo    = "Sin SoA o no aprobada por dirección",
                EvidenciaRequerida = "Documento SoA con firmas de aprobación y fecha de revisión."
            },
            new() {
                Id = 20, DominioId = 6, Codigo = "CMP-002",
                Nombre = "Cumplimiento de política de retención de datos",
                Descripcion = "Confirma que los datos sensibles (clientes, empleados, financieros) sean eliminados o anonimizados según el período de retención aprobado.",
                TipoEvaluacion = TipoEvaluacion.SEMI_AUTOMATICO, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "GDPR Art. 5 / Ley 8968 CR / COBIT DSS06.05",
                CondicionVerde   = "Política de retención implementada y auditada en el último año",
                CondicionAmarillo = "Política existe pero implementación parcial o sin evidencia de purga",
                CondicionRojo    = "Sin política de retención o datos vencidos sin eliminar",
                EvidenciaRequerida = "Política de retención aprobada; evidencia de purgas o anonimización ejecutadas."
            },
            new() {
                Id = 21, DominioId = 6, Codigo = "CMP-003",
                Nombre = "Contratos con proveedores con cláusulas de seguridad",
                Descripcion = "Verifica que los contratos con proveedores de servicios TI críticos incluyan cláusulas de seguridad de la información y NDA.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.15.1.2 / COBIT APO10",
                CondicionVerde   = "100% de proveedores críticos con cláusulas de seguridad vigentes",
                CondicionAmarillo = "≥ 80% de proveedores críticos con contratos adecuados",
                CondicionRojo    = "< 80% o proveedor crítico sin cláusulas de seguridad",
                EvidenciaRequerida = "Listado de proveedores críticos; contratos con cláusulas de seguridad; registro de evaluaciones."
            },
            new() {
                Id = 22, DominioId = 6, Codigo = "CMP-004",
                Nombre = "Plan de auditoría interna TI ejecutado anualmente",
                Descripcion = "Confirma que se haya ejecutado al menos un ciclo completo de auditoría interna TI en los últimos 12 meses con evidencia de seguimiento.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001:2022 Cláusula 9.2 / COBIT MEA02",
                CondicionVerde   = "Auditoría interna ejecutada en los últimos 12 meses con informe y seguimiento",
                CondicionAmarillo = "Auditoría ejecutada hace 13–18 meses",
                CondicionRojo    = "Sin auditoría interna TI en los últimos 18 meses",
                EvidenciaRequerida = "Programa de auditoría; informes de auditoría; planes de acción correctiva."
            },

            // ─────────────────────────────────────────────────────────────────
            // Documentación y Procedimientos (DOC)
            // ─────────────────────────────────────────────────────────────────
            new() {
                Id = 23, DominioId = 7, Codigo = "DOC-001",
                Nombre = "Política de Seguridad de la Información aprobada y vigente",
                Descripcion = "Verifica la existencia, aprobación directiva y vigencia de la Política Maestra de Seguridad de la Información.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001:2022 Cláusula 5.2",
                CondicionVerde   = "Política aprobada por dirección, revisada en los últimos 12 meses y publicada",
                CondicionAmarillo = "Política vigente pero no revisada en 12–24 meses",
                CondicionRojo    = "Sin política o no aprobada formalmente por dirección",
                EvidenciaRequerida = "Política firmada; página intranet/portal donde está publicada; fecha de última revisión."
            },
            new() {
                Id = 24, DominioId = 7, Codigo = "DOC-002",
                Nombre = "Plan de Continuidad del Negocio (BCP) vigente y probado",
                Descripcion = "Confirma que el BCP exista, esté aprobado y se haya ejercitado al menos una vez en los últimos 12 meses.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 22301 / ISO 27001 A.17 / COBIT DSS04",
                CondicionVerde   = "BCP aprobado, probado en los últimos 12 meses con lecciones aprendidas documentadas",
                CondicionAmarillo = "BCP existe pero no probado en los últimos 12–24 meses",
                CondicionRojo    = "Sin BCP o sin evidencia de prueba en más de 24 meses",
                EvidenciaRequerida = "Documento BCP; actas de ejercicio/prueba; plan de remediación post-ejercicio."
            },
            new() {
                Id = 25, DominioId = 7, Codigo = "DOC-003",
                Nombre = "Procedimientos operativos TI documentados",
                Descripcion = "Verifica que los procedimientos críticos (cambios, incidentes, accesos, respaldos) estén documentados, aprobados y accesibles al equipo TI.",
                TipoEvaluacion = TipoEvaluacion.MANUAL, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.12.1.1 / COBIT BAI10",
                CondicionVerde   = "≥ 8 procedimientos críticos documentados y vigentes",
                CondicionAmarillo = "4–7 procedimientos documentados con brechas identificadas",
                CondicionRojo    = "< 4 procedimientos documentados o inexistencia de repositorio",
                EvidenciaRequerida = "Repositorio de procedimientos; últimas versiones con aprobaciones."
            },
            new() {
                Id = 26, DominioId = 7, Codigo = "DOC-004",
                Nombre = "Capacitación en seguridad a usuarios completada",
                Descripcion = "Confirma que el 100% del personal con acceso a sistemas completó la capacitación anual de concienciación en seguridad.",
                TipoEvaluacion = TipoEvaluacion.SEMI_AUTOMATICO, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.7.2.2 / NIST PR.AT-1",
                CondicionVerde   = "≥ 95% del personal capacitado en los últimos 12 meses",
                CondicionAmarillo = "80–94% del personal capacitado",
                CondicionRojo    = "< 80% del personal capacitado o sin programa formal",
                EvidenciaRequerida = "Registros de asistencia o completación del LMS; programa de capacitación aprobado."
            },
            new() {
                Id = 27, DominioId = 7, Codigo = "DOC-005",
                Nombre = "Registro de incidentes de seguridad TI",
                Descripcion = "Verifica la existencia de un registro formal de incidentes de seguridad TI con clasificación, tiempos de respuesta y lecciones aprendidas.",
                TipoEvaluacion = TipoEvaluacion.SEMI_AUTOMATICO, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.16.1 / NIST RS.CO-3",
                CondicionVerde   = "Registro activo con todos los incidentes del último año clasificados y cerrados",
                CondicionAmarillo = "Registro existe pero con incidentes sin cerrar > 30 días",
                CondicionRojo    = "Sin registro formal o incidentes críticos sin documentar",
                EvidenciaRequerida = "Registro o ticket de incidentes del período; métricas MTTR/MTTD."
            },
        };

        db.PuntosControl.AddRange(controles);
        await db.SaveChangesAsync();

        await SeedControlsCruzadosAsync(db);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CONTROLES CRUZADOS CC-001 a CC-012 — se ejecutan con upsert por código
    // Se pueden agregar aunque ya existan los dominios (guard propio).
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedControlsCruzadosAsync(AppDbContext db)
    {
        var codigosExistentes = await db.PuntosControl
            .Where(p => p.Codigo.StartsWith("CC-"))
            .Select(p => p.Codigo)
            .ToHashSetAsync();

        if (codigosExistentes.Count >= 12) return;

        var dominioId = 1; // Accesos e Identidad

        var nuevos = new List<PuntoControl>
        {
            new() {
                DominioId = dominioId, Codigo = "CC-001",
                Nombre = "Usuario SAP activo sin empleado maestro asociado (por cédula)",
                Descripcion = "Cruza usuarios SAP activos contra la nómina usando la cédula normalizada. Detecta usuarios SAP que no corresponden a ningún empleado activo.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.9.2.1 / COBIT DSS06.03",
                CondicionVerde = "0 usuarios SAP activos sin empleado maestro",
                CondicionAmarillo = "1–5 usuarios SAP sin correspondencia en nómina",
                CondicionRojo = "Más de 5 usuarios SAP sin empleado maestro asociado"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-002",
                Nombre = "Empleado inactivo o baja procesada con usuario SAP activo",
                Descripcion = "Detecta empleados con estado INACTIVO o BAJA_PROCESADA que aún tienen cuenta SAP activa. Cruce por cédula normalizada.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.9.2.6 / NIST PR.AC-1",
                CondicionVerde = "0 empleados de baja con acceso SAP activo",
                CondicionAmarillo = "1–2 casos bajo revisión",
                CondicionRojo = "3+ empleados de baja con acceso SAP activo"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-003",
                Nombre = "Usuario SAP activo sin ingreso en los últimos 90 días",
                Descripcion = "Identifica cuentas SAP activas que no han sido usadas en 90 días o más según la fecha de último ingreso registrada.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "ISO 27001 A.9.2.1 / CIS Controls v8 #5",
                CondicionVerde = "0 cuentas SAP inactivas ≥ 90 días",
                CondicionAmarillo = "1–10 cuentas SAP sin acceso reciente",
                CondicionRojo = "Más de 10 cuentas SAP sin acceso en 90+ días"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-004",
                Nombre = "Usuario Entra ID activo sin EmployeeId",
                Descripcion = "Detecta cuentas habilitadas en Entra ID que no tienen el campo EmployeeId (cédula) configurado. Sin este campo no se puede vincular con SAP o nómina.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.9.2.1 / NIST PR.AC-6",
                CondicionVerde = "0 cuentas Entra ID sin EmployeeId",
                CondicionAmarillo = "1–3 cuentas sin EmployeeId",
                CondicionRojo = "4+ cuentas activas sin EmployeeId"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-005",
                Nombre = "EmployeeId duplicado en Entra ID",
                Descripcion = "Detecta EmployeeId repetidos en el directorio Entra ID. Indica posible error de datos o suplantación de identidad.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.9.2.1 / NIST PR.AC-6",
                CondicionVerde = "0 EmployeeId duplicados en Entra ID",
                CondicionAmarillo = "No aplica — cualquier duplicado es crítico",
                CondicionRojo = "≥ 1 EmployeeId duplicado detectado"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-006",
                Nombre = "Inconsistencia entre cédula SAP y EmployeeId de Entra ID",
                Descripcion = "Cruza por email corporativo y compara la cédula en SAP contra el EmployeeId en Entra ID. Una discrepancia indica posible error de identidad.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "ISO 27001 A.9.2.1 / COBIT DSS06.03",
                CondicionVerde = "0 inconsistencias de identidad entre SAP y Entra ID",
                CondicionAmarillo = "No aplica — cualquier inconsistencia es crítica",
                CondicionRojo = "≥ 1 usuario con cédula SAP distinta al EmployeeId Entra ID"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-007",
                Nombre = "Usuario SAP con sociedad no autorizada por la Matriz de Puestos",
                Descripcion = "Verifica que la combinación Puesto/Sociedad de cada usuario SAP exista en la Matriz de Puestos aprobada por Contraloría.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "COBIT DSS06.03 / ISO 27001 A.9.2.2",
                CondicionVerde = "Todos los usuarios SAP tienen Puesto/Sociedad en la Matriz",
                CondicionAmarillo = "1–5 combinaciones no documentadas en Matriz",
                CondicionRojo = "6+ combinaciones no autorizadas"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-008",
                Nombre = "Rol SAP no permitido por la Matriz de Puestos",
                Descripcion = "Detecta asignaciones de roles SAP que no están autorizadas en la Matriz de Puestos para el puesto y sociedad del usuario. Considera excepciones SE Suite vigentes.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "COBIT DSS06.03 / SOX Section 404",
                CondicionVerde = "Todos los roles están en la Matriz o con excepción SE Suite vigente",
                CondicionAmarillo = "1–3 roles fuera de Matriz sin excepción",
                CondicionRojo = "4+ roles no autorizados sin justificación"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-009",
                Nombre = "Transacción SAP no permitida por la Matriz para el puesto/rol/sociedad",
                Descripcion = "Verifica que las transacciones de cada rol SAP asignado estén autorizadas en la Matriz de Puestos. Excluye casos SE Suite vigentes.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "COBIT DSS06.03 / SOX Section 404",
                CondicionVerde = "Todas las transacciones autorizadas en Matriz o con excepción",
                CondicionAmarillo = "1–5 transacciones fuera de Matriz",
                CondicionRojo = "6+ transacciones no autorizadas"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-010",
                Nombre = "Fecha de revisión de Contraloría vencida en Matriz de Puestos",
                Descripcion = "Detecta puestos cuya última revisión por Contraloría ya expiró según FechaRevisionContraloria en la Matriz de Puestos.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.MEDIA,
                NormaReferencia = "COBIT APO01.02 / ISO 27001 A.9.2.2",
                CondicionVerde = "Todos los puestos con revisión Contraloría vigente",
                CondicionAmarillo = "1–3 puestos con revisión vencida",
                CondicionRojo = "4+ puestos con Matriz sin actualizar por Contraloría"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-011",
                Nombre = "Rol crítico SAP sin expediente o justificación",
                Descripcion = "Verifica que toda asignación de un rol marcado como crítico tenga expediente adjunto o caso SE Suite que la justifique.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "COBIT DSS06.03 / SOX Section 302",
                CondicionVerde = "Todos los roles críticos con expediente o SE Suite vigente",
                CondicionAmarillo = "No aplica — cualquier caso sin justificación es crítico",
                CondicionRojo = "≥ 1 asignación de rol crítico sin respaldo documental"
            },
            new() {
                DominioId = dominioId, Codigo = "CC-012",
                Nombre = "Posible conflicto de Segregación de Funciones (SoD)",
                Descripcion = "Detecta usuarios que tienen asignados simultáneamente roles incompatibles según la Matriz SoD. Considera severidad de los conflictos definidos.",
                TipoEvaluacion = TipoEvaluacion.AUTOMATICO, CriticidadBase = Criticidad.CRITICA,
                NormaReferencia = "COBIT APO01.02 / SOX Section 404 / ISO 27001 A.6.1.2",
                CondicionVerde = "0 usuarios con conflicto SoD activo",
                CondicionAmarillo = "1–2 usuarios con conflicto SoD documentado y mitigado",
                CondicionRojo = "3+ usuarios con conflicto SoD no resuelto"
            },
        };

        // Upsert por código — nunca duplicar
        foreach (var c in nuevos)
        {
            if (!codigosExistentes.Contains(c.Codigo))
                db.PuntosControl.Add(c);
        }

        if (nuevos.Any(c => !codigosExistentes.Contains(c.Codigo)))
            await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SOCIEDADES ILG LOGISTICS — se ejecuta siempre (upsert por Codigo)
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task SeedSociedadesAsync(AppDbContext db)
    {
        var todasLasSociedades = new[]
        {
            new Sociedad { Codigo = "CR00", Nombre = "ILG Logistics S.A.",                          Pais = "Costa Rica",          Activa = false },
            new Sociedad { Codigo = "CR01", Nombre = "Corporación ILG Internacional S.A.",           Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR02", Nombre = "Marina Intercontinental",                      Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR03", Nombre = "Servicios de Atención de Carga S.A.",          Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR04", Nombre = "Servicios Neptuno",                            Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR05", Nombre = "Consolidaciones ILG",                          Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR06", Nombre = "Almacén Fiscal Flogar",                        Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR07", Nombre = "Servinave",                                    Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR08", Nombre = "TGD Soluciones",                               Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR09", Nombre = "ILG Supply Chain Services",                    Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR10", Nombre = "Centro de Distribución ILG Logistics",         Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR12", Nombre = "ILG Logistics S.A.",                           Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "CR14", Nombre = "Terminales de Granos ILG",                     Pais = "Costa Rica",          Activa = true  },
            new Sociedad { Codigo = "DO01", Nombre = "ILG Logistics Dominicana",                     Pais = "República Dominicana", Activa = true  },
            new Sociedad { Codigo = "GT01", Nombre = "ILG Logistics Guatemala",                      Pais = "Guatemala",           Activa = true  },
            new Sociedad { Codigo = "GT02", Nombre = "TGD de Guatemala S.A.",                        Pais = "Guatemala",           Activa = true  },
            new Sociedad { Codigo = "HN01", Nombre = "ILG Logistics de Honduras",                    Pais = "Honduras",            Activa = true  },
            new Sociedad { Codigo = "NI01", Nombre = "ILG Logistics Nicaragua",                      Pais = "Nicaragua",           Activa = true  },
            new Sociedad { Codigo = "PA01", Nombre = "TGD WorldWide INC",                            Pais = "Panamá",              Activa = true  },
            new Sociedad { Codigo = "PA02", Nombre = "COLON CARGO CENTER",                           Pais = "Panamá",              Activa = true  },
            new Sociedad { Codigo = "PA03", Nombre = "ILG Logistics de Panamá",                      Pais = "Panamá",              Activa = true  },
            new Sociedad { Codigo = "PA04", Nombre = "4 ALTOS C-7,C-8,C-9, S.A.",                   Pais = "Panamá",              Activa = false },
            new Sociedad { Codigo = "SV01", Nombre = "ILG Logistics El Salvador",                    Pais = "El Salvador",         Activa = true  },
            new Sociedad { Codigo = "SV02", Nombre = "TGD El Salvador S.A. de C.V.",                 Pais = "El Salvador",         Activa = true  },
        };

        var codigosExistentes = await db.Sociedades
            .Select(s => s.Codigo)
            .ToHashSetAsync();

        var nuevas = todasLasSociedades
            .Where(s => !codigosExistentes.Contains(s.Codigo))
            .ToList();

        if (nuevas.Count > 0)
        {
            db.Sociedades.AddRange(nuevas);
            await db.SaveChangesAsync();
        }
    }
}
