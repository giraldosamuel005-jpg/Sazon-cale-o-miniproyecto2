namespace Restaurante.Core.Servicios;

/// <summary>
/// Dato de entrada para crear una orden. Es un record: inmutable y con igualdad por valor,
/// ideal para transportar datos entre la interfaz y la lógica de negocio.
/// </summary>
public record ItemPedido(int ProductoId, int Cantidad, string? Nota = null);
