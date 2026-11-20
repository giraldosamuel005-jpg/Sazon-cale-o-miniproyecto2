using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Datos;
using Restaurante.Core.Dominio;

namespace Restaurante.Core.Servicios;

/// <summary>Argumentos del evento que se dispara cada vez que una orden cambia de estado.</summary>
public class CambioEstadoEventArgs : EventArgs
{
    public required int OrdenId { get; init; }
    public required int MesaNumero { get; init; }
    public required EstadoOrden Anterior { get; init; }
    public required EstadoOrden Nuevo { get; init; }
}

public class ServicioOrdenes
{
    private readonly RestauranteDbContext _db;

    /// <summary>Evento (delegado) para que la interfaz reaccione a los cambios de estado.</summary>
    public event EventHandler<CambioEstadoEventArgs>? EstadoCambiado;

    public ServicioOrdenes(RestauranteDbContext db) => _db = db;

    private IQueryable<Orden> OrdenesCompletas() =>
        _db.Ordenes
           .Include(o => o.Mesa)
           .Include(o => o.Detalles).ThenInclude(d => d.Producto)
           .Include(o => o.Factura);

    public Task<Orden?> ObtenerAsync(int id) => OrdenesCompletas().FirstOrDefaultAsync(o => o.Id == id);

    public Task<List<Orden>> ObtenerActivasAsync() =>
        OrdenesCompletas()
            .Where(o => o.Estado != EstadoOrden.Pagada && o.Estado != EstadoOrden.Cancelada)
            .OrderBy(o => o.CreadoEn)
            .ToListAsync();

    public Task<List<Orden>> ObtenerPorEstadoAsync(EstadoOrden estado) =>
        OrdenesCompletas().Where(o => o.Estado == estado).OrderBy(o => o.CreadoEn).ToListAsync();

    public Task<List<Orden>> ObtenerHistorialAsync(int maximo = 50) =>
        OrdenesCompletas()
            .Where(o => o.Estado == EstadoOrden.Pagada || o.Estado == EstadoOrden.Cancelada)
            .OrderByDescending(o => o.CreadoEn)
            .Take(maximo)
            .ToListAsync();

    /// <summary>
    /// Cola de cocina: órdenes pendientes en orden de llegada.
    /// Queue (FIFO) modela exactamente la regla "la primera orden en llegar es la primera
    /// en prepararse": Peek() muestra la siguiente y Dequeue() la saca en O(1).
    /// </summary>
    public async Task<Queue<Orden>> ObtenerColaCocinaAsync()
    {
        var pendientes = await ObtenerPorEstadoAsync(EstadoOrden.Pendiente);
        return new Queue<Orden>(pendientes);
    }

    /// <summary>Cocina toma la siguiente orden de la cola (la más antigua).</summary>
    public async Task<Orden?> TomarSiguienteAsync()
    {
        var cola = await ObtenerColaCocinaAsync();
        if (cola.Count == 0) return null;

        var siguiente = cola.Dequeue();
        await CambiarEstadoAsync(siguiente.Id, EstadoOrden.EnPreparacion);
        return siguiente;
    }

    public async Task<Orden> CrearAsync(int mesaId, string meseroId, string meseroNombre,
                                        IEnumerable<ItemPedido> items, string? notas = null)
    {
        var pedidos = items.Where(i => i.Cantidad > 0).ToList();
        if (pedidos.Count == 0)
            throw new ReglaNegocioException("La orden debe tener al menos un producto.");

        var mesa = await _db.Mesas.FindAsync(mesaId)
            ?? throw new ReglaNegocioException("La mesa seleccionada no existe.");

        // HashSet con las mesas que ya tienen una orden abierta: Contains en O(1).
        var mesasConOrdenAbierta = (await _db.Ordenes
                .Where(o => o.Estado != EstadoOrden.Pagada && o.Estado != EstadoOrden.Cancelada)
                .Select(o => o.MesaId)
                .ToListAsync())
            .ToHashSet();

        if (mesasConOrdenAbierta.Contains(mesaId))
            throw new ReglaNegocioException($"La mesa {mesa.Numero} ya tiene una orden abierta.");

        var menu = await new ServicioMenu(_db).ObtenerIndiceAsync(); // Dictionary<int, Producto>

        var orden = new Orden
        {
            MesaId = mesa.Id,
            Mesa = mesa,
            MeseroId = meseroId,
            MeseroNombre = meseroNombre,
            Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim()
        };

        foreach (var item in pedidos)
        {
            if (!menu.TryGetValue(item.ProductoId, out var producto))
                throw new ReglaNegocioException("Uno de los productos ya no está disponible.");
            orden.AgregarProducto(producto, item.Cantidad, item.Nota);
        }

        mesa.Ocupar();
        _db.Ordenes.Add(orden);
        await _db.SaveChangesAsync();
        return orden;
    }

    public async Task CambiarEstadoAsync(int ordenId, EstadoOrden nuevo)
    {
        var orden = await _db.Ordenes.Include(o => o.Mesa).FirstOrDefaultAsync(o => o.Id == ordenId)
            ?? throw new ReglaNegocioException($"La orden #{ordenId} no existe.");

        var anterior = orden.Estado;
        orden.CambiarEstado(nuevo); // Valida la transición en el dominio.

        if (nuevo == EstadoOrden.Cancelada)
            orden.Mesa?.Liberar();

        await _db.SaveChangesAsync();

        EstadoCambiado?.Invoke(this, new CambioEstadoEventArgs
        {
            OrdenId = orden.Id,
            MesaNumero = orden.Mesa?.Numero ?? 0,
            Anterior = anterior,
            Nuevo = nuevo
        });
    }
}
