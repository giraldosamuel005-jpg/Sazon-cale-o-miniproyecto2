// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Core.Dominio;
using Restaurante.Core.Seguridad;
using Restaurante.Core.Servicios;
using Restaurante.Web.Infraestructura;
using Restaurante.Web.Models;

namespace Restaurante.Web.Controllers;

[Authorize(Roles = Roles.AdminOMesero)]
public class OrdenesController : BaseController
{
    private readonly ServicioOrdenes _ordenes;
    private readonly ServicioMenu _menu;
    private readonly ServicioMesas _mesas;

    public OrdenesController(ServicioOrdenes ordenes, ServicioMenu menu, ServicioMesas mesas)
    {
        _ordenes = ordenes;
        _menu = menu;
        _mesas = mesas;
    }

    public async Task<IActionResult> Index() => View(await _ordenes.ObtenerActivasAsync());

    public async Task<IActionResult> Historial() => View(await _ordenes.ObtenerHistorialAsync());

    public async Task<IActionResult> Detalle(int id)
    {
        var orden = await _ordenes.ObtenerAsync(id);
        return orden is null ? NotFound() : View(orden);
    }

    public async Task<IActionResult> Crear(int? mesaId)
    {
        var modelo = new CrearOrdenViewModel { MesaId = mesaId };
        await CargarDatosFormularioAsync(modelo);
        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearOrdenViewModel modelo)
    {
        if (!modelo.Items.Any(i => i.Cantidad > 0))
            ModelState.AddModelError(string.Empty, "Agregue al menos un producto.");

        if (ModelState.IsValid)
        {
            try
            {
                var items = modelo.Items.Select(i => new ItemPedido(i.ProductoId, i.Cantidad));
                var orden = await _ordenes.CrearAsync(modelo.MesaId!.Value, UsuarioId, User.NombreVisible(), items, modelo.Notas);
                MostrarExito($"Orden #{orden.Id} enviada a cocina.");
                return RedirectToAction(nameof(Detalle), new { id = orden.Id });
            }
            catch (ReglaNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await CargarDatosFormularioAsync(modelo);
        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Entregar(int id) => CambiarEstado(id, EstadoOrden.Entregada, "entregada en la mesa");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Cancelar(int id) => CambiarEstado(id, EstadoOrden.Cancelada, "cancelada");

    private async Task<IActionResult> CambiarEstado(int id, EstadoOrden estado, string texto)
    {
        try
        {
            await _ordenes.CambiarEstadoAsync(id, estado);
            MostrarExito($"Orden #{id} {texto}.");
        }
        catch (ReglaNegocioException ex)
        {
            MostrarError(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Llena las mesas libres y la carta, conservando las cantidades ya digitadas.</summary>
    private async Task CargarDatosFormularioAsync(CrearOrdenViewModel modelo)
    {
        modelo.MesasLibres = await _mesas.ObtenerLibresAsync();

        // Dictionary ProductoId -> cantidad digitada, para no perderla si el formulario vuelve con error.
        var cantidades = modelo.Items.ToDictionary(i => i.ProductoId, i => i.Cantidad);
        var menu = await _menu.ObtenerMenuPorCategoriaAsync();

        modelo.Items = menu.SelectMany(categoria => categoria.Value)
            .Select(p => new ItemFormulario
            {
                ProductoId = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Precio = p.Precio,
                Categoria = p.Categoria,
                Cantidad = cantidades.GetValueOrDefault(p.Id)
            })
            .ToList();
    }
}
