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

var clienteApi = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

Application.Init();
var win = new CatalogoWindow(clienteApi);
Application.Run(win);
Application.Shutdown();
return;

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
                new MenuItem("_Nuevo", "", () => MessageBox.Query("Info", "Pronto", "OK")),
                new MenuItem("_Editar", "", () => MessageBox.Query("Info", "Pronto", "OK")),
                new MenuItem("_Eliminar", "", () => MessageBox.Query("Info", "Pronto", "OK"))
            }),
            new MenuBarItem("_Stock", new MenuItem[] {
                new MenuItem("_Registrar Movimiento", "", () => MessageBox.Query("Info", "Pronto", "OK"))
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

        // Llamo a los datos de una al arrancar
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
            MessageBox.ErrorQuery("Error", "Fijate si el servidor.cs esta corriendo", "OK");
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
        _detalleMovimientos.Text = ""; // limpio el historial si cambia la busqueda
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
                txt += "No hay movimientos registrados.";
            }
            
            _detalleMovimientos.Text = txt;
        }
        catch
        {
            _detalleMovimientos.Text = "Error al cargar historial.";
        }
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
