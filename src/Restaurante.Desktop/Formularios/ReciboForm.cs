// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using System.Text;
using Restaurante.Core.Dominio;

namespace Restaurante.Desktop.Formularios;

/// <summary>Muestra la factura en formato de tirilla (como una impresora POS).</summary>
public class ReciboForm : Form
{
    public ReciboForm(Factura factura)
    {
        Text = $"Factura #{factura.Id}";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(420, 560);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;

        var texto = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10f),
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            Text = Generar(factura)
        };
        var cerrar = Tema.Boton("Cerrar");
        cerrar.Dock = DockStyle.Bottom;
        cerrar.Click += (_, _) => Close();

        Controls.Add(texto);
        Controls.Add(cerrar);
    }

    private static string Generar(Factura f)
    {
        const int ancho = 40;
        var o = f.Orden!;
        // StringBuilder: concatenar muchas líneas sin crear un string nuevo en cada paso.
        var sb = new StringBuilder();
        string Linea(string izq, string der) => izq.PadRight(ancho - der.Length) + der;

        sb.AppendLine("SAZÓN CALEÑO".PadLeft((ancho + 12) / 2));
        sb.AppendLine("Cali, Valle del Cauca".PadLeft((ancho + 21) / 2));
        sb.AppendLine(new string('=', ancho));
        sb.AppendLine(Linea($"Factura #{f.Id}", f.CreadoEn.ToString("dd/MM/yyyy HH:mm")));
        sb.AppendLine($"Mesa {o.Mesa?.Numero} · Orden #{o.Id}");
        sb.AppendLine($"Atendió: {o.MeseroNombre}");
        sb.AppendLine(new string('-', ancho));
        foreach (var d in o.Detalles)
        {
            var nombre = $"{d.Cantidad} x {d.Producto?.Nombre}";
            if (nombre.Length > 27) nombre = nombre[..27];
            sb.AppendLine(Linea(nombre, d.Subtotal.Pesos()));
        }
        sb.AppendLine(new string('-', ancho));
        sb.AppendLine(Linea("Subtotal", f.Subtotal.Pesos()));
        sb.AppendLine(Linea("Impoconsumo 8%", f.Impuesto.Pesos()));
        sb.AppendLine(Linea("Propina", f.Propina.Pesos()));
        sb.AppendLine(new string('=', ancho));
        sb.AppendLine(Linea("TOTAL", f.Total.Pesos()));
        sb.AppendLine();
        sb.AppendLine($"Pago: {f.MetodoPago.Nombre()}");
        sb.AppendLine($"Cajero: {f.CajeroNombre}");
        sb.AppendLine();
        sb.AppendLine("¡Gracias por su visita!".PadLeft((ancho + 23) / 2));
        return sb.ToString();
    }
}
