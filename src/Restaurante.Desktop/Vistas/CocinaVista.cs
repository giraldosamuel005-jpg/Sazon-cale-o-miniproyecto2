using Restaurante.Core.Dominio;
using Restaurante.Core.Servicios;

namespace Restaurante.Desktop.Vistas;

/// <summary>
/// Pantalla de cocina. Muestra la cola FIFO de órdenes pendientes (Queue&lt;Orden&gt;),
/// las que se están preparando y un registro de eventos de cambio de estado.
/// Se refresca sola cada 5 segundos para ver las órdenes que llegan desde la web.
/// </summary>
public class CocinaVista : UserControl
{
    private readonly DataGridView _cola = Tema.Tabla();
    private readonly DataGridView _preparando = Tema.Tabla();
    private readonly ListBox _detalle = new() { Dock = DockStyle.Fill, Font = Tema.Normal, BorderStyle = BorderStyle.None };
    private readonly ListBox _eventos = new() { Dock = DockStyle.Fill, Font = new Font("Consolas", 9.5f), BorderStyle = BorderStyle.None };
    private readonly Label _siguiente = new() { AutoSize = true, Font = Tema.Titulo, ForeColor = Tema.Marca };
    private readonly Button _tomar = Tema.Boton("▶ Tomar siguiente");
    private readonly Button _lista = Tema.Boton("✔ Marcar lista", Tema.Exito);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 5000 };

    // Copia en memoria de las órdenes en preparación, indexadas por Id para mostrar su detalle.
    private Dictionary<int, Orden> _ordenesPorId = new();
    private bool _cargando;

    public CocinaVista()
    {
        var acciones = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 5, 0, 5) };
        acciones.Controls.AddRange(new Control[] { _tomar, _lista, _siguiente });
        _siguiente.Margin = new Padding(20, 8, 0, 0);

        var izquierda = Grupo("Cola de órdenes (primero en llegar, primero en salir)", _cola);
        var centro = Grupo("En preparación", _preparando);
        var derecha = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        derecha.Controls.Add(Grupo("Detalle de la orden seleccionada", _detalle), 0, 0);
        derecha.Controls.Add(Grupo("Registro de eventos", _eventos), 0, 1);

        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        tabla.Controls.Add(izquierda, 0, 0);
        tabla.Controls.Add(centro, 1, 0);
        tabla.Controls.Add(derecha, 2, 0);

        Controls.Add(tabla);
        Controls.Add(acciones);

        _tomar.Click += async (_, _) => await TomarSiguienteAsync();
        _lista.Click += async (_, _) => await MarcarListaAsync();
        _cola.SelectionChanged += (_, _) => MostrarDetalle(_cola);
        _preparando.SelectionChanged += (_, _) => MostrarDetalle(_preparando);
        _timer.Tick += async (_, _) => await CargarAsync();

        Load += async (_, _) =>
        {
            await CargarAsync();
            _timer.Start();
        };
    }

    internal static GroupBox Grupo(string titulo, Control contenido)
    {
        var g = new GroupBox { Text = titulo, Dock = DockStyle.Fill, Font = Tema.Normal, Padding = new Padding(8) };
        contenido.Dock = DockStyle.Fill;
        g.Controls.Add(contenido);
        return g;
    }

    private async Task CargarAsync()
    {
        if (_cargando) return;
        _cargando = true;
        try
        {
            using var db = Fabrica.CrearContexto();
            var servicio = new ServicioOrdenes(db);
            Queue<Orden> cola = await servicio.ObtenerColaCocinaAsync();
            var preparando = await servicio.ObtenerPorEstadoAsync(EstadoOrden.EnPreparacion);

            _ordenesPorId = cola.Concat(preparando).ToDictionary(o => o.Id);

            int posicion = 1;
            _cola.DataSource = cola.Select(o => new
            {
                Turno = posicion++,
                Orden = o.Id,
                Mesa = o.Mesa?.Numero,
                Productos = o.CantidadItems,
                Espera = $"{o.MinutosEspera} min"
            }).ToList();

            _preparando.DataSource = preparando.Select(o => new
            {
                Orden = o.Id,
                Mesa = o.Mesa?.Numero,
                Mesero = o.MeseroNombre,
                Espera = $"{o.MinutosEspera} min"
            }).ToList();

            // Peek: ver quién sigue sin sacarlo de la cola.
            _siguiente.Text = cola.Count > 0
                ? $"Siguiente: mesa {cola.Peek().Mesa?.Numero} (orden #{cola.Peek().Id}) · {cola.Count} en cola"
                : "Sin órdenes en cola";
            _tomar.Enabled = cola.Count > 0;
            _lista.Enabled = preparando.Count > 0;
        }
        catch (Exception ex)
        {
            _siguiente.Text = "Error al cargar: " + ex.Message;
        }
        finally
        {
            _cargando = false;
        }
    }

    private void MostrarDetalle(DataGridView grilla)
    {
        if (!grilla.Focused && grilla.SelectedRows.Count == 0) return;
        _detalle.Items.Clear();
        if (grilla.CurrentRow?.Cells["Orden"].Value is not int id || !_ordenesPorId.TryGetValue(id, out var orden))
            return;

        _detalle.Items.Add($"Orden #{orden.Id} · Mesa {orden.Mesa?.Numero}");
        _detalle.Items.Add($"Mesero: {orden.MeseroNombre}");
        _detalle.Items.Add("");
        foreach (var d in orden.Detalles)
            _detalle.Items.Add($"  {d.Cantidad} x {d.Producto?.Nombre}");
        if (!string.IsNullOrEmpty(orden.Notas))
        {
            _detalle.Items.Add("");
            _detalle.Items.Add($"NOTA: {orden.Notas}");
        }
    }

    /// <summary>Se suscribe al evento EstadoCambiado del servicio para registrar cada cambio.</summary>
    private ServicioOrdenes CrearServicio(Core.Datos.RestauranteDbContext db)
    {
        var servicio = new ServicioOrdenes(db);
        servicio.EstadoCambiado += (_, e) =>
            _eventos.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  #{e.OrdenId} mesa {e.MesaNumero}: {e.Anterior.Nombre()} → {e.Nuevo.Nombre()}");
        return servicio;
    }

    private async Task TomarSiguienteAsync()
    {
        try
        {
            using var db = Fabrica.CrearContexto();
            var orden = await CrearServicio(db).TomarSiguienteAsync();
            if (orden is null)
                MessageBox.Show("No hay órdenes en cola.", "Cocina", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (ReglaNegocioException ex)
        {
            MessageBox.Show(ex.Message, "Cocina", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        await CargarAsync();
    }

    private async Task MarcarListaAsync()
    {
        if (_preparando.CurrentRow?.Cells["Orden"].Value is not int id)
        {
            MessageBox.Show("Seleccione una orden en preparación.", "Cocina", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            using var db = Fabrica.CrearContexto();
            await CrearServicio(db).CambiarEstadoAsync(id, EstadoOrden.Lista);
        }
        catch (ReglaNegocioException ex)
        {
            MessageBox.Show(ex.Message, "Cocina", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        await CargarAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
