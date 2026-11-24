// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

namespace Restaurante.Core.Dominio;

/// <summary>
/// Clase base abstracta de todas las entidades persistidas.
/// Centraliza la llave primaria y la fecha de creación (herencia).
/// </summary>
public abstract class EntidadBase
{
    public int Id { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.Now;
}
