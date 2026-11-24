// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Restaurante.Core.Datos;

/// <summary>
/// Decide a qué base de datos se conectan la web y el escritorio.
/// <list type="bullet">
/// <item>Con cadena de conexión de PostgreSQL (servidor en la nube): web y escritorio,
/// desde cualquier equipo, comparten los mismos datos en tiempo real.</item>
/// <item>Sin cadena: SQLite local en %LOCALAPPDATA%\RestauranteApp (modo sin internet).</item>
/// </list>
/// La cadena se busca, en orden, en: el valor recibido (appsettings de la web),
/// la variable de entorno SAZON_DB y el archivo "conexion.txt" junto al ejecutable.
/// </summary>
public static class ConexionBaseDatos
{
    public const string VariableEntorno = "SAZON_DB";
    public const string ArchivoConfiguracion = "conexion.txt";

    static ConexionBaseDatos()
    {
        // El dominio usa DateTime.Now (hora local de Colombia). Con este modo Npgsql guarda las
        // fechas como "timestamp without time zone" tal cual, igual que lo hacía SQLite.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    /// <summary>Cadena efectiva o null si no hay ninguna configurada (se usará SQLite).</summary>
    public static string? Resolver(string? configurada = null)
    {
        if (!string.IsNullOrWhiteSpace(configurada)) return configurada.Trim();

        var entorno = Environment.GetEnvironmentVariable(VariableEntorno);
        if (!string.IsNullOrWhiteSpace(entorno)) return entorno.Trim();

        var archivo = Path.Combine(AppContext.BaseDirectory, ArchivoConfiguracion);
        if (File.Exists(archivo))
        {
            var linea = File.ReadAllLines(archivo)
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 0 && !l.StartsWith('#'));
            if (!string.IsNullOrWhiteSpace(linea)) return linea;
        }
        return null;
    }

    public static bool EsPostgres(string? cadena) =>
        cadena is not null &&
        (cadena.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
         cadena.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
         cadena.Contains("Host=", StringComparison.OrdinalIgnoreCase));

    /// <summary>Configura el proveedor de EF Core según la cadena.</summary>
    public static DbContextOptionsBuilder Configurar(DbContextOptionsBuilder opciones, string? cadena)
    {
        if (EsPostgres(cadena))
            return opciones.UseNpgsql(NormalizarPostgres(cadena!), o => o.EnableRetryOnFailure(3));

        return opciones.UseSqlite(string.IsNullOrWhiteSpace(cadena) ? RutaBaseDatos.CadenaConexion : cadena);
    }

    /// <summary>
    /// Neon, Render y casi todos los proveedores entregan la conexión como URL
    /// (postgresql://usuario:clave@servidor/base?sslmode=require). Npgsql espera el formato
    /// "Host=...;Username=...", así que aquí se traduce.
    /// </summary>
    public static string NormalizarPostgres(string cadena)
    {
        if (!cadena.Contains("://")) return cadena;

        var uri = new Uri(cadena);
        var credenciales = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(credenciales[0]),
            Password = credenciales.Length > 1 ? Uri.UnescapeDataString(credenciales[1]) : null,
            SslMode = SslMode.Require
        };
        return builder.ConnectionString;
    }
}
