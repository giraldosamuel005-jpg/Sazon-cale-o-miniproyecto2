using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Core.Dominio;
using Restaurante.Core.Seguridad;
using Restaurante.Core.Servicios;

namespace Restaurante.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class ProductosController : BaseController
{
    private readonly ServicioMenu _menu;

    public ProductosController(ServicioMenu menu) => _menu = menu;

    public async Task<IActionResult> Index() => View(await _menu.ObtenerProductosAsync());

    public IActionResult Crear() => View("Formulario", new Producto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Producto producto)
    {
        if (!ModelState.IsValid) return View("Formulario", producto);
        try
        {
            await _menu.CrearAsync(producto);
            MostrarExito($"Producto '{producto.Nombre}' creado.");
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(nameof(Producto.Nombre), ex.Message);
            return View("Formulario", producto);
        }
    }

    public async Task<IActionResult> Editar(int id)
    {
        var producto = await _menu.ObtenerAsync(id);
        return producto is null ? NotFound() : View("Formulario", producto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Producto producto)
    {
        if (id != producto.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Formulario", producto);
        try
        {
            await _menu.ActualizarAsync(producto);
            MostrarExito($"Producto '{producto.Nombre}' actualizado.");
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(nameof(Producto.Nombre), ex.Message);
            return View("Formulario", producto);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _menu.EliminarAsync(id);
            MostrarExito("Producto eliminado.");
        }
        catch (ReglaNegocioException ex)
        {
            MostrarError(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }
}
