namespace Restaurante.Core.Datos;

/// <summary>
/// Ubicación única del archivo SQLite. La web y el escritorio la calculan igual
/// (%LOCALAPPDATA%\RestauranteApp\restaurante.db), así comparten los mismos datos.
/// </summary>
public static class RutaBaseDatos
{
    public static string Archivo
    {
        get
        {
            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RestauranteApp");
            Directory.CreateDirectory(carpeta);
            return Path.Combine(carpeta, "restaurante.db");
        }
    }

    public static string CadenaConexion => $"Data Source={Archivo}";
}
