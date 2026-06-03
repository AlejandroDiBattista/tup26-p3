using Terminal.Gui;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

if (args.Length > 0 && args[0].Equals("servidor", StringComparison.OrdinalIgnoreCase))
{
    return;
}

Application.Init();
var capaPrincipal = Application.Top;

var canalServicioWeb = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

var catalogoGeneralProductos = new List<Mercaderia>();
var vistaFiltradaProductos = new List<Mercaderia>();
var bitacoraKardexItem = new List<RegistroKardex>();


var pantallaOperativa = new Window() {
    Title = " [ S.O.M.I. - Catálogo y Gestión Visual de Existencias ] ",
    X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() - 1
};

var lblBuscadorFiltro = new Label() { Text = "Texto a filtrar: ", X = 2, Y = 1 };
var txtCajaTextoBusqueda = new TextField() { X = 20, Y = 1, Width = 38 };

var moduloCatalogo = new FrameView(" Listado Comercial de Productos ") {
    X = 1, Y = 3, Width = Dim.Percent(55), Height = Dim.Fill() - 1
};
var gridListaProductos = new ListView() {
    X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
};
moduloCatalogo.Add(gridListaProductos);

var moduloMovimientos = new FrameView(" Historial de Movimientos (Kardex) ") {
    X = Pos.Right(moduloCatalogo) + 1, Y = 3, Width = Dim.Fill() - 1, Height = Dim.Fill() - 1
};
var gridListaKardex = new ListView() {
    X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill()
};
moduloMovimientos.Add(gridListaKardex);

pantallaOperativa.Add(lblBuscadorFiltro, txtCajaTextoBusqueda, moduloCatalogo, moduloMovimientos);

var barraHerramientas = new MenuBar(new MenuBarItem[] {
    new MenuBarItem("_Artículos", new MenuItem[] {
        new MenuItem("_Ingresar Producto", "Nuevo registro", () => MostrarCajaFicha(null)),
        new MenuItem("_Editar Ficha", "Modificar datos", () => LanzarEdicionFicha()),
        new MenuItem("_Remover Registro", "Borrar definitivamente", () => ConfirmarRemocionFila())
    }),
    new MenuBarItem("_Inventariar", new MenuItem[] {
        new MenuItem("Ajustar _Unidades", "Flujo de stock manual", () => PanelEntradaSalidaStock())
    }),
    new MenuBarItem("_Cerrar", "Terminar proceso", () => Application.RequestStop())
});

capaPrincipal.Add(barraHerramientas, pantallaOperativa);


async Task ObtenerCatalogoRemotoAsync() {
    try {
        var datosApi = await canalServicioWeb.GetFromJsonAsync<List<Mercaderia>>("articulos");
        if (datosApi != null) {
            catalogoGeneralProductos = datosApi;
            FiltrarResultadosCatalogo(txtCajaTextoBusqueda.Text?.ToString() ?? string.Empty);
        }
    } catch {
    }
}

void FiltrarResultadosCatalogo(string patron) {
    if (string.IsNullOrWhiteSpace(patron)) {
        vistaFiltradaProductos = catalogoGeneralProductos;
    } else {
        vistaFiltradaProductos = catalogoGeneralProductos
            .Where(p => p.Descripcion.Contains(patron, StringComparison.OrdinalIgnoreCase) || 
                        p.Sku.Contains(patron, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
    
    var filasPantalla = new List<string>();
    foreach(var p in vistaFiltradaProductos) {
        filasPantalla.Add($"[SKU: {p.Sku}] {p.Descripcion} -> Costo: ${p.Costo} | Stock: {p.ExistenciaActual} uds.");
    }
    gridListaProductos.SetSource(filasPantalla);
}

async Task DescargarKardexItemAsync(int itemId) {
    try {
        var registros = await canalServicioWeb.GetFromJsonAsync<List<RegistroKardex>>($"articulos/{itemId}/historial");
        if (registros != null) {
            bitacoraKardexItem = registros;
            gridListaKardex.SetSource(bitacoraKardexItem
                .Select(k => $"{k.FechaHoraRegistro:dd/MM/yyyy} | Operación: {k.TipoMovimiento} -> Cantidad: {k.CantidadUnidades}").ToList());
        }
    } catch {
        bitacoraKardexItem.Clear();
        gridListaKardex.SetSource(new List<string>());
    }
}


gridListaProductos.SelectedItemChanged += async (cambio) => {
    if (vistaFiltradaProductos.Count > 0 && gridListaProductos.SelectedItem < vistaFiltradaProductos.Count) {
        var productoSeleccionado = vistaFiltradaProductos[gridListaProductos.SelectedItem];
        await DescargarKardexItemAsync(productoSeleccionado.Id);
    } else {
        gridListaKardex.SetSource(new List<string>());
    }
};

txtCajaTextoBusqueda.TextChanged += (arg) => {
    FiltrarResultadosCatalogo(txtCajaTextoBusqueda.Text?.ToString() ?? string.Empty);
};

void LanzarEdicionFicha() {
    if (vistaFiltradaProductos.Count > 0) {
        var targetProd = vistaFiltradaProductos[gridListaProductos.SelectedItem];
        MostrarCajaFicha(targetProd);
    }
}

async void ConfirmarRemocionFila() {
    if (vistaFiltradaProductos.Count > 0) {
        var targetProd = vistaFiltradaProductos[gridListaProductos.SelectedItem];
        int feedback = MessageBox.Query("Alerta de Sistema", $"¿Dar de baja: {targetProd.Descripcion}?", "SÍ", "NO");
        if (feedback == 0) {
            await canalServicioWeb.DeleteAsync($"articulos/{targetProd.Id}");
            await ObtenerCatalogoRemotoAsync();
        }
    }
}


void MostrarCajaFicha(Mercaderia? mapeoInicial) {
    bool nuevo = mapeoInicial == null;
    var cuadroModal = new Dialog(nuevo ? "Ficha Comercial: Alta" : "Ficha Comercial: Modificación", 54, 14);

    var labelSku = new Label("Código SKU:") { X = 2, Y = 1 };
    var inputSku = new TextField(nuevo ? "" : mapeoInicial!.Sku) { X = 16, Y = 1, Width = 33 };

    var labelDesc = new Label("Descripción:") { X = 2, Y = 3 };
    var inputDesc = new TextField(nuevo ? "" : mapeoInicial!.Descripcion) { X = 16, Y = 3, Width = 33 };

    var labelCost = new Label("Costo Unit:") { X = 2, Y = 5 };
    var inputCost = new TextField(nuevo ? "0" : mapeoInicial!.Costo.ToString()) { X = 16, Y = 5, Width = 33 };

    var labelStock = new Label("Stock Inicial:") { X = 2, Y = 7 };
    var inputStock = new TextField(nuevo ? "0" : mapeoInicial!.ExistenciaActual.ToString()) { X = 16, Y = 7, Width = 33, ReadOnly = !nuevo };

    cuadroModal.Add(labelSku, inputSku, labelDesc, inputDesc, labelCost, inputCost, labelStock, inputStock);

    var btnGuardar = new Button("Procesar");
    btnGuardar.Clicked += async () => {
        var entidadLocal = nuevo ? new Mercaderia() : mapeoInicial!;
        entidadLocal.Sku = inputSku.Text?.ToString() ?? string.Empty;
        entidadLocal.Descripcion = inputDesc.Text?.ToString() ?? string.Empty;
        entidadLocal.Costo = decimal.TryParse(inputCost.Text?.ToString() ?? string.Empty, out decimal c) ? c : 0;
        entidadLocal.ExistenciaActual = int.TryParse(inputStock.Text?.ToString() ?? string.Empty, out int s) ? s : 0;

        if (nuevo) {
            await canalServicioWeb.PostAsJsonAsync("articulos", entidadLocal);
        } else {
            await canalServicioWeb.PutAsJsonAsync($"articulos/{entidadLocal.Id}", entidadLocal);
        }

        Application.RequestStop();
        await ObtenerCatalogoRemotoAsync();
    };

    var btnVolver = new Button("Descartar");
    btnVolver.Clicked += () => Application.RequestStop();

    cuadroModal.AddButton(btnGuardar);
    cuadroModal.AddButton(btnVolver);
    Application.Run(cuadroModal);
}

void PanelEntradaSalidaStock() {
    if (vistaFiltradaProductos.Count == 0) return;
    var prodActivo = vistaFiltradaProductos[gridListaProductos.SelectedItem];

    var dialogKardex = new Dialog("Asignar Transacción de Inventario", 52, 11);

    var lblT = new Label("Operación (0=Alta, 1=Baja, 2=Recuento):") { X = 2, Y = 1 };
    var txtT = new TextField("0") { X = 2, Y = 2, Width = 14 };

    var lblU = new Label("Cantidad de Unidades:") { X = 2, Y = 4 };
    var txtU = new TextField("1") { X = 2, Y = 5, Width = 24 };

    dialogKardex.Add(lblT, txtT, lblU, txtU);

    var btnAplicar = new Button("Asignar");
    btnAplicar.Clicked += async () => {
        int.TryParse(txtT.Text.ToString(), out int modoId);
        int.TryParse(txtU.Text.ToString(), out int unidades);

        var nuevoKardex = new RegistroKardex {
            TipoMovimiento = (TipoKardex)Math.Clamp(modoId, 0, 2),
            CantidadUnidades = Math.Abs(unidades)
        };

        await canalServicioWeb.PostAsJsonAsync($"articulos/{prodActivo.Id}/historial", nuevoKardex);
        
        Application.RequestStop();
        await ObtenerCatalogoRemotoAsync(); 
        await DescargarKardexItemAsync(prodActivo.Id); 
    };

    var btnCancelar = new Button("Volver");
    btnCancelar.Clicked += () => Application.RequestStop();

    dialogKardex.AddButton(btnAplicar);
    dialogKardex.AddButton(btnCancelar);
    Application.Run(dialogKardex);
}

_ = ObtenerCatalogoRemotoAsync();
Application.Run();
Application.Shutdown();

public class Mercaderia {
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Costo { get; set; }
    public int ExistenciaActual { get; set; }
}
public class RegistroKardex {
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public DateTime FechaHoraRegistro { get; set; } = DateTime.Now;
    public TipoKardex TipoMovimiento { get; set; }
    public int CantidadUnidades { get; set; }
}
public enum TipoKardex { Alta = 0, Baja = 1, Recuento = 2 }