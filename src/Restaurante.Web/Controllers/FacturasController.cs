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
public class FacturasController : BaseController
{
    private readonly ServicioFacturacion _facturacion;
    private readonly ServicioOrdenes _ordenes;

    public FacturasController(ServicioFacturacion facturacion, ServicioOrdenes ordenes)
    {
        _facturacion = facturacion;
        _ordenes = ordenes;
    }

    /// <summary>Órdenes entregadas pendientes de pago.</summary>
    public async Task<IActionResult> Index() => View(await _facturacion.ObtenerPorCobrarAsync());

    public async Task<IActionResult> Cobrar(int id)
    {
        var orden = await _ordenes.ObtenerAsync(id);
        if (orden is null) return NotFound();
        if (orden.Estado != EstadoOrden.Entregada)
        {
            MostrarError($"La orden #{id} está {orden.Estado.Nombre().ToLower()}; solo se cobran órdenes entregadas.");
            return RedirectToAction(nameof(Index));
        }
        return View(new CobrarViewModel { Orden = orden });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cobrar(int id, MetodoPago metodoPago, bool incluirPropina)
    {
        try
        {
            var factura = await _facturacion.FacturarAsync(id, metodoPago, incluirPropina, User.NombreVisible());
            MostrarExito($"Factura #{factura.Id} generada por {factura.Total.Pesos()}.");
            return RedirectToAction(nameof(Ver), new { id = factura.Id });
        }
        catch (ReglaNegocioException ex)
        {
            MostrarError(ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Ver(int id)
    {
        var factura = await _facturacion.ObtenerAsync(id);
        return factura is null ? NotFound() : View(factura);
    }
}
