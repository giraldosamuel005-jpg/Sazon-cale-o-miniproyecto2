using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Datos;
using Restaurante.Core.Dominio;

namespace Restaurante.Core.Servicios;

public record ResumenVentas(
    int CantidadFacturas,
    decimal Ventas,
    decimal Impuestos,
    decimal Propinas,
    decimal TicketPromedio,
    Dictionary<MetodoPago, decimal> PorMetodoPago);

public record ProductoVendido(string Nombre, CategoriaProducto Categoria, int Cantidad, decimal Total);

public class ServicioReportes
{
    private readonly RestauranteDbContext _db;

    public ServicioReportes(RestauranteDbContext db) => _db = db;

    public async Task<ResumenVentas> ResumenAsync(DateTime desde, DateTime hasta)
    {
        // Se traen las facturas del rango y se agregan con LINQ to Objects.
        var facturas = await _db.Facturas.AsNoTracking()
            .Where(f => f.CreadoEn >= desde && f.CreadoEn < hasta)
            .ToListAsync();

        // Dictionary: total recaudado por cada método de pago (acceso O(1) por llave).
        var porMetodo = Enum.GetValues<MetodoPago>().ToDictionary(m => m, _ => 0m);
        foreach (var f in facturas)
            porMetodo[f.MetodoPago] += f.Total;

        var ventas = facturas.Sum(f => f.Total);
        return new ResumenVentas(
            facturas.Count,
            ventas,
            facturas.Sum(f => f.Impuesto),
            facturas.Sum(f => f.Propina),
            facturas.Count == 0 ? 0 : Math.Round(ventas / facturas.Count, 0),
            porMetodo);
    }

    public Task<ResumenVentas> ResumenDelDiaAsync(DateTime dia) =>
        ResumenAsync(dia.Date, dia.Date.AddDays(1));

    /// <summary>Ranking de productos vendidos (órdenes pagadas) en el rango.</summary>
    public async Task<List<ProductoVendido>> ProductosMasVendidosAsync(DateTime desde, DateTime hasta, int top = 5)
    {
        var detalles = await _db.DetallesOrden.AsNoTracking()
            .Include(d => d.Producto)
            .Where(d => d.Orden!.Estado == EstadoOrden.Pagada
                     && d.Orden.CreadoEn >= desde && d.Orden.CreadoEn < hasta)
            .ToListAsync();

        // Se agrupa por la llave (ProductoId) y no por el objeto Producto: con AsNoTracking
        // cada detalle trae su propia instancia y agrupar por referencia no las uniría.
        return detalles
            .GroupBy(d => d.ProductoId)
            .Select(g => new ProductoVendido(g.First().Producto!.Nombre, g.First().Producto!.Categoria,
                                             g.Sum(d => d.Cantidad), g.Sum(d => d.Subtotal)))
            .OrderByDescending(p => p.Cantidad)
            .ThenByDescending(p => p.Total)
            .Take(top)
            .ToList();
    }

    /// <summary>
    /// Ventas por día. SortedDictionary deja los días ordenados cronológicamente
    /// para graficar, incluyendo los días sin ventas (valor 0).
    /// </summary>
    public async Task<SortedDictionary<DateTime, decimal>> VentasPorDiaAsync(int dias = 7)
    {
        var desde = DateTime.Today.AddDays(-(dias - 1));
        var facturas = await _db.Facturas.AsNoTracking()
            .Where(f => f.CreadoEn >= desde)
            .ToListAsync();

        var resultado = new SortedDictionary<DateTime, decimal>();
        for (int i = 0; i < dias; i++)
            resultado[desde.AddDays(i)] = 0m;

        foreach (var f in facturas)
            resultado[f.CreadoEn.Date] += f.Total;

        return resultado;
    }
}
