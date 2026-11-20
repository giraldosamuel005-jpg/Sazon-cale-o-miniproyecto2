using System.ComponentModel.DataAnnotations;

namespace Restaurante.Core.Dominio;

public class Mesa : EntidadBase
{
    [Range(1, 999)]
    [Display(Name = "Número")]
    public int Numero { get; set; }

    [Range(1, 20)]
    public int Capacidad { get; set; } = 4;

    public EstadoMesa Estado { get; set; } = EstadoMesa.Libre;

    public List<Orden> Ordenes { get; set; } = new();

    public void Ocupar() => Estado = EstadoMesa.Ocupada;
    public void Liberar() => Estado = EstadoMesa.Libre;

    public override string ToString() => $"Mesa {Numero} ({Capacidad} pers.)";
}
