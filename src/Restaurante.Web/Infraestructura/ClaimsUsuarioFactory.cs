// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Restaurante.Core.Seguridad;

namespace Restaurante.Web.Infraestructura;

/// <summary>Agrega el nombre completo del usuario a la cookie de sesión como un claim.</summary>
public class ClaimsUsuarioFactory : UserClaimsPrincipalFactory<Usuario, IdentityRole>
{
    public const string ClaimNombre = "NombreCompleto";

    public ClaimsUsuarioFactory(UserManager<Usuario> userManager, RoleManager<IdentityRole> roleManager,
                                IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Usuario user)
    {
        var identidad = await base.GenerateClaimsAsync(user);
        identidad.AddClaim(new Claim(ClaimNombre, user.NombreCompleto));
        return identidad;
    }
}

public static class ClaimsPrincipalExtensiones
{
    public static string NombreVisible(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue(ClaimsUsuarioFactory.ClaimNombre) is { Length: > 0 } nombre
            ? nombre
            : usuario.Identity?.Name ?? "Usuario";
}
