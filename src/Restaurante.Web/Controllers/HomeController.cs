using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Core.Seguridad;
using Restaurante.Core.Servicios;
using Restaurante.Web.Models;

namespace Restaurante.Web.Controllers;

public class HomeController : BaseController
{
    private readonly ServicioReportes _reportes;
    private readonly ServicioMesas _mesas;
    private readonly ServicioOrdenes _ordenes;

    public HomeController(ServicioReportes reportes, ServicioMesas mesas, ServicioOrdenes ordenes)
    {
        _reportes = reportes;
        _mesas = mesas;
        _ordenes = ordenes;
    }

    /// <summary>Cada rol aterriza en su pantalla principal; el administrador ve el tablero.</summary>
    public async Task<IActionResult> Index()
    {
        if (!User.IsInRole(Roles.Admin))
        {
            if (User.IsInRole(Roles.Cocina)) return RedirectToAction("Index", "Cocina");
            if (User.IsInRole(Roles.Mesero)) return RedirectToAction("Index", "Ordenes");
        }

        var modelo = new DashboardViewModel
        {
            ResumenHoy = await _reportes.ResumenDelDiaAsync(DateTime.Today),
            Mesas = await _mesas.ObtenerTodasAsync(),
            OrdenesActivas = await _ordenes.ObtenerActivasAsync(),
            VentasSemana = await _reportes.VentasPorDiaAsync(7),
            TopProductos = await _reportes.ProductosMasVendidosAsync(DateTime.Today.AddDays(-6), DateTime.Today.AddDays(1))
        };
        return View(modelo);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
