using Terminal.Gui;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

if (args.Length > 0 && args[0].Equals("servidor", StringComparison.OrdinalIgnoreCase))
{
    await Servidor.RunAsync(args.Skip(1).ToArray());
    return;
}

var hiloServidorDeFondo = Servidor.RunAsync(Array.Empty<string>());

Application.Init();
var raizConsola = Application.Top;

var endpointApi = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

var maestroArticulos = new List<Articulo>();
var buscadorArticulos = new List<Articulo>();
var historialAuditoria = new List<TransaccionStock>();


var marcoConsola = new Window() {
    Title = " [ Servidor de Control / Panel Central ] ",
    X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() - 1
};

var lblFiltroBusqueda = new Label() { Text = "Buscar registro: ", X = 1, Y = 1 };
var inputCriterio = new TextField() { X = 20, Y = 1, Width = 30 };

var seccionIzquierda = new FrameView(" Inventario Base ") {
    X = 1, Y = 3, Width = Dim.Percent(50), Height = Dim.Fill() - 1
};
var listadoArticulosTui = new ListView() {
    X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
};
seccionIzquierda.Add(listadoArticulosTui);

var seccionDerecha = new FrameView(" Reporte de Movimientos ") {
    X = Pos.Right(seccionIzquierda) + 2, Y = 3, Width = Dim.Fill() - 1, Height = Dim.Fill() - 1
};
var listadoMovimientosTui = new ListView() {
    X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
};
seccionDerecha.Add(listadoMovimientosTui);

marcoConsola.Add(lblFiltroBusqueda, inputCriterio, seccionIzquierda, seccionDerecha);

var menuEstructura = new MenuBar(new MenuBarItem[] {
    new MenuBarItem("_Datos", new MenuItem[] {
        new MenuItem("_Nuevo Registro", "Crear artículo", () => AbrirFormularioDatos(null)),
        new MenuItem("_Modificar", "Cambiar campos", () => InicializarEdicion()),
        new MenuItem("_Borrar", "Eliminar de base de datos", () => EjecutarBajaLogica())
    }),
    new MenuBarItem("_Stock", new MenuItem[] {
        new MenuItem("Registrar _Kardex", "Movimiento manual", () => MenuMovimientoStock())
    }),
    new MenuBarItem("_Salir", "Cerrar sistema", () => Application.RequestStop())
});

raizConsola.Add(menuEstructura, marcoConsola);


async Task TraerDatosDelServidorAsync() {
    try {
        var descarga = await endpointApi.GetFromJsonAsync<List<Articulo>>("articulos");
        if (descarga != null) {
            maestroArticulos = descarga;
            ActualizarMatrizPantalla(inputCriterio.Text?.ToString() ?? string.Empty);
        }
    } catch {
    }
}

void ActualizarMatrizPantalla(string query) {
    if (string.IsNullOrWhiteSpace(query)) {
        buscadorArticulos = maestroArticulos;
    } else {
        buscadorArticulos = maestroArticulos
            .Where(x => x.Descripcion.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                        x.Sku.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
    
    var filasFormateadas = new List<string>();
    foreach(var item in buscadorArticulos) {
        filasFormateadas.Add($"SKU: {item.Sku} | {item.Descripcion} | Price: ${item.Costo} | Stock: {item.ExistenciaActual}");
    }
    listadoArticulosTui.SetSource(filasFormateadas);
}

async Task CargarHistorialArticuloAsync(int idItem) {
    try {
        var logs = await endpointApi.GetFromJsonAsync<List<TransaccionStock>>($"articulos/{idItem}/historial");
        if (logs != null) {
            historialAuditoria = logs;
            listadoMovimientosTui.SetSource(historialAuditoria
                .Select(l => $"[{l.MomentoRegistro:yyyy-MM-dd}] Mod: {l.Variante} -> Cant: {l.Unidades}").ToList());
        }
    } catch {
        historialAuditoria.Clear();
        listadoMovimientosTui.SetSource(new List<string>());
    }
}


listadoArticulosTui.SelectedItemChanged += async (e) => {
    if (buscadorArticulos.Count > 0 && listadoArticulosTui.SelectedItem < buscadorArticulos.Count) {
        var row = buscadorArticulos[listadoArticulosTui.SelectedItem];
        await CargarHistorialArticuloAsync(row.Id);
    } else {
        listadoMovimientosTui.SetSource(new List<string>());
    }
};

inputCriterio.TextChanged += (txt) => {
    ActualizarMatrizPantalla(inputCriterio.Text?.ToString() ?? string.Empty);
};

void InicializarEdicion() {
    if (buscadorArticulos.Count > 0) {
        var actual = buscadorArticulos[listadoArticulosTui.SelectedItem];
        AbrirFormularioDatos(actual);
    }
}

async void EjecutarBajaLogica() {
    if (buscadorArticulos.Count > 0) {
        var target = buscadorArticulos[listadoArticulosTui.SelectedItem];
        int alert = MessageBox.Query("Ventana de confirmación", $"¿Remover item: {target.Descripcion}?", "Confirmar", "Cancelar");
        if (alert == 0) {
            await endpointApi.DeleteAsync($"articulos/{target.Id}");
            await TraerDatosDelServidorAsync();
        }
    }
}


void AbrirFormularioDatos(Articulo? modeloBase) {
    bool esAlta = modeloBase == null;
    var popup = new Dialog(esAlta ? "Ficha Técnica: Inserción" : "Ficha Técnica: Actualización", 52, 14);

    var label1 = new Label("Código SKU:") { X = 2, Y = 1 };
    var box1 = new TextField(esAlta ? "" : modeloBase!.Sku) { X = 15, Y = 1, Width = 32 };

    var label2 = new Label("Detalle:") { X = 2, Y = 3 };
    var box2 = new TextField(esAlta ? "" : modeloBase!.Descripcion) { X = 15, Y = 3, Width = 32 };

    var label3 = new Label("Costo:") { X = 2, Y = 5 };
    var box3 = new TextField(esAlta ? "0" : modeloBase!.Costo.ToString()) { X = 15, Y = 5, Width = 32 };

    var label4 = new Label("Stock Fijo:") { X = 2, Y = 7 };
    var box4 = new TextField(esAlta ? "0" : modeloBase!.ExistenciaActual.ToString()) { X = 15, Y = 7, Width = 32, ReadOnly = !esAlta };

    popup.Add(label1, box1, label2, box2, label3, box3, label4, box4);

    var btnOk = new Button("Confirmar");
    btnOk.Clicked += async () => {
        var temporal = esAlta ? new Articulo() : modeloBase!;
        temporal.Sku = box1.Text?.ToString() ?? string.Empty;
        temporal.Descripcion = box2.Text?.ToString() ?? string.Empty;
        temporal.Costo = decimal.TryParse(box3.Text?.ToString() ?? string.Empty, out decimal val) ? val : 0;
        temporal.ExistenciaActual = int.TryParse(box4.Text?.ToString() ?? string.Empty, out int stk) ? stk : 0;

        if (esAlta) {
            await endpointApi.PostAsJsonAsync("articulos", temporal);
        } else {
            await endpointApi.PutAsJsonAsync($"articulos/{temporal.Id}", temporal);
        }

        Application.RequestStop();
        await TraerDatosDelServidorAsync();
    };

    var btnCancel = new Button("Abortar");
    btnCancel.Clicked += () => Application.RequestStop();

    popup.AddButton(btnOk);
    popup.AddButton(btnCancel);
    Application.Run(popup);
}

void MenuMovimientoStock() {
    if (buscadorArticulos.Count == 0) return;
    var prodActivo = buscadorArticulos[listadoArticulosTui.SelectedItem];

    var modal = new Dialog("Inyectar Flujo de Unidades", 52, 11);

    var lblM = new Label("Modo (0=Ingreso, 1=Egreso, 2=Ajuste):") { X = 2, Y = 1 };
    var inputM = new TextField("0") { X = 2, Y = 2, Width = 12 };

    var lblC = new Label("Volumen numérico:") { X = 2, Y = 4 };
    var inputC = new TextField("1") { X = 2, Y = 5, Width = 22 };

    modal.Add(lblM, inputM, lblC, inputC);

    var btnSave = new Button("Aplicar");
    btnSave.Clicked += async () => {
        int.TryParse(inputM.Text.ToString(), out int tipoInt);
        int.TryParse(inputC.Text.ToString(), out int qUnidades);

        var nuevaTransaccion = new TransaccionStock {
            Variante = (ModoMovimiento)Math.Clamp(tipoInt, 0, 2),
            Unidades = Math.Abs(qUnidades)
        };

        await endpointApi.PostAsJsonAsync($"articulos/{prodActivo.Id}/historial", nuevaTransaccion);
        
        Application.RequestStop();
        await TraerDatosDelServidorAsync(); 
        await CargarHistorialArticuloAsync(prodActivo.Id); 
    };

    var btnExit = new Button("Cerrar");
    btnExit.Clicked += () => Application.RequestStop();

    modal.AddButton(btnSave);
    modal.AddButton(btnExit);
    Application.Run(modal);
}

_ = TraerDatosDelServidorAsync();
Application.Run();
Application.Shutdown();

public class Articulo {
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Costo { get; set; }
    public int ExistenciaActual { get; set; }
}
public class TransaccionStock {
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public DateTime MomentoRegistro { get; set; } = DateTime.Now;
    public ModoMovimiento Variante { get; set; }
    public int Unidades { get; set; }
}
public enum ModoMovimiento { Alta = 0, Baja = 1, Recuento = 2 }