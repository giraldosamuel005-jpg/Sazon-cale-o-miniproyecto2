using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Dominio;
using Restaurante.Core.Seguridad;

namespace Restaurante.Core.Datos;

/// <summary>
/// Aplica las migraciones pendientes y carga datos iniciales (roles, usuarios de prueba,
/// mesas, menú y algunas ventas de ejemplo para los reportes).
/// Se ejecuta al arrancar la web o el escritorio; si los datos ya existen no hace nada.
/// </summary>
public static class InicializadorDatos
{
    /// <summary>Usuarios de prueba (correo, contraseña, nombre, rol). Documentados en el README.</summary>
    public static readonly (string Email, string Contrasena, string Nombre, string Rol)[] UsuariosPrueba =
    {
        ("admin@restaurante.com",  "Admin123*",  "Administrador", Roles.Admin),
        ("mesero@restaurante.com", "Mesero123*", "Carlos Mesero", Roles.Mesero),
        ("mesera@restaurante.com", "Mesero123*", "Laura Mesera",  Roles.Mesero),
        ("cocina@restaurante.com", "Cocina123*", "Chef Cocina",   Roles.Cocina),
    };

    public static async Task InicializarAsync(RestauranteDbContext db)
    {
        // PostgreSQL (nube): migraciones de EF Core. SQLite (modo local sin internet): el esquema
        // se crea directamente desde el modelo, porque las migraciones están generadas para PostgreSQL.
        if (db.Database.IsNpgsql())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();
        await SembrarSeguridadAsync(db);
        await SembrarMesasAsync(db);
        await SembrarMenuAsync(db);
        await SembrarVentasEjemploAsync(db);
    }

    private static async Task SembrarSeguridadAsync(RestauranteDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        var roles = Roles.Todos.ToDictionary(
            nombre => nombre,
            nombre => new IdentityRole(nombre) { NormalizedName = nombre.ToUpperInvariant() });
        db.Roles.AddRange(roles.Values);

        var hasher = new PasswordHasher<Usuario>();
        foreach (var (email, contrasena, nombre, rol) in UsuariosPrueba)
        {
            var usuario = new Usuario
            {
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                NombreCompleto = nombre,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            usuario.PasswordHash = hasher.HashPassword(usuario, contrasena);
            db.Users.Add(usuario);
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = usuario.Id, RoleId = roles[rol].Id });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SembrarMesasAsync(RestauranteDbContext db)
    {
        if (await db.Mesas.AnyAsync()) return;

        for (int numero = 1; numero <= 10; numero++)
            db.Mesas.Add(new Mesa { Numero = numero, Capacidad = numero <= 6 ? 4 : 6 });

        await db.SaveChangesAsync();
    }

    private static async Task SembrarMenuAsync(RestauranteDbContext db)
    {
        if (await db.Productos.AnyAsync()) return;

        db.Productos.AddRange(
            new Producto { Nombre = "Empanadas (3 und)", Precio = 9000, Categoria = CategoriaProducto.Entrada, Descripcion = "Empanadas de pipián con ají casero" },
            new Producto { Nombre = "Patacón con hogao", Precio = 11000, Categoria = CategoriaProducto.Entrada, Descripcion = "Patacón crocante con hogao y queso" },
            new Producto { Nombre = "Arepa de choclo", Precio = 8500, Categoria = CategoriaProducto.Entrada, Descripcion = "Con queso campesino" },
            new Producto { Nombre = "Bandeja paisa", Precio = 32000, Categoria = CategoriaProducto.PlatoFuerte, Descripcion = "Frijoles, arroz, chicharrón, carne molida, huevo, maduro y arepa" },
            new Producto { Nombre = "Sancocho de gallina", Precio = 28000, Categoria = CategoriaProducto.PlatoFuerte, Descripcion = "Con arroz, aguacate y plátano" },
            new Producto { Nombre = "Churrasco", Precio = 38000, Categoria = CategoriaProducto.PlatoFuerte, Descripcion = "300 g con papa a la francesa y ensalada" },
            new Producto { Nombre = "Mojarra frita", Precio = 34000, Categoria = CategoriaProducto.PlatoFuerte, Descripcion = "Con patacón, arroz de coco y ensalada" },
            new Producto { Nombre = "Arroz con pollo", Precio = 24000, Categoria = CategoriaProducto.PlatoFuerte, Descripcion = "Con papas fritas y ensalada" },
            new Producto { Nombre = "Limonada de coco", Precio = 9000, Categoria = CategoriaProducto.Bebida },
            new Producto { Nombre = "Jugo natural", Precio = 7000, Categoria = CategoriaProducto.Bebida, Descripcion = "Mora, lulo, maracuyá o mango" },
            new Producto { Nombre = "Gaseosa", Precio = 5000, Categoria = CategoriaProducto.Bebida },
            new Producto { Nombre = "Cerveza nacional", Precio = 7500, Categoria = CategoriaProducto.Bebida },
            new Producto { Nombre = "Postre de natas", Precio = 9500, Categoria = CategoriaProducto.Postre },
            new Producto { Nombre = "Brownie con helado", Precio = 12000, Categoria = CategoriaProducto.Postre },
            new Producto { Nombre = "Cholado", Precio = 11000, Categoria = CategoriaProducto.Postre, Descripcion = "El clásico de Cali" }
        );
        await db.SaveChangesAsync();
    }

    /// <summary>Genera órdenes pagadas de los últimos 6 días para que los reportes tengan datos.</summary>
    private static async Task SembrarVentasEjemploAsync(RestauranteDbContext db)
    {
        if (await db.Facturas.AnyAsync()) return;

        var mesero = await db.Users.FirstAsync(u => u.Email == "mesero@restaurante.com");
        var mesas = await db.Mesas.ToListAsync();
        var productos = await db.Productos.ToListAsync();
        var azar = new Random(2026); // Semilla fija: siempre genera los mismos datos.

        for (int dia = 6; dia >= 1; dia--)
        {
            int ordenesDelDia = azar.Next(4, 9);
            for (int i = 0; i < ordenesDelDia; i++)
            {
                var fecha = DateTime.Today.AddDays(-dia).AddHours(12 + azar.Next(0, 9)).AddMinutes(azar.Next(0, 60));
                var orden = new Orden
                {
                    Mesa = mesas[azar.Next(mesas.Count)],
                    MeseroId = mesero.Id,
                    MeseroNombre = mesero.NombreCompleto,
                    CreadoEn = fecha
                };
                foreach (var producto in productos.OrderBy(_ => azar.Next()).Take(azar.Next(2, 5)))
                    orden.AgregarProducto(producto, azar.Next(1, 3));

                // Recorre el flujo real de estados hasta Pagada.
                orden.CambiarEstado(EstadoOrden.EnPreparacion);
                orden.CambiarEstado(EstadoOrden.Lista);
                orden.CambiarEstado(EstadoOrden.Entregada);
                orden.CambiarEstado(EstadoOrden.Pagada);
                orden.ActualizadoEn = fecha.AddMinutes(50);

                var factura = Factura.Crear(orden, (MetodoPago)azar.Next(0, 3), azar.Next(0, 2) == 1, "Administrador");
                factura.CreadoEn = fecha.AddMinutes(55);
                db.Ordenes.Add(orden);
                db.Facturas.Add(factura);
            }
        }
        await db.SaveChangesAsync();
    }
}
