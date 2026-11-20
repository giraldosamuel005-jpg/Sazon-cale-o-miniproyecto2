using Restaurante.Core.Datos;

namespace Restaurante.Desktop;

/// <summary>
/// Crea un DbContext nuevo por operación. En escritorio no hay contenedor de dependencias
/// por petición como en la web, así que cada acción abre y cierra su propio contexto
/// (evita datos "viejos" en memoria cuando la web modifica la misma base).
/// </summary>
public static class Fabrica
{
    public static RestauranteDbContext CrearContexto() => new(RestauranteDbContext.CrearOpciones());
}

/// <summary>Colores y fuentes compartidos para que todas las pantallas se vean iguales.</summary>
public static class Tema
{
    public static readonly Color Marca = Color.FromArgb(178, 58, 30);
    public static readonly Color Fondo = Color.FromArgb(250, 247, 242);
    public static readonly Color Exito = Color.FromArgb(46, 139, 87);
    public static readonly Font Titulo = new("Segoe UI Semibold", 14f);
    public static readonly Font Normal = new("Segoe UI", 10f);
    public static readonly Font Grande = new("Segoe UI", 18f, FontStyle.Bold);

    public static Button Boton(string texto, Color? fondo = null)
    {
        var b = new Button
        {
            Text = texto,
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            BackColor = fondo ?? Marca,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = Normal,
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    public static DataGridView Tabla()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = Normal
        };
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10f);
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 236, 230);
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 200, 185);
        g.DefaultCellStyle.SelectionForeColor = Color.Black;
        return g;
    }
}
