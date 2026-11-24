// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using Restaurante.Core.Dominio;
using Restaurante.Core.Servicios;

namespace Restaurante.Desktop.Vistas;

/// <summary>Reportes para el administrador: indicadores, gráfico de ventas por día y top de productos.</summary>
public class ReportesVista : UserControl
{
    private readonly DateTimePicker _desde = new() { Format = DateTimePickerFormat.Short, Width = 130 };
    private readonly DateTimePicker _hasta = new() { Format = DateTimePickerFormat.Short, Width = 130 };
    private readonly Button _consultar = Tema.Boton("Consultar");
    private readonly FlowLayoutPanel _indicadores = new() { Dock = DockStyle.Top, Height = 95, Padding = new Padding(0, 5, 0, 5) };
    private readonly DataGridView _top = Tema.Tabla();
    private readonly DataGridView _metodos = Tema.Tabla();
    private readonly GraficoBarras _grafico = new() { Dock = DockStyle.Fill };

    public ReportesVista()
    {
        _desde.Value = DateTime.Today.AddDays(-6);
        _hasta.Value = DateTime.Today;

        var filtros = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46 };
        filtros.Controls.AddRange(new Control[]
        {
            new Label { Text = "Desde", AutoSize = true, Margin = new Padding(3, 10, 3, 3) }, _desde,
            new Label { Text = "Hasta", AutoSize = true, Margin = new Padding(12, 10, 3, 3) }, _hasta,
            _consultar
        });

        var abajo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        abajo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        abajo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        abajo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        abajo.Controls.Add(CocinaVista.Grupo("Ventas por día (últimos 7 días)", _grafico), 0, 0);
        abajo.Controls.Add(CocinaVista.Grupo("Productos más vendidos", _top), 1, 0);
        abajo.Controls.Add(CocinaVista.Grupo("Por método de pago", _metodos), 2, 0);

        Controls.Add(abajo);
        Controls.Add(_indicadores);
        Controls.Add(filtros);

        _consultar.Click += async (_, _) => await CargarAsync();
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        var desde = _desde.Value.Date;
        var hasta = _hasta.Value.Date.AddDays(1);

        using var db = Fabrica.CrearContexto();
        var reportes = new ServicioReportes(db);
        var resumen = await reportes.ResumenAsync(desde, hasta);
        var top = await reportes.ProductosMasVendidosAsync(desde, hasta, 10);
        var porDia = await reportes.VentasPorDiaAsync(7);

        _indicadores.Controls.Clear();
        _indicadores.Controls.Add(Indicador("Ventas", resumen.Ventas.Pesos()));
        _indicadores.Controls.Add(Indicador("Facturas", resumen.CantidadFacturas.ToString()));
        _indicadores.Controls.Add(Indicador("Ticket promedio", resumen.TicketPromedio.Pesos()));
        _indicadores.Controls.Add(Indicador("Propinas", resumen.Propinas.Pesos()));
        _indicadores.Controls.Add(Indicador("Impoconsumo", resumen.Impuestos.Pesos()));

        _top.DataSource = top.Select((p, i) => new
        {
            Puesto = i + 1,
            Producto = p.Nombre,
            Unidades = p.Cantidad,
            Total = p.Total.Pesos()
        }).ToList();

        _metodos.DataSource = resumen.PorMetodoPago.Select(kv => new
        {
            Método = kv.Key.Nombre(),
            Total = kv.Value.Pesos()
        }).ToList();

        _grafico.Datos = porDia;
    }

    private static Panel Indicador(string titulo, string valor)
    {
        var p = new Panel { Width = 190, Height = 78, BackColor = Color.White, Margin = new Padding(0, 0, 10, 0) };
        p.Controls.Add(new Label { Text = valor, Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = Tema.Marca, AutoSize = true, Location = new Point(10, 32) });
        p.Controls.Add(new Label { Text = titulo.ToUpper(), Font = new Font("Segoe UI", 8.5f), ForeColor = Color.Gray, AutoSize = true, Location = new Point(10, 10) });
        return p;
    }
}

/// <summary>Control propio que dibuja un gráfico de barras con GDI+ (sin librerías externas).</summary>
public class GraficoBarras : Control
{
    private SortedDictionary<DateTime, decimal> _datos = new();

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public SortedDictionary<DateTime, decimal> Datos
    {
        get => _datos;
        set { _datos = value; Invalidate(); } // Invalidate fuerza a redibujar.
    }

    public GraficoBarras()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_datos.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        const int margenInferior = 40, margenSuperior = 30, margenLateral = 15;
        var maximo = Math.Max(1, _datos.Values.Max());
        var altoUtil = Height - margenInferior - margenSuperior;
        var anchoColumna = (float)(Width - 2 * margenLateral) / _datos.Count;
        var anchoBarra = anchoColumna * 0.6f;

        using var pincel = new SolidBrush(Tema.Marca);
        using var fuente = new Font("Segoe UI", 8.5f);
        int i = 0;
        foreach (var (dia, valor) in _datos)
        {
            var alto = (float)(valor / maximo) * altoUtil;
            var x = margenLateral + i * anchoColumna + (anchoColumna - anchoBarra) / 2;
            var y = margenSuperior + altoUtil - alto;
            g.FillRectangle(pincel, x, y, anchoBarra, Math.Max(alto, 1));

            var etiquetaValor = $"{valor / 1000:0}k";
            var tamValor = g.MeasureString(etiquetaValor, fuente);
            g.DrawString(etiquetaValor, fuente, Brushes.DimGray, x + (anchoBarra - tamValor.Width) / 2, y - tamValor.Height - 2);

            var etiquetaDia = dia.ToString("ddd dd");
            var tamDia = g.MeasureString(etiquetaDia, fuente);
            g.DrawString(etiquetaDia, fuente, Brushes.DimGray, x + (anchoBarra - tamDia.Width) / 2, Height - margenInferior + 8);
            i++;
        }
    }
}
