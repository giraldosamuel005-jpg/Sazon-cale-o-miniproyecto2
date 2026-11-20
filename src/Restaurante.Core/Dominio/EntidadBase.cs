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
