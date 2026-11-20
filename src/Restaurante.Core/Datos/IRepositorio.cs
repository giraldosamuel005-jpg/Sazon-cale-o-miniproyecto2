using System.Linq.Expressions;
using Restaurante.Core.Dominio;

namespace Restaurante.Core.Datos;

/// <summary>
/// Repositorio genérico: operaciones CRUD comunes a cualquier entidad.
/// La restricción "where T : EntidadBase" garantiza que T tiene Id.
/// </summary>
public interface IRepositorio<T> where T : EntidadBase
{
    Task<List<T>> ObtenerTodosAsync();
    Task<T?> ObtenerPorIdAsync(int id);
    Task AgregarAsync(T entidad);
    Task ActualizarAsync(T entidad);
    Task EliminarAsync(int id);
    Task<bool> ExisteAsync(Expression<Func<T, bool>> condicion);
}
