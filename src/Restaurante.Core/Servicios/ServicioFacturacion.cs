// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Datos;
using Restaurante.Core.Dominio;

namespace Restaurante.Core.Servicios;

public class ServicioFacturacion
{
    private readonly RestauranteDbContext _db;

    public ServicioFacturacion(RestauranteDbContext db) => _db = db;

    /// <summary>Órdenes entregadas en la mesa que esperan ser cobradas.</summary>
    public Task<List<Orden>> ObtenerPorCobrarAsync() =>
        _db.Ordenes
           .Include(o => o.Mesa)
           .Include(o => o.Detalles).ThenInclude(d => d.Producto)
           .Where(o => o.Estado == EstadoOrden.Entregada)
           .OrderBy(o => o.CreadoEn)
           .ToListAsync();

    /// <summary>
    /// Cobra una orden: valida el estado, crea la factura, marca la orden como pagada
    /// y libera la mesa. Todo se guarda en una sola transacción (SaveChanges).
    /// </summary>
    public async Task<Factura> FacturarAsync(int ordenId, MetodoPago metodo, bool incluirPropina, string cajero)
    {
        var orden = await _db.Ordenes
            .Include(o => o.Mesa)
            .Include(o => o.Detalles).ThenInclude(d => d.Producto)
            .Include(o => o.Factura)
            .FirstOrDefaultAsync(o => o.Id == ordenId)
            ?? throw new ReglaNegocioException($"La orden #{ordenId} no existe.");

        if (orden.Factura is not null)
            throw new ReglaNegocioException($"La orden #{ordenId} ya fue facturada.");

        orden.CambiarEstado(EstadoOrden.Pagada); // Lanza excepción si no estaba Entregada.

        var factura = Factura.Crear(orden, metodo, incluirPropina, cajero);
        _db.Facturas.Add(factura);
        orden.Mesa?.Liberar();

        await _db.SaveChangesAsync();
        return factura;
    }

    public Task<Factura?> ObtenerAsync(int id) =>
        _db.Facturas
           .Include(f => f.Orden).ThenInclude(o => o!.Mesa)
           .Include(f => f.Orden).ThenInclude(o => o!.Detalles).ThenInclude(d => d.Producto)
           .FirstOrDefaultAsync(f => f.Id == id);

    public Task<List<Factura>> ObtenerEntreAsync(DateTime desde, DateTime hasta) =>
        _db.Facturas
           .Include(f => f.Orden).ThenInclude(o => o!.Mesa)
           .Where(f => f.CreadoEn >= desde && f.CreadoEn < hasta)
           .OrderByDescending(f => f.CreadoEn)
           .ToListAsync();
}
