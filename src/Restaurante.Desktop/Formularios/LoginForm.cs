// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using Restaurante.Core.Seguridad;

namespace Restaurante.Desktop.Formularios;

/// <summary>Inicio de sesión con los mismos usuarios de ASP.NET Core Identity que usa la web.</summary>
public class LoginForm : Form
{
    private readonly TextBox _email = new() { Width = 280, Font = Tema.Normal };
    private readonly TextBox _contrasena = new() { Width = 280, Font = Tema.Normal, UseSystemPasswordChar = true };
    private readonly Label _error = new() { AutoSize = true, ForeColor = Color.Firebrick, Font = Tema.Normal };
    private readonly Button _ingresar = Tema.Boton("Ingresar");

    public SesionUsuario? Sesion { get; private set; }

    public LoginForm()
    {
        Text = "Sazón Caleño - Iniciar sesión";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Tema.Fondo;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(30);

        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false
        };
        panel.Controls.Add(new Label { Text = "🔥 Sazón Caleño", Font = Tema.Grande, ForeColor = Tema.Marca, AutoSize = true });
        panel.Controls.Add(new Label { Text = "Aplicación de cocina y caja", Font = Tema.Normal, ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(3, 0, 3, 15) });
        panel.Controls.Add(new Label { Text = "Correo", Font = Tema.Normal, AutoSize = true });
        panel.Controls.Add(_email);
        panel.Controls.Add(new Label { Text = "Contraseña", Font = Tema.Normal, AutoSize = true, Margin = new Padding(3, 10, 3, 3) });
        panel.Controls.Add(_contrasena);
        panel.Controls.Add(_error);
        _ingresar.Margin = new Padding(3, 10, 3, 3);
        panel.Controls.Add(_ingresar);
        Controls.Add(panel);

        AcceptButton = _ingresar;
        _ingresar.Click += async (_, _) => await IngresarAsync();
    }

    private async Task IngresarAsync()
    {
        _error.Text = "";
        if (string.IsNullOrWhiteSpace(_email.Text) || string.IsNullOrEmpty(_contrasena.Text))
        {
            _error.Text = "Ingrese correo y contraseña.";
            return;
        }

        _ingresar.Enabled = false;
        try
        {
            using var db = Fabrica.CrearContexto();
            var sesion = await new ServicioAutenticacion(db).IniciarSesionAsync(_email.Text, _contrasena.Text);

            if (sesion is null)
                _error.Text = "Correo o contraseña incorrectos.";
            else if (!sesion.EsAdmin && !sesion.TieneRol(Roles.Cocina) && !sesion.TieneRol(Roles.Mesero))
                _error.Text = "Su usuario no tiene un rol asignado.";
            else
            {
                Sesion = sesion;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        finally
        {
            _ingresar.Enabled = true;
        }
    }
}
