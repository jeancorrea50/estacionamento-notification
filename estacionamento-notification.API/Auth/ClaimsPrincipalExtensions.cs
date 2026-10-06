using System.Security.Claims;

namespace EstacionamentoNotification.API.Auth;

internal static class JwtClaimTypes
{
    /// <summary>ClaimTypes.NameIdentifier após outbound map do JwtSecurityTokenHandler.</summary>
    public const string NameId = "nameid";

    /// <summary>ClaimTypes.Role após outbound map.</summary>
    public const string Role = "role";

    /// <summary>ClaimTypes.Name após outbound map.</summary>
    public const string UniqueName = "unique_name";
}

internal static class ClaimsPrincipalExtensions
{
    public static int ResolveUsuarioId(this ClaimsPrincipal? user)
    {
        if (user is null) return 0;

        var raw =
            user.FindFirstValue(JwtClaimTypes.NameId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return int.TryParse(raw, out var id) ? id : 0;
    }

    public static bool IsInRoleAdmin(this ClaimsPrincipal? user)
    {
        if (user is null) return false;
        if (user.IsInRole("Admin") || user.IsInRole("Administrador")) return true;

        return user.FindAll(JwtClaimTypes.Role)
            .Concat(user.FindAll(ClaimTypes.Role))
            .Any(c =>
                string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Value, "Administrador", StringComparison.OrdinalIgnoreCase));
    }
}
