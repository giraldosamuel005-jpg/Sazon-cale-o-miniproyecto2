using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Datos;
using Restaurante.Core.Dominio;

namespace Restaurante.Core.Servicios;

public class ServicioMenu
{
    private readonly RestauranteDbContext _db;
    private readonly IRepositorio<Producto> _productos;

    public ServicioMenu(RestauranteDbContext db)
    {
        _db = db;
        _productos = new Repositorio<Producto>(db);
    }

    public Task<List<Producto>> ObtenerProductosAsync(bool soloDisponibles = false) =>
        _db.Productos.AsNoTracking()
           .Where(p => !soloDisponibles || p.Disponible)
           .OrderBy(p => p.Categoria).ThenBy(p => p.Nombre)
           .ToListAsync();

    /// <summary>
    /// Menú agrupado por categoría. SortedDictionary mantiene las llaves ordenadas
    /// según el valor del enum (Entradas, Platos fuertes, Bebidas, Postres), que es
    /// el orden en que se muestra la carta, sin tener que reordenar en cada vista.
    /// </summary>
    public async Task<SortedDictionary<CategoriaProducto, List<Producto>>> ObtenerMenuPorCategoriaAsync()
    {
        var productos = await ObtenerProductosAsync(soloDisponibles: true);
        var menu = new SortedDictionary<CategoriaProducto, List<Producto>>();

        foreach (var producto in productos)
        {
            if (!menu.TryGetValue(producto.Categoria, out var lista))
                menu[producto.Categoria] = lista = new List<Producto>();
            lista.Add(producto);
        }
        return menu;
    }

    /// <summary>
    /// Índice Id -> Producto. Al armar una orden se busca cada producto por Id:
    /// con Dictionary cada búsqueda es O(1) en vez de recorrer la lista (O(n)).
    /// </summary>
    public async Task<Dictionary<int, Producto>> ObtenerIndiceAsync() =>
        await _db.Productos.Where(p => p.Disponible).ToDictionaryAsync(p => p.Id);

    public Task<Producto?> ObtenerAsync(int id) => _productos.ObtenerPorIdAsync(id);

    public async Task CrearAsync(Producto producto)
    {
        await ValidarNombreUnicoAsync(producto);
        await _productos.AgregarAsync(producto);
    }

    public async Task ActualizarAsync(Producto producto)
    {
        await ValidarNombreUnicoAsync(producto);
        await _productos.ActualizarAsync(producto);
    }

    public async Task EliminarAsync(int id)
    {
        // Si el producto ya se vendió no se borra (dañaría el historial): se desactiva.
        bool usado = await _db.DetallesOrden.AnyAsync(d => d.ProductoId == id);
        if (usado)
        {
            var producto = await _productos.ObtenerPorIdAsync(id)
                ?? throw new ReglaNegocioException("El producto no existe.");
            producto.Disponible = false;
            await _db.SaveChangesAsync();
            throw new ReglaNegocioException(
                $"'{producto.Nombre}' tiene ventas registradas; se marcó como no disponible en lugar de eliminarlo.");
        }
        await _productos.EliminarAsync(id);
    }

    private async Task ValidarNombreUnicoAsync(Producto producto)
    {
        var nombre = producto.Nombre.Trim();
        if (await _productos.ExisteAsync(p => p.Nombre == nombre && p.Id != producto.Id))
            throw new ReglaNegocioException($"Ya existe un producto llamado '{nombre}'.");
        producto.Nombre = nombre;
    }
}
