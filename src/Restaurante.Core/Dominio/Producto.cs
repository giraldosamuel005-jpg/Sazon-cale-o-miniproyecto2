// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using System.ComponentModel.DataAnnotations;

namespace Restaurante.Core.Dominio;

public class Producto : EntidadBase
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(100, 1_000_000, ErrorMessage = "El precio debe estar entre $100 y $1.000.000")]
    public decimal Precio { get; set; }

    [Display(Name = "Categoría")]
    public CategoriaProducto Categoria { get; set; }

    public bool Disponible { get; set; } = true;

    public override string ToString() => $"{Nombre} - {Precio:C0}";
}
