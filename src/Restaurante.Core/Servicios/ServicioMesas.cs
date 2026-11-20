using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Datos;
using Restaurante.Core.Dominio;

namespace Restaurante.Core.Servicios;

public class ServicioMesas
{
    private readonly RestauranteDbContext _db;
    private readonly IRepositorio<Mesa> _mesas;

    public ServicioMesas(RestauranteDbContext db)
    {
        _db = db;
        _mesas = new Repositorio<Mesa>(db);
    }

    public Task<List<Mesa>> ObtenerTodasAsync() =>
        _db.Mesas.AsNoTracking().OrderBy(m => m.Numero).ToListAsync();

    public Task<List<Mesa>> ObtenerLibresAsync() =>
        _db.Mesas.AsNoTracking().Where(m => m.Estado == EstadoMesa.Libre).OrderBy(m => m.Numero).ToListAsync();

    public Task<Mesa?> ObtenerAsync(int id) => _mesas.ObtenerPorIdAsync(id);

    public async Task CrearAsync(Mesa mesa)
    {
        if (await _mesas.ExisteAsync(m => m.Numero == mesa.Numero))
            throw new ReglaNegocioException($"Ya existe la mesa {mesa.Numero}.");
        mesa.Estado = EstadoMesa.Libre;
        await _mesas.AgregarAsync(mesa);
    }

    public async Task ActualizarAsync(Mesa mesa)
    {
        if (await _mesas.ExisteAsync(m => m.Numero == mesa.Numero && m.Id != mesa.Id))
            throw new ReglaNegocioException($"Ya existe la mesa {mesa.Numero}.");
        await _mesas.ActualizarAsync(mesa);
    }

    public async Task EliminarAsync(int id)
    {
        if (await _db.Ordenes.AnyAsync(o => o.MesaId == id))
            throw new ReglaNegocioException("La mesa tiene órdenes registradas y no se puede eliminar.");
        await _mesas.EliminarAsync(id);
    }
}
