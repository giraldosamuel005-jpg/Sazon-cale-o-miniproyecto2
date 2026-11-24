// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

namespace Restaurante.Core.Dominio;

/// <summary>Una línea de la orden: producto + cantidad + precio congelado al momento de pedir.</summary>
public class DetalleOrden : EntidadBase
{
    public int OrdenId { get; set; }
    public Orden? Orden { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; }

    /// <summary>
    /// Se copia el precio del producto al crear la línea para que un cambio
    /// posterior en el menú no altere órdenes ya registradas.
    /// </summary>
    public decimal PrecioUnitario { get; set; }

    public string? Nota { get; set; }

    public decimal Subtotal => PrecioUnitario * Cantidad;
}
