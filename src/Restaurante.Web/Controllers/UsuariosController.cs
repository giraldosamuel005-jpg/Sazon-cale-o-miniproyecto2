// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Core.Seguridad;
using Restaurante.Web.Models;

namespace Restaurante.Web.Controllers;

/// <summary>Gestión de usuarios y roles con UserManager de ASP.NET Core Identity.</summary>
[Authorize(Roles = Roles.Admin)]
public class UsuariosController : BaseController
{
    private readonly UserManager<Usuario> _usuarios;

    public UsuariosController(UserManager<Usuario> usuarios) => _usuarios = usuarios;

    public async Task<IActionResult> Index()
    {
        var lista = new List<UsuarioListadoViewModel>();
        foreach (var u in _usuarios.Users.OrderBy(u => u.Email).ToList())
        {
            lista.Add(new UsuarioListadoViewModel
            {
                Id = u.Id,
                Email = u.Email ?? "",
                Nombre = u.NombreCompleto,
                Roles = await _usuarios.GetRolesAsync(u)
            });
        }
        return View(lista);
    }

    public IActionResult Crear() => View(new CrearUsuarioViewModel { Rol = Roles.Mesero });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearUsuarioViewModel modelo)
    {
        if (!Roles.Todos.Contains(modelo.Rol))
            ModelState.AddModelError(nameof(modelo.Rol), "Rol no válido.");
        if (!ModelState.IsValid) return View(modelo);

        var usuario = new Usuario
        {
            UserName = modelo.Email,
            Email = modelo.Email,
            EmailConfirmed = true,
            NombreCompleto = modelo.Nombre
        };

        var resultado = await _usuarios.CreateAsync(usuario, modelo.Contrasena);
        if (resultado.Succeeded)
            resultado = await _usuarios.AddToRoleAsync(usuario, modelo.Rol);

        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
                ModelState.AddModelError(string.Empty, TraducirError(error));
            return View(modelo);
        }

        MostrarExito($"Usuario {modelo.Email} creado con rol {modelo.Rol}.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string id)
    {
        if (id == UsuarioId)
        {
            MostrarError("No puede eliminar su propio usuario.");
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _usuarios.FindByIdAsync(id);
        if (usuario is not null)
        {
            await _usuarios.DeleteAsync(usuario);
            MostrarExito($"Usuario {usuario.Email} eliminado.");
        }
        return RedirectToAction(nameof(Index));
    }

    private static string TraducirError(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Ya existe un usuario con ese correo.",
        "PasswordTooShort" => "La contraseña debe tener al menos 6 caracteres.",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe tener al menos un símbolo (ej. * o #).",
        "PasswordRequiresDigit" => "La contraseña debe tener al menos un número.",
        "PasswordRequiresUpper" => "La contraseña debe tener al menos una mayúscula.",
        "PasswordRequiresLower" => "La contraseña debe tener al menos una minúscula.",
        _ => error.Description
    };
}
