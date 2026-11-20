namespace Restaurante.Core.Dominio;

public class Factura : EntidadBase
{
    /// <summary>Impuesto al consumo de restaurantes en Colombia (8 %).</summary>
    public const decimal TasaImpoconsumo = 0.08m;

    /// <summary>Propina sugerida (voluntaria, 10 %).</summary>
    public const decimal TasaPropina = 0.10m;

    public int OrdenId { get; set; }
    public Orden? Orden { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Propina { get; set; }
    public decimal Total { get; set; }

    public MetodoPago MetodoPago { get; set; }
    public string CajeroNombre { get; set; } = string.Empty;

    /// <summary>Fábrica: crea la factura calculando impuesto y propina a partir de la orden.</summary>
    public static Factura Crear(Orden orden, MetodoPago metodo, bool incluirPropina, string cajero)
    {
        var subtotal = orden.Total;
        var impuesto = Math.Round(subtotal * TasaImpoconsumo, 0);
        var propina = incluirPropina ? Math.Round(subtotal * TasaPropina, 0) : 0m;

        return new Factura
        {
            OrdenId = orden.Id,
            Orden = orden,
            Subtotal = subtotal,
            Impuesto = impuesto,
            Propina = propina,
            Total = subtotal + impuesto + propina,
            MetodoPago = metodo,
            CajeroNombre = cajero
        };
    }
}
