using Terminal.Gui;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionInventario.Cliente
{
    public static class Program
    {
        private static HttpClient apiControler;
        private static List<Mercaderia> cacheItems = new List<Mercaderia>();
        private static List<Mercaderia> registrosFiltrados = new List<Mercaderia>();
        private static List<RegistroKardex> logKardex = new List<RegistroKardex>();

        private static ListView viewCatalogo;
        private static ListView viewKardex;
        private static TextField inputFiltro;

        public static async Task Main(string[] args)
        {
            if (args.Length > 0 && args.Contains("servidor", StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            apiControler = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
            Application.Init();
            
            var UI_Top = Application.Top;
            var layoutPrincipal = CrearContenedoresUI();
            
            UI_Top.Add(CrearMenuSistema(), layoutPrincipal);
            
            _ = CargarDatosBackend();
            Application.Run();
            Application.Shutdown();
        }

        private static Window CrearContenedoresUI()
        {
            var win = new Window { Title = " [ S.O.M.I. - Catálogo y Gestión Visual de Existencias ] ", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() - 1 };
            
            var lbl = new Label("Texto a filtrar: ") { X = 2, Y = 1 };
            inputFiltro = new TextField { X = 20, Y = 1, Width = 38 };
            inputFiltro.TextChanged += (e) => FiltrarCatalogoLocal(inputFiltro.Text?.ToString() ?? string.Empty);

            var frameLeft = new FrameView(" Listado Comercial de Productos ") { X = 1, Y = 3, Width = Dim.Percent(55), Height = Dim.Fill() - 1 };
            viewCatalogo = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
            viewCatalogo.SelectedItemChanged += async (idx) => await ConsultarHistorialKardex();
            frameLeft.Add(viewCatalogo);

            var frameRight = new FrameView(" Historial de Movimientos (Kardex) ") { X = Pos.Right(frameLeft) + 1, Y = 3, Width = Dim.Fill() - 1, Height = Dim.Fill() - 1 };
            viewKardex = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
            frameRight.Add(viewKardex);

            win.Add(lbl, inputFiltro, frameLeft, frameRight);
            return win;
        }

        private static MenuBar CrearMenuSistema()
        {
            return new MenuBar(new MenuBarItem[] {
                new MenuBarItem("_Artículos", new MenuItem[] {
                    new MenuItem("_Ingresar Producto", "", () => MostrarVentanaFicha(null)),
                    new MenuItem("_Editar Ficha", "", () => {
                        if (registrosFiltrados.Count > 0) MostrarVentanaFicha(registrosFiltrados[viewCatalogo.SelectedItem]);
                    }),
                    new MenuItem("_Remover Registro", "", () => EjecutarEliminacionItem())
                }),
                new MenuBarItem("_Inventariar", new MenuItem[] {
                    new MenuItem("Ajustar _Unidades", "", () => EjecutarAjusteManual())
                }),
                new MenuBarItem("_Cerrar", "", () => Application.RequestStop())
            });
        }

        private static async Task CargarDatosBackend()
        {
            try
            {
                var response = await apiControler.GetFromJsonAsync<List<Mercaderia>>("articulos");
                if (response != null)
                {
                    cacheItems = response;
                    FiltrarCatalogoLocal(inputFiltro.Text?.ToString() ?? string.Empty);
                }
            }
            catch {}
        }

        private static void FiltrarCatalogoLocal(string query)
        {
            registrosFiltrados = string.IsNullOrWhiteSpace(query) 
                ? cacheItems 
                : cacheItems.Where(x => x.Descripcion.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Sku.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            viewCatalogo.SetSource(registrosFiltrados.Select(p => $"[SKU: {p.Sku}] {p.Descripcion} -> Costo: ${p.Costo} | Stock: {p.ExistenciaActual} uds.").ToList());
        }

        private static async Task ConsultarHistorialKardex()
        {
            if (registrosFiltrados.Count > 0 && viewCatalogo.SelectedItem < registrosFiltrados.Count)
            {
                try
                {
                    var id = registrosFiltrados[viewCatalogo.SelectedItem].Id;
                    var res = await apiControler.GetFromJsonAsync<List<RegistroKardex>>($"articulos/{id}/historial");
                    if (res != null)
                    {
                        logKardex = res;
                        viewKardex.SetSource(logKardex.Select(k => $"{k.FechaHoraRegistro:dd/MM/yyyy} | Operación: {k.TipoMovimiento} -> Cantidad: {k.CantidadUnidades}").ToList());
                        return;
                    }
                }
                catch {}
            }
            viewKardex.SetSource(new List<string>());
        }

        private static async void EjecutarEliminacionItem()
        {
            if (registrosFiltrados.Count > 0)
            {
                var item = registrosFiltrados[viewCatalogo.SelectedItem];
                if (MessageBox.Query("Alerta de Sistema", $"¿Dar de baja: {item.Descripcion}?", "SÍ", "NO") == 0)
                {
                    await apiControler.DeleteAsync($"articulos/{item.Id}");
                    await CargarDatosBackend();
                }
            }
        }

        private static void MostrarVentanaFicha(Mercaderia? entidad)
        {
            boolisNew = entidad == null;
            var dial = new Dialog(isNew ? "Ficha Comercial: Alta" : "Ficha Comercial: Modificación", 54, 14);

            var fSku = new TextField(isNew ? "" : entidad!.Sku) { X = 16, Y = 1, Width = 33 };
            var fDesc = new TextField(isNew ? "" : entidad!.Descripcion) { X = 16, Y = 3, Width = 33 };
            var fCost = new TextField(isNew ? "0" : entidad!.Costo.ToString()) { X = 16, Y = 5, Width = 33 };
            var fStk = new TextField(isNew ? "0" : entidad!.ExistenciaActual.ToString()) { X = 16, Y = 7, Width = 33, ReadOnly = !isNew };

            dial.Add(new Label("Código SKU:") { X = 2, Y = 1 }, fSku, new Label("Descripción:") { X = 2, Y = 3 }, fDesc,
                     new Label("Costo Unit:") { X = 2, Y = 5 }, fCost, new Label("Stock Inicial:") { X = 2, Y = 7 }, fStk);

            var btnOk = new Button("Procesar");
            btnOk.Clicked += async () => {
                var obj = isNew ? new Mercaderia() : entidad!;
                obj.Sku = fSku.Text?.ToString() ?? string.Empty;
                obj.Descripcion = fDesc.Text?.ToString() ?? string.Empty;
                obj.Costo = decimal.TryParse(fCost.Text?.ToString(), out decimal c) ? c : 0;
                obj.ExistenciaActual = int.TryParse(fStk.Text?.ToString(), out int s) ? s : 0;

                _ = isNew ? await apiControler.PostAsJsonAsync("articulos", obj) : await apiControler.PutAsJsonAsync($"articulos/{obj.Id}", obj);
                Application.RequestStop();
                await CargarDatosBackend();
            };

            var btnCancel = new Button("Descartar");
            btnCancel.Clicked += () => Application.RequestStop();

            dial.AddButton(btnOk);
            dial.AddButton(btnCancel);
            Application.Run(dial);
        }

        private static void EjecutarAjusteManual()
        {
            if (registrosFiltrados.Count == 0) return;
            var target = registrosFiltrados[viewCatalogo.SelectedItem];

            var dial = new Dialog("Asignar Transacción de Inventario", 52, 11);
            var fModo = new TextField("0") { X = 2, Y = 2, Width = 14 };
            var fCant = new TextField("1") { X = 2, Y = 5, Width = 24 };

            dial.Add(new Label("Operación (0=Alta, 1=Baja, 2=Recuento):") { X = 2, Y = 1 }, fModo, new Label("Cantidad de Unidades:") { X = 2, Y = 4 }, fCant);

            var btnSave = new Button("Asignar");
            btnSave.Clicked += async () => {
                int.TryParse(fModo.Text.ToString(), out int m);
                int.TryParse(fCant.Text.ToString(), out int q);

                var k = new RegistroKardex { TipoMovimiento = (TipoKardex)Math.Clamp(m, 0, 2), CantidadUnidades = Math.Abs(q) };
                await apiControler.PostAsJsonAsync($"articulos/{target.Id}/historial", k);
                
                Application.RequestStop();
                await CargarDatosBackend();
                await ConsultarHistorialKardex();
            };

            var btnBack = new Button("Volver");
            btnBack.Clicked += () => Application.RequestStop();

            dial.AddButton(btnSave);
            dial.AddButton(btnBack);
            Application.Run(dial);
        }
    }

    public class Mercaderia
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public int ExistenciaActual { get; set; }
    }

    public class RegistroKardex
    {
        public int Id { get; set; }
        public int ArticuloId { get; set; }
        public DateTime FechaHoraRegistro { get; set; } = DateTime.Now;
        public TipoKardex TipoMovimiento { get; set; }
        public int CantidadUnidades { get; set; }
    }

    public enum TipoKardex { Alta = 0, Baja = 1, Recuento = 2 }
}