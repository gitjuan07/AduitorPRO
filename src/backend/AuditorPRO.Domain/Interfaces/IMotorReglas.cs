using AuditorPRO.Domain.Entities;

namespace AuditorPRO.Domain.Interfaces;

public interface IMotorReglasService
{
    Task<ResultadoEjecucion> EjecutarSimulacionAsync(SimulacionAuditoria simulacion, CancellationToken ct = default);
}

public class ResultadoEjecucion
{
    public int TotalControles { get; set; }
    public int ControlesVerde { get; set; }
    public int ControlesAmarillo { get; set; }
    public int ControlesRojo { get; set; }
    public decimal ScoreMadurez { get; set; }
    public decimal PorcentajeCumplimiento { get; set; }
    public List<ResultadoControl> Resultados { get; set; } = [];
    public List<Hallazgo> HallazgosGenerados { get; set; } = [];
}
