using Restaurante.Core.Datos;
using Restaurante.Desktop.Formularios;

namespace Restaurante.Desktop;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.Message, "Error inesperado", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Misma base de datos que la web: aplica migraciones y datos iniciales si hace falta.
        try
        {
            using var db = Fabrica.CrearContexto();
            InicializadorDatos.InicializarAsync(db).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo abrir la base de datos:\n{ex.Message}", "Sazón Caleño",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Ciclo: login -> ventana principal -> (cerrar sesión) -> login...
        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK || login.Sesion is null) break;

            using var principal = new PrincipalForm(login.Sesion);
            Application.Run(principal);
            if (!principal.CerroSesion) break;
        }
    }
}
