namespace Restaurante.Core.Seguridad;

/// <summary>Roles del sistema. Se usan como constantes para evitar errores de escritura en [Authorize].</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Mesero = "Mesero";
    public const string Cocina = "Cocina";

    // Combinaciones usadas en [Authorize(Roles = ...)]
    public const string AdminOMesero = Admin + "," + Mesero;
    public const string AdminOCocina = Admin + "," + Cocina;

    public static readonly IReadOnlyList<string> Todos = new[] { Admin, Mesero, Cocina };
}
