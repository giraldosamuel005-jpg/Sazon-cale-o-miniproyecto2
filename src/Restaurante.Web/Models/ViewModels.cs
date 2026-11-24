// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using System.ComponentModel.DataAnnotations;
using Restaurante.Core.Dominio;
using Restaurante.Core.Servicios;

namespace Restaurante.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su correo")]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ItemFormulario
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public CategoriaProducto Categoria { get; set; }

    [Range(0, 50)]
    public int Cantidad { get; set; }
}

public class CrearOrdenViewModel
{
    [Required(ErrorMessage = "Seleccione una mesa")]
    [Display(Name = "Mesa")]
    public int? MesaId { get; set; }

    [StringLength(200)]
    [Display(Name = "Notas para cocina")]
    public string? Notas { get; set; }

    public List<ItemFormulario> Items { get; set; } = new();

    // Solo para mostrar
    public List<Mesa> MesasLibres { get; set; } = new();
}

public class CobrarViewModel
{
    public Orden Orden { get; set; } = null!;
    public MetodoPago MetodoPago { get; set; }
    public bool IncluirPropina { get; set; } = true;

    public decimal Impuesto => Math.Round(Orden.Total * Factura.TasaImpoconsumo, 0);
    public decimal Propina => Math.Round(Orden.Total * Factura.TasaPropina, 0);
}

public class DashboardViewModel
{
    public ResumenVentas ResumenHoy { get; set; } = null!;
    public List<Mesa> Mesas { get; set; } = new();
    public List<Orden> OrdenesActivas { get; set; } = new();
    public SortedDictionary<DateTime, decimal> VentasSemana { get; set; } = new();
    public List<ProductoVendido> TopProductos { get; set; } = new();
}

public class ReporteViewModel
{
    [DataType(DataType.Date)] public DateTime Desde { get; set; }
    [DataType(DataType.Date)] public DateTime Hasta { get; set; }
    public ResumenVentas Resumen { get; set; } = null!;
    public List<ProductoVendido> TopProductos { get; set; } = new();
    public List<Factura> Facturas { get; set; } = new();
}

public class UsuarioListadoViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class CrearUsuarioViewModel
{
    [Required, EmailAddress, Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(60), Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;

    [Required, Display(Name = "Rol")]
    public string Rol { get; set; } = string.Empty;
}
