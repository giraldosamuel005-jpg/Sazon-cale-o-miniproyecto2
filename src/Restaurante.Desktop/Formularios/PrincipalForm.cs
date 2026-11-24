// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using Restaurante.Core.Seguridad;
using Restaurante.Desktop.Vistas;

namespace Restaurante.Desktop.Formularios;

/// <summary>Ventana principal: muestra solo las pestañas que el rol del usuario puede usar.</summary>
public class PrincipalForm : Form
{
    public bool CerroSesion { get; private set; }

    public PrincipalForm(SesionUsuario sesion)
    {
        Text = $"Sazón Caleño - {sesion.Nombre}";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 650);
        BackColor = Tema.Fondo;
        Font = Tema.Normal;

        // Encabezado
        var encabezado = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Tema.Marca, Padding = new Padding(12, 8, 12, 8) };
        var titulo = new Label { Text = "🔥 Sazón Caleño", Font = Tema.Titulo, ForeColor = Color.White, AutoSize = true, Location = new Point(12, 13) };
        var salir = Tema.Boton("Cerrar sesión", Color.FromArgb(140, 44, 21));
        salir.Dock = DockStyle.Right;
        var usuario = new Label
        {
            Text = $"{sesion.Nombre}  ·  {string.Join(", ", sesion.Roles)}",
            ForeColor = Color.White,
            AutoSize = true,
            Dock = DockStyle.Right,
            Padding = new Padding(0, 8, 12, 0)
        };
        encabezado.Controls.Add(titulo);
        encabezado.Controls.Add(usuario);
        encabezado.Controls.Add(salir);

        // Pestañas según rol
        var pestanas = new TabControl { Dock = DockStyle.Fill, Font = Tema.Normal, Padding = new Point(16, 6) };
        if (sesion.EsAdmin || sesion.TieneRol(Roles.Cocina))
            pestanas.TabPages.Add(CrearPestana("🍳 Cocina", new CocinaVista()));
        if (sesion.EsAdmin || sesion.TieneRol(Roles.Mesero))
            pestanas.TabPages.Add(CrearPestana("💵 Caja", new CajaVista(sesion)));
        if (sesion.EsAdmin)
            pestanas.TabPages.Add(CrearPestana("📊 Reportes", new ReportesVista()));

        Controls.Add(pestanas);
        Controls.Add(encabezado);

        salir.Click += (_, _) =>
        {
            CerroSesion = true;
            Close();
        };
    }

    private static TabPage CrearPestana(string titulo, Control contenido)
    {
        var pagina = new TabPage(titulo) { BackColor = Tema.Fondo, Padding = new Padding(10) };
        contenido.Dock = DockStyle.Fill;
        pagina.Controls.Add(contenido);
        return pagina;
    }
}
