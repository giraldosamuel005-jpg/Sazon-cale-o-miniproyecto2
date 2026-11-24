// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using System.Globalization;

namespace Restaurante.Core.Dominio;

/// <summary>Métodos de extensión para mostrar enums y valores en español.</summary>
public static class Extensiones
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    public static string Nombre(this EstadoOrden estado) => estado switch
    {
        EstadoOrden.Pendiente => "Pendiente",
        EstadoOrden.EnPreparacion => "En preparación",
        EstadoOrden.Lista => "Lista",
        EstadoOrden.Entregada => "Entregada",
        EstadoOrden.Pagada => "Pagada",
        EstadoOrden.Cancelada => "Cancelada",
        _ => estado.ToString()
    };

    public static string Nombre(this CategoriaProducto categoria) => categoria switch
    {
        CategoriaProducto.Entrada => "Entradas",
        CategoriaProducto.PlatoFuerte => "Platos fuertes",
        CategoriaProducto.Bebida => "Bebidas",
        CategoriaProducto.Postre => "Postres",
        _ => categoria.ToString()
    };

    public static string Nombre(this MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Transferencia => "Transferencia",
        _ => metodo.ToString()
    };

    /// <summary>Clase CSS de Bootstrap según el estado (la usa la web).</summary>
    public static string ColorBootstrap(this EstadoOrden estado) => estado switch
    {
        EstadoOrden.Pendiente => "warning",
        EstadoOrden.EnPreparacion => "info",
        EstadoOrden.Lista => "success",
        EstadoOrden.Entregada => "primary",
        EstadoOrden.Pagada => "secondary",
        EstadoOrden.Cancelada => "danger",
        _ => "light"
    };

    /// <summary>Formato de pesos colombianos: $ 25.000</summary>
    public static string Pesos(this decimal valor) => valor.ToString("C0", Colombia);
}
