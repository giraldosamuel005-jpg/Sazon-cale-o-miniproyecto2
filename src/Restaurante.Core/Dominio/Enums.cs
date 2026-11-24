// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

namespace Restaurante.Core.Dominio;

/// <summary>Ciclo de vida de una orden dentro del restaurante.</summary>
public enum EstadoOrden
{
    Pendiente = 0,      // El mesero la registró; espera turno en la cola de cocina.
    EnPreparacion = 1,  // Cocina la tomó de la cola.
    Lista = 2,          // Cocina la terminó; el mesero debe llevarla a la mesa.
    Entregada = 3,      // Está en la mesa, pendiente de cobro.
    Pagada = 4,         // Se generó la factura.
    Cancelada = 5
}

public enum EstadoMesa
{
    Libre = 0,
    Ocupada = 1
}

public enum CategoriaProducto
{
    Entrada = 0,
    PlatoFuerte = 1,
    Bebida = 2,
    Postre = 3
}

public enum MetodoPago
{
    Efectivo = 0,
    Tarjeta = 1,
    Transferencia = 2
}
