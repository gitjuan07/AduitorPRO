namespace AuditorPRO.Infrastructure.Helpers;

public static class IdentityNormalizationHelper
{
    /// <summary>
    /// Normaliza una cédula: quita guiones, espacios, convierte a mayúsculas.
    /// Regla maestra de identidad: SAP ↔ Nómina ↔ Entra ID.
    /// </summary>
    public static string? NormalizarCedula(string? cedula)
    {
        if (string.IsNullOrWhiteSpace(cedula)) return null;
        return cedula
            .Trim()
            .Replace("-", "")
            .Replace(" ", "")
            .ToUpperInvariant();
    }

    /// <summary>
    /// Normaliza un correo electrónico: trim + minúsculas.
    /// </summary>
    public static string? NormalizarEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normaliza nombre de rol SAP: trim + mayúsculas.
    /// </summary>
    public static string? NormalizarRol(string? rol)
    {
        if (string.IsNullOrWhiteSpace(rol)) return null;
        return rol.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Compara dos cédulas normalizadas. Retorna true si son la misma identidad.
    /// </summary>
    public static bool MismaCedula(string? a, string? b)
    {
        var na = NormalizarCedula(a);
        var nb = NormalizarCedula(b);
        return na != null && nb != null && na == nb;
    }
}
