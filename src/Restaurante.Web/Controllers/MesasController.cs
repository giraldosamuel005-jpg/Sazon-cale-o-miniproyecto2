using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Core.Dominio;
using Restaurante.Core.Seguridad;
using Restaurante.Core.Servicios;

namespace Restaurante.Web.Controllers;

// Los meseros ven el plano de mesas; solo el administrador las crea, edita o elimina.
// (Si se pusiera [Authorize(Roles=Admin)] en la clase, se sumaría a la del método y el mesero no entraría.)
[Authorize(Roles = Roles.AdminOMesero)]
public class MesasController : BaseController
{
    private readonly ServicioMesas _mesas;

    public MesasController(ServicioMesas mesas) => _mesas = mesas;

    public async Task<IActionResult> Index() => View(await _mesas.ObtenerTodasAsync());

    [Authorize(Roles = Roles.Admin)]
    public IActionResult Crear() => View("Formulario", new Mesa());

    [Authorize(Roles = Roles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Mesa mesa)
    {
        if (!ModelState.IsValid) return View("Formulario", mesa);
        try
        {
            await _mesas.CrearAsync(mesa);
            MostrarExito($"Mesa {mesa.Numero} creada.");
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(nameof(Mesa.Numero), ex.Message);
            return View("Formulario", mesa);
        }
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Editar(int id)
    {
        var mesa = await _mesas.ObtenerAsync(id);
        return mesa is null ? NotFound() : View("Formulario", mesa);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Mesa mesa)
    {
        if (id != mesa.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Formulario", mesa);
        try
        {
            await _mesas.ActualizarAsync(mesa);
            MostrarExito($"Mesa {mesa.Numero} actualizada.");
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(nameof(Mesa.Numero), ex.Message);
            return View("Formulario", mesa);
        }
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _mesas.EliminarAsync(id);
            MostrarExito("Mesa eliminada.");
        }
        catch (ReglaNegocioException ex)
        {
            MostrarError(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }
}
