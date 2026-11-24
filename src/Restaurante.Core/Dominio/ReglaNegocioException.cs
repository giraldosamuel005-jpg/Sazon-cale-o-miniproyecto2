// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

namespace Restaurante.Core.Dominio;

/// <summary>
/// Excepción propia del dominio: se lanza cuando una operación viola una regla
/// del negocio (ej. cobrar una orden que no ha sido entregada).
/// La web y el escritorio la capturan para mostrar el mensaje al usuario.
/// </summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}
