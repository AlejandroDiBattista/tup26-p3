#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1

#pragma warning disable CS0618
#pragma warning disable CS8618

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var clienteApi = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

Application.Init();
var win = new CatalogoWindow(clienteApi);
Application.Run(win);
Application.Shutdown();
return;

// --- Carteles para esquivar el bug de la Beta ---
static class Cartel 
{
    public static void Aviso(string titulo, string texto) 
    {
        var d = new Dialog { Title = titulo, Width = 50, Height = 6 };
        d.Add(new Label { Text = texto, X = Pos.Center(), Y = 1 });
        var btn = new Button { Text = "Aceptar", X = Pos.Center(), Y = 3 };
        btn.Accepting += (s, e) => Application.RequestStop();
        d.Add(btn);
        Application.Run(d);
    }

    public static bool Confirmar(string titulo, string texto) 
    {
        var d = new Dialog { Title = titulo, Width = 50, Height = 6 };
        d.Add(new Label { Text = texto, X = Pos.Center(), Y = 1 });
        bool res = false;
        var btnSi = new Button { Text = "Si", X = 15, Y = 3 };
        var btnNo = new Button { Text = "No", X = 25, Y = 3 };
        btnSi.Accepting += (s, e) => { res = true; Application.RequestStop(); };
        btnNo.Accepting += (s, e) => { res = false; Application.RequestStop(); };
        d.Add(btnSi, btnNo);
        Application.Run(d);
        return res;
    }
}

class CatalogoWindow : Window
{
    HttpClient _api;
    List<Producto> _productos = new();
    List<Producto> _filtrados = new();
    ListView _listaProductos;
    TextView _detalleMovimientos;
    TextField _txtBuscar;

    public CatalogoWindow(HttpClient api)
    {
        _api = api;
        Title = " Sistema de Catalogo y Stock ";
        Width = Dim.Fill();
        Height = Dim.Fill();

        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem("_Archivo", new MenuItem[] {
                new MenuItem("_Salir", "", () => Application.RequestStop())
            }),
            new MenuBarItem("_Productos", new MenuItem[] {
                new MenuItem("_Nuevo", "", () => AgregarProducto()),
                new MenuItem("_Editar", "", () => EditarProducto()),
                new MenuItem("_Eliminar", "", () => EliminarProducto())
            }),
            new MenuBarItem("_Stock", new MenuItem[] {
                new MenuItem("_Registrar Movimiento", "", () => RegistrarMovimiento())
            })
        });
        Add(menu);

        Add(new Label { Text = "Buscar:", X = 1, Y = 2 });
        _txtBuscar = new TextField { Text = "", X = 9, Y = 2, Width = 40 };
        _txtBuscar.TextChanged += (s, e) => FiltrarLista();
        Add(_txtBuscar);

        var frameIzq = new FrameView { Title = " Productos (Maestro) ", X = 1, Y = 4, Width = Dim.Percent(50), Height = Dim.Fill(1) };
        _listaProductos = new ListView { Width = Dim.Fill(), Height = Dim.Fill() };
        _listaProductos.Accepting += async (s, e) => {
            e.Handled = true;
            await CargarHistorial();
        };
        frameIzq.Add(_listaProductos);

        var frameDer = new FrameView { Title = " Historial Movimientos (Enter en la lista para ver) ", X = Pos.Right(frameIzq) + 1, Y = 4, Width = Dim.Fill(1), Height = Dim.Fill(1) };
        _detalleMovimientos = new TextView { Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        frameDer.Add(_detalleMovimientos);

        Add(frameIzq, frameDer);
        
        _ = CargarDatos();
    }

    async Task CargarDatos()
    {
        try
        {
            var res = await _api.GetFromJsonAsync<List<Producto>>("/productos");
            if (res != null) _productos = res;
            FiltrarLista();
        }
        catch (Exception)
        {
            Cartel.Aviso("Error", "Fijate si el servidor esta corriendo");
        }
    }

    void FiltrarLista()
    {
        var texto = _txtBuscar.Text?.ToString()?.ToLower() ?? "";
        
        _filtrados = _productos.Where(p => 
            p.Nombre.ToLower().Contains(texto) || 
            p.Codigo.ToLower().Contains(texto)).ToList();
            
        var lineas = _filtrados.Select(p => $"{p.Codigo} | {p.Nombre} | ${p.Precio} | Stock: {p.Stock}").ToList();
        _listaProductos.SetSource(new ObservableCollection<string>(lineas));
        _detalleMovimientos.Text = ""; 
    }

    async Task CargarHistorial()
    {
        int idx = _listaProductos.SelectedItem ?? -1;
        if (idx < 0 || idx >= _filtrados.Count) return;
        
        var prod = _filtrados[idx];
        
        try
        {
            var movs = await _api.GetFromJsonAsync<List<Movimiento>>($"/productos/{prod.Id}/movimientos");
            
            var txt = $"Historial de: {prod.Nombre}\n\n";
            if (movs != null && movs.Count > 0)
            {
                foreach(var m in movs)
                {
                    txt += $"> {m.Fecha:dd/MM/yyyy HH:mm} - {m.Tipo}: {m.Cantidad} uds.\n";
                }
            }
            else
            {
                txt += "No hay movimientos.";
            }
            
            _detalleMovimientos.Text = txt;
        }
        catch
        {
            _detalleMovimientos.Text = "Error al cargar historial.";
        }
    }

    // --- ACCIONES CRUD ---

    void AgregarProducto()
    {
        var prod = new Producto();
        var dialog = new DialogoProducto(prod);
        Application.Run(dialog);
        
        if (dialog.Guardar) {
            _ = _api.PostAsJsonAsync("/productos", prod).ContinueWith(t => CargarDatos());
        }
    }

    void EditarProducto()
    {
        int idx = _listaProductos.SelectedItem ?? -1;
        if (idx < 0 || idx >= _filtrados.Count) {
            Cartel.Aviso("Aviso", "Selecciona un producto primero");
            return;
        }
        
        var prod = _filtrados[idx];
        var dialog = new DialogoProducto(prod);
        Application.Run(dialog);
        
        if (dialog.Guardar) {
            _ = _api.PutAsJsonAsync($"/productos/{prod.Id}", prod).ContinueWith(t => CargarDatos());
        }
    }

    void EliminarProducto()
    {
        int idx = _listaProductos.SelectedItem ?? -1;
        if (idx < 0 || idx >= _filtrados.Count) {
            Cartel.Aviso("Aviso", "Selecciona un producto primero");
            return;
        }
        
        var prod = _filtrados[idx];
        var confirmado = Cartel.Confirmar("Ojo", $"Seguro que queres borrar {prod.Nombre}?");
        if (confirmado) {
            _ = _api.DeleteAsync($"/productos/{prod.Id}").ContinueWith(t => CargarDatos());
        }
    }

    void RegistrarMovimiento()
    {
        int idx = _listaProductos.SelectedItem ?? -1;
        if (idx < 0 || idx >= _filtrados.Count) {
            Cartel.Aviso("Aviso", "Selecciona un producto primero");
            return;
        }
        
        var prod = _filtrados[idx];
        var dialog = new DialogoMovimiento();
        Application.Run(dialog);
        
        if (dialog.Guardar) {
            var mov = new Movimiento {
                Tipo = dialog.Tipo,
                Cantidad = dialog.Cantidad
            };
            
            _ = _api.PostAsJsonAsync($"/productos/{prod.Id}/movimientos", mov).ContinueWith(async t => {
                await CargarDatos();
                // forzamos la actualizacion del historial
                Application.Invoke(async () => await CargarHistorial());
            });
        }
    }
}

// --- DIALOGOS RUSTICOS ---

class DialogoProducto : Dialog
{
    public bool Guardar = false;
    TextField _txtCodigo, _txtNombre, _txtPrecio, _txtStock;

    public DialogoProducto(Producto p)
    {
        Title = "Formulario de Producto";
        Width = 50; Height = 14;

        Add(new Label { Text = "Codigo:", X = 1, Y = 1 });
        _txtCodigo = new TextField { Text = p.Codigo, X = 10, Y = 1, Width = 30 };
        Add(_txtCodigo);

        Add(new Label { Text = "Nombre:", X = 1, Y = 3 });
        _txtNombre = new TextField { Text = p.Nombre, X = 10, Y = 3, Width = 30 };
        Add(_txtNombre);

        Add(new Label { Text = "Precio:", X = 1, Y = 5 });
        _txtPrecio = new TextField { Text = p.Precio.ToString(), X = 10, Y = 5, Width = 30 };
        Add(_txtPrecio);

        Add(new Label { Text = "Stock:", X = 1, Y = 7 });
        _txtStock = new TextField { Text = p.Stock.ToString(), X = 10, Y = 7, Width = 30 };
        Add(_txtStock);

        var btnGuardar = new Button { Text = "Guardar", X = 10, Y = 9 };
        var btnCancelar = new Button { Text = "Cancelar", X = 25, Y = 9 };

        btnCancelar.Accepting += (s, e) => Application.RequestStop();
        btnGuardar.Accepting += (s, e) => {
            p.Codigo = _txtCodigo.Text?.ToString() ?? "";
            p.Nombre = _txtNombre.Text?.ToString() ?? "";
            decimal.TryParse(_txtPrecio.Text?.ToString(), out decimal pre);
            p.Precio = pre;
            int.TryParse(_txtStock.Text?.ToString(), out int stk);
            p.Stock = stk;
            
            Guardar = true;
            e.Handled = true;
            Application.RequestStop();
        };

        Add(btnGuardar, btnCancelar);
    }
}

class DialogoMovimiento : Dialog
{
    public bool Guardar = false;
    public string Tipo = "";
    public int Cantidad = 0;
    
    TextField _txtTipo;
    TextField _txtCant;

    public DialogoMovimiento()
    {
        Title = "Movimiento de Stock";
        Width = 50; Height = 12;

        Add(new Label { Text = "Tipo (C=Compra, V=Venta, A=Ajuste):", X = 1, Y = 2 });
        _txtTipo = new TextField { Text = "C", X = 38, Y = 2, Width = 5 };
        Add(_txtTipo);

        Add(new Label { Text = "Cantidad:", X = 1, Y = 4 });
        _txtCant = new TextField { Text = "0", X = 12, Y = 4, Width = 15 };
        Add(_txtCant);

        var btnOk = new Button { Text = "Aceptar", X = 10, Y = 7 };
        var btnCancelar = new Button { Text = "Cancelar", X = 25, Y = 7 };

        btnCancelar.Accepting += (s, e) => Application.RequestStop();
        btnOk.Accepting += (s, e) => {
            var t = _txtTipo.Text?.ToString()?.ToUpper() ?? "C";
            if (t == "V") Tipo = "Venta";
            else if (t == "A") Tipo = "Ajuste";
            else Tipo = "Compra"; // Default por si tipean mal
            
            int.TryParse(_txtCant.Text?.ToString(), out int c);
            Cantidad = c;
            
            Guardar = true;
            e.Handled = true;
            Application.RequestStop();
        };

        Add(btnOk, btnCancelar);
    }
}

class Producto 
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

class Movimiento 
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}
