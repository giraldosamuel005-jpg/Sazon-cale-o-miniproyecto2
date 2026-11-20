using Microsoft.AspNetCore.Identity;

namespace Restaurante.Core.Seguridad;

/// <summary>Usuario del sistema: extiende el usuario de ASP.NET Core Identity con el nombre visible.</summary>
public class Usuario : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
}
