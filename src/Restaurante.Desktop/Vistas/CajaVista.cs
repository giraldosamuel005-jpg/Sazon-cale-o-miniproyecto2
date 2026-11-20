using Restaurante.Core.Dominio;
using Restaurante.Core.Seguridad;
using Restaurante.Core.Servicios;

namespace Restaurante.Desktop.Vistas;

/// <summary>Caja: cobra las órdenes entregadas y genera la factura con impuesto y propina.</summary>
public class CajaVista : UserControl
{
    private readonly SesionUsuario _sesion;
    private readonly DataGridView _porCobrar = Tema.Tabla();
    private readonly DataGridView _detalle = Tema.Tabla();
    private readonly ComboBox _metodo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = Tema.Normal };
    private readonly CheckBox _propina = new() { Text = "Incluir propina sugerida (10 %)", Checked = true, AutoSize = true, Font = Tema.Normal };
    private readonly Label _subtotal = Valor();
    private readonly Label _impuesto = Valor();
    private readonly Label _valorPropina = Valor();
    private readonly Label _total = new() { AutoSize = true, Font = Tema.Grande, ForeColor = Tema.Marca };
    private readonly Button _cobrar = Tema.Boton("💵 Cobrar");
    private readonly Button _refrescar = Tema.Boton("⟳ Actualizar", Color.DimGray);

    private List<Orden> _ordenes = new();

    public CajaVista(SesionUsuario sesion)
    {
        _sesion = sesion;

        _metodo.DataSource = Enum.GetValues<MetodoPago>()
            .Select(m => new { Valor = m, Texto = m.Nombre() }).ToList();
        _metodo.DisplayMember = "Texto";
        _metodo.ValueMember = "Valor";

        // Panel de cobro (derecha)
        var cobro = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, Padding = new Padding(10) };
        cobro.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cobro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Fila(string texto, Control control)
        {
            cobro.Controls.Add(new Label { Text = texto, AutoSize = true, Font = Tema.Normal, Margin = new Padding(3, 8, 10, 3) });
            cobro.Controls.Add(control);
        }
        Fila("Subtotal", _subtotal);
        Fila("Impoconsumo (8 %)", _impuesto);
        Fila("Propina", _valorPropina);
        Fila("TOTAL", _total);
        Fila("Método de pago", _metodo);
        cobro.Controls.Add(new Label());
        cobro.Controls.Add(_propina);
        cobro.Controls.Add(new Label());
        cobro.Controls.Add(_cobrar);

        var derecha = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        derecha.Controls.Add(CocinaVista.Grupo("Detalle de la cuenta", _detalle), 0, 0);
        derecha.Controls.Add(CocinaVista.Grupo("Cobro", cobro), 0, 1);

        var principal = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        principal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        principal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        principal.Controls.Add(CocinaVista.Grupo("Órdenes entregadas por cobrar", _porCobrar), 0, 0);
        principal.Controls.Add(derecha, 1, 0);

        var acciones = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46 };
        acciones.Controls.Add(_refrescar);

        Controls.Add(principal);
        Controls.Add(acciones);

        _porCobrar.SelectionChanged += (_, _) => MostrarCuenta();
        _propina.CheckedChanged += (_, _) => MostrarCuenta();
        _cobrar.Click += async (_, _) => await CobrarAsync();
        _refrescar.Click += async (_, _) => await CargarAsync();
        Load += async (_, _) => await CargarAsync();
        VisibleChanged += async (_, _) => { if (Visible) await CargarAsync(); };
    }

    private static Label Valor() => new() { AutoSize = true, Font = Tema.Normal, Margin = new Padding(3, 8, 3, 3) };

    private async Task CargarAsync()
    {
        using var db = Fabrica.CrearContexto();
        _ordenes = await new ServicioFacturacion(db).ObtenerPorCobrarAsync();
        _porCobrar.DataSource = _ordenes.Select(o => new
        {
            Orden = o.Id,
            Mesa = o.Mesa?.Numero,
            Mesero = o.MeseroNombre,
            Productos = o.CantidadItems,
            Total = o.Total.Pesos()
        }).ToList();
        MostrarCuenta();
    }

    private Orden? OrdenSeleccionada =>
        _porCobrar.CurrentRow?.Cells["Orden"].Value is int id ? _ordenes.FirstOrDefault(o => o.Id == id) : null;

    private void MostrarCuenta()
    {
        var orden = OrdenSeleccionada;
        _cobrar.Enabled = orden is not null;
        if (orden is null)
        {
            _detalle.DataSource = null;
            _subtotal.Text = _impuesto.Text = _valorPropina.Text = _total.Text = "-";
            return;
        }

        _detalle.DataSource = orden.Detalles.Select(d => new
        {
            Producto = d.Producto?.Nombre,
            Cantidad = d.Cantidad,
            Precio = d.PrecioUnitario.Pesos(),
            Subtotal = d.Subtotal.Pesos()
        }).ToList();

        // Vista previa con la misma fábrica de la factura real (misma regla de negocio).
        var vistaPrevia = Factura.Crear(orden, MetodoPago.Efectivo, _propina.Checked, _sesion.Nombre);
        _subtotal.Text = vistaPrevia.Subtotal.Pesos();
        _impuesto.Text = vistaPrevia.Impuesto.Pesos();
        _valorPropina.Text = vistaPrevia.Propina.Pesos();
        _total.Text = vistaPrevia.Total.Pesos();
    }

    private async Task CobrarAsync()
    {
        var orden = OrdenSeleccionada;
        if (orden is null) return;

        var metodo = (MetodoPago)_metodo.SelectedValue!;
        var confirmar = MessageBox.Show(
            $"¿Cobrar la mesa {orden.Mesa?.Numero} por {_total.Text} en {metodo.Nombre()}?",
            "Confirmar pago", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmar != DialogResult.Yes) return;

        try
        {
            using var db = Fabrica.CrearContexto();
            var factura = await new ServicioFacturacion(db).FacturarAsync(orden.Id, metodo, _propina.Checked, _sesion.Nombre);
            using var recibo = new Formularios.ReciboForm(factura);
            recibo.ShowDialog(this);
        }
        catch (ReglaNegocioException ex)
        {
            MessageBox.Show(ex.Message, "Caja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        await CargarAsync();
    }
}
