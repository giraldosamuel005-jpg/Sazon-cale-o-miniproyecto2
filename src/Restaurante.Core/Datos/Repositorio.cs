using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Dominio;

namespace Restaurante.Core.Datos;

public class Repositorio<T> : IRepositorio<T> where T : EntidadBase
{
    protected readonly RestauranteDbContext Contexto;
    protected readonly DbSet<T> Tabla;

    public Repositorio(RestauranteDbContext contexto)
    {
        Contexto = contexto;
        Tabla = contexto.Set<T>();
    }

    public virtual Task<List<T>> ObtenerTodosAsync() => Tabla.AsNoTracking().ToListAsync();

    public virtual Task<T?> ObtenerPorIdAsync(int id) => Tabla.FirstOrDefaultAsync(e => e.Id == id);

    public async Task AgregarAsync(T entidad)
    {
        Tabla.Add(entidad);
        await Contexto.SaveChangesAsync();
    }

    public async Task ActualizarAsync(T entidad)
    {
        Tabla.Update(entidad);
        await Contexto.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await Tabla.FindAsync(id);
        if (entidad is null) return;
        Tabla.Remove(entidad);
        await Contexto.SaveChangesAsync();
    }

    public Task<bool> ExisteAsync(Expression<Func<T, bool>> condicion) => Tabla.AnyAsync(condicion);
}
