using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Dominio;
using Restaurante.Core.Seguridad;

namespace Restaurante.Core.Datos;

/// <summary>
/// Contexto de Entity Framework Core. Hereda de IdentityDbContext para que las tablas
/// de usuarios y roles (AspNetUsers, AspNetRoles...) vivan en la misma base de datos
/// que el negocio. Lo usan tanto la aplicación web como la de escritorio.
/// </summary>
public class RestauranteDbContext : IdentityDbContext<Usuario>
{
    public RestauranteDbContext(DbContextOptions<RestauranteDbContext> options) : base(options) { }

    public DbSet<Mesa> Mesas => Set<Mesa>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Orden> Ordenes => Set<Orden>();
    public DbSet<DetalleOrden> DetallesOrden => Set<DetalleOrden>();
    public DbSet<Factura> Facturas => Set<Factura>();

    /// <summary>
    /// Crea las opciones del contexto: PostgreSQL en la nube si hay cadena configurada
    /// (ver <see cref="ConexionBaseDatos"/>), o SQLite local si no.
    /// </summary>
    public static DbContextOptions<RestauranteDbContext> CrearOpciones(string? cadenaConexion = null)
    {
        var builder = new DbContextOptionsBuilder<RestauranteDbContext>();
        ConexionBaseDatos.Configurar(builder, ConexionBaseDatos.Resolver(cadenaConexion));
        return builder.Options;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // SQLite no tiene tipo decimal nativo: se guarda como REAL para poder ordenar y comparar.
        // PostgreSQL sí tiene numeric, así que allí se deja el decimal exacto.
        if (Database.IsSqlite())
            builder.Properties<decimal>().HaveConversion<double>();
        else
            builder.Properties<decimal>().HavePrecision(14, 2);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Tablas de Identity

        builder.Entity<Mesa>(e =>
        {
            e.HasIndex(m => m.Numero).IsUnique();
        });

        builder.Entity<Producto>(e =>
        {
            e.Property(p => p.Nombre).HasMaxLength(80).IsRequired();
            e.HasIndex(p => p.Nombre).IsUnique();
        });

        builder.Entity<Orden>(e =>
        {
            e.HasOne(o => o.Mesa)
             .WithMany(m => m.Ordenes)
             .HasForeignKey(o => o.MesaId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(o => o.Detalles)
             .WithOne(d => d.Orden)
             .HasForeignKey(d => d.OrdenId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(o => o.Estado); // Cocina y caja filtran por estado constantemente.
        });

        builder.Entity<DetalleOrden>(e =>
        {
            e.HasOne(d => d.Producto)
             .WithMany()
             .HasForeignKey(d => d.ProductoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Factura>(e =>
        {
            // Relación 1 a 1: una orden tiene como máximo una factura.
            e.HasOne(f => f.Orden)
             .WithOne(o => o.Factura)
             .HasForeignKey<Factura>(f => f.OrdenId);
            e.HasIndex(f => f.CreadoEn);
        });
    }
}
