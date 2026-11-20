namespace Restaurante.Core.Dominio;

/// <summary>
/// Orden de una mesa. Encapsula sus reglas: qué transiciones de estado son válidas,
/// cuándo se pueden agregar productos y cómo se calcula el total.
/// </summary>
public class Orden : EntidadBase
{
    // Máquina de estados: para cada estado, el conjunto de estados a los que puede pasar.
    // Dictionary  -> búsqueda O(1) por el estado actual.
    // HashSet     -> pregunta "¿esta transición está permitida?" en O(1) y sin duplicados.
    private static readonly Dictionary<EstadoOrden, HashSet<EstadoOrden>> Transiciones = new()
    {
        [EstadoOrden.Pendiente]     = new() { EstadoOrden.EnPreparacion, EstadoOrden.Cancelada },
        [EstadoOrden.EnPreparacion] = new() { EstadoOrden.Lista },
        [EstadoOrden.Lista]         = new() { EstadoOrden.Entregada },
        [EstadoOrden.Entregada]     = new() { EstadoOrden.Pagada },
        [EstadoOrden.Pagada]        = new(),
        [EstadoOrden.Cancelada]     = new()
    };

    public int MesaId { get; set; }
    public Mesa? Mesa { get; set; }

    /// <summary>Id del usuario de Identity (mesero) que tomó la orden.</summary>
    public string MeseroId { get; set; } = string.Empty;
    public string MeseroNombre { get; set; } = string.Empty;

    public EstadoOrden Estado { get; set; } = EstadoOrden.Pendiente;
    public string? Notas { get; set; }
    public DateTime? ActualizadoEn { get; set; }

    // List: las líneas se recorren en el orden en que se pidieron y son pocas (< 30).
    public List<DetalleOrden> Detalles { get; set; } = new();

    public Factura? Factura { get; set; }

    public decimal Total => Detalles.Sum(d => d.Subtotal);
    public int CantidadItems => Detalles.Sum(d => d.Cantidad);
    public bool EstaAbierta => Estado is not (EstadoOrden.Pagada or EstadoOrden.Cancelada);

    public bool PuedeCambiarA(EstadoOrden nuevo) => Transiciones[Estado].Contains(nuevo);

    public IReadOnlyCollection<EstadoOrden> SiguientesEstados() => Transiciones[Estado];

    public void CambiarEstado(EstadoOrden nuevo)
    {
        if (!PuedeCambiarA(nuevo))
            throw new ReglaNegocioException(
                $"La orden #{Id} no puede pasar de {Estado.Nombre()} a {nuevo.Nombre()}.");

        Estado = nuevo;
        ActualizadoEn = DateTime.Now;
    }

    public void AgregarProducto(Producto producto, int cantidad, string? nota = null)
    {
        if (Estado != EstadoOrden.Pendiente)
            throw new ReglaNegocioException("Solo se pueden agregar productos a una orden pendiente.");
        if (!producto.Disponible)
            throw new ReglaNegocioException($"'{producto.Nombre}' no está disponible.");
        if (cantidad <= 0)
            throw new ReglaNegocioException("La cantidad debe ser mayor que cero.");

        // Si el producto ya está en la orden (y sin nota distinta) se suma la cantidad.
        var existente = Detalles.FirstOrDefault(d => d.ProductoId == producto.Id && d.Nota == nota);
        if (existente is not null)
        {
            existente.Cantidad += cantidad;
            return;
        }

        Detalles.Add(new DetalleOrden
        {
            ProductoId = producto.Id,
            Producto = producto,
            Cantidad = cantidad,
            PrecioUnitario = producto.Precio,
            Nota = nota
        });
    }

    /// <summary>Minutos que lleva la orden desde que se creó (útil para cocina).</summary>
    public int MinutosEspera => (int)(DateTime.Now - CreadoEn).TotalMinutes;
}
