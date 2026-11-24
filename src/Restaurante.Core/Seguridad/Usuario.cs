// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using Microsoft.AspNetCore.Identity;

namespace Restaurante.Core.Seguridad;

/// <summary>Usuario del sistema: extiende el usuario de ASP.NET Core Identity con el nombre visible.</summary>
public class Usuario : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
}
