using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Core.Seguridad;
using Restaurante.Core.Servicios;
using Restaurante.Web.Models;

namespace Restaurante.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class ReportesController : BaseController
{
    private readonly ServicioReportes _reportes;
    private readonly ServicioFacturacion _facturacion;

    public ReportesController(ServicioReportes reportes, ServicioFacturacion facturacion)
    {
        _reportes = reportes;
        _facturacion = facturacion;
    }

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var inicio = (desde ?? DateTime.Today.AddDays(-6)).Date;
        var fin = (hasta ?? DateTime.Today).Date;
        if (fin < inicio) (inicio, fin) = (fin, inicio); // Tupla para intercambiar valores.

        var finExclusivo = fin.AddDays(1);
        return View(new ReporteViewModel
        {
            Desde = inicio,
            Hasta = fin,
            Resumen = await _reportes.ResumenAsync(inicio, finExclusivo),
            TopProductos = await _reportes.ProductosMasVendidosAsync(inicio, finExclusivo, top: 10),
            Facturas = await _facturacion.ObtenerEntreAsync(inicio, finExclusivo)
        });
    }
}
