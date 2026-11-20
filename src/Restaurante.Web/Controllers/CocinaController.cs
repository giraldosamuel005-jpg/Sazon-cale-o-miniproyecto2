using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Core.Dominio;
using Restaurante.Core.Seguridad;
using Restaurante.Core.Servicios;

namespace Restaurante.Web.Controllers;

[Authorize(Roles = Roles.AdminOCocina)]
public class CocinaController : BaseController
{
    private readonly ServicioOrdenes _ordenes;

    public CocinaController(ServicioOrdenes ordenes) => _ordenes = ordenes;

    public async Task<IActionResult> Index()
    {
        ViewBag.Cola = await _ordenes.ObtenerColaCocinaAsync();               // Queue<Orden>
        ViewBag.EnPreparacion = await _ordenes.ObtenerPorEstadoAsync(EstadoOrden.EnPreparacion);
        ViewBag.Listas = await _ordenes.ObtenerPorEstadoAsync(EstadoOrden.Lista);
        return View();
    }

    /// <summary>Saca de la cola la orden más antigua (FIFO) y la pasa a preparación.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TomarSiguiente()
    {
        var orden = await _ordenes.TomarSiguienteAsync();
        if (orden is null) MostrarError("No hay órdenes en cola.");
        else MostrarExito($"Preparando orden #{orden.Id} (mesa {orden.Mesa?.Numero}).");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLista(int id)
    {
        try
        {
            await _ordenes.CambiarEstadoAsync(id, EstadoOrden.Lista);
            MostrarExito($"Orden #{id} lista para entregar.");
        }
        catch (ReglaNegocioException ex)
        {
            MostrarError(ex.Message);
        }
        return RedirectToAction(nameof(Index));
    }
}
