using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Restaurante.Web.Controllers;

/// <summary>Controlador base con utilidades comunes: mensajes al usuario y datos de la sesión.</summary>
public abstract class BaseController : Controller
{
    protected void MostrarExito(string mensaje) => TempData["Exito"] = mensaje;
    protected void MostrarError(string mensaje) => TempData["Error"] = mensaje;

    protected string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
