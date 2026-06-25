using Terminal.Gui;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionInventario.ServidorApp
{
    public static class Program
    {
        private static HttpClient proxyApi;
        private static List<Articulo> masterArticulos = new List<Articulo>();
        private static List<Articulo> filtradosArticulos = new List<Articulo>();
        private static List<TransaccionStock> transaccionesAudit = new List<TransaccionStock>();

        private static ListView tuiListArticulos;
        private static ListView tuiListLogs;
        private static TextField boxFiltroBusqueda;

        public static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("servidor", StringComparison.OrdinalIgnoreCase))
            {
                await Servidor.RunAsync(args.Skip(1).ToArray());
                return;
            }

            var serverThread = Servidor.RunAsync(Array.Empty<string>());
            proxyApi = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
            
            Application.Init();
            var tuiRoot = Application.Top;
            
            var mainWin = InicializarVentanaConsola();
            tuiRoot.Add(ConstruirMenuPrincipal(), mainWin);

            _ = PullDatosServidor();
            Application.Run();
            Application.Shutdown();
        }

        private static Window InicializarVentanaConsola()
        {
            var win = new Window { Title = " [ Servidor de Control / Panel Central ] ", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() - 1 };

            boxFiltroBusqueda = new TextField { X = 20, Y = 1, Width = 30 };
            boxFiltroBusqueda.TextChanged += (e) => ProcesarFiltroPantalla(boxFiltroBusqueda.Text?.ToString() ?? string.Empty);

            var panelLeft = new FrameView(" Inventario Base ") { X = 1, Y = 3, Width = Dim.Percent(50), Height = Dim.Fill() - 1 };
            tuiListArticulos = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
            tuiListArticulos.SelectedItemChanged += async (e) => await FetchHistorialArticulo();
            panelLeft.Add(tuiListArticulos);

            var panelRight = new FrameView(" Reporte de Movimientos ") { X = Pos.Right(panelLeft) + 2, Y = 3, Width = Dim.Fill() - 1, Height = Dim.Fill() - 1 };
            tuiListLogs = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
            panelRight.Add(tuiListLogs);

            win.Add(new Label("Buscar registro: ") { X = 1, Y = 1 }, boxFiltroBusqueda, panelLeft, panelRight);
            return win;
        }

        private static MenuBar ConstruirMenuPrincipal()
        {
            return new MenuBar(new MenuBarItem[] {
                new MenuBarItem("_Datos", new MenuItem[] {
                    new MenuItem("_Nuevo Registro", "", () => DesplegarModalDatos(null)),
                    new MenuItem("_Modificar", "", () => {
                        if (filtradosArticulos.Count > 0) DesplegarModalDatos(filtradosArticulos[tuiListArticulos.SelectedItem]);
                    }),
                    new MenuItem("_Borrar", "", () => EliminarArticuloBackend())
                }),
                new MenuBarItem("_Stock", new MenuItem[] {
                    new MenuItem("Registrar _Kardex", "", () => LanzarFlujoStock())
                }),
                new MenuBarItem("_Salir", "", () => Application.RequestStop())
            });
        }

        private static async Task PullDatosServidor()
        {
            try
            {
                var data = await proxyApi.GetFromJsonAsync<List<Articulo>>("articulos");
                if (data != null)
                {
                    masterArticulos = data;
                    ProcesarFiltroPantalla(boxFiltroBusqueda.Text?.ToString() ?? string.Empty);
                }
            }
            catch {}
        }

        private static void ProcesarFiltroPantalla(string text)
        {
            filtradosArticulos = string.IsNullOrWhiteSpace(text)
                ? masterArticulos
                : masterArticulos.Where(a => a.Descripcion.Contains(text, StringComparison.OrdinalIgnoreCase) || a.Sku.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();

            tuiListArticulos.SetSource(filtradosArticulos.Select(i => $"SKU: {i.Sku} | {i.Descripcion} | Price: ${i.Costo} | Stock: {i.ExistenciaActual}").ToList());
        }

        private static async Task FetchHistorialArticulo()
        {
            if (filtradosArticulos.Count > 0 && tuiListArticulos.SelectedItem < filtradosArticulos.Count)
            {
                try
                {
                    var id = filtradosArticulos[tuiListArticulos.SelectedItem].Id;
                    var list = await proxyApi.GetFromJsonAsync<List<TransaccionStock>>($"articulos/{id}/historial");
                    if (list != null)
                    {
                        transaccionesAudit = list;
                        tuiListLogs.SetSource(transaccionesAudit.Select(t => $"[{t.MomentoRegistro:yyyy-MM-dd}] Mod: {t.Variante} -> Cant: {t.Unidades}").ToList());
                        return;
                    }
                }
                catch {}
            }
            tuiListLogs.SetSource(new List<string>());
        }

        private static async void EliminarArticuloBackend()
        {
            if (filtradosArticulos.Count > 0)
            {
                var row = filtradosArticulos[tuiListArticulos.SelectedItem];
                if (MessageBox.Query("Ventana de confirmación", $"¿Remover item: {row.Descripcion}?", "Confirmar", "Cancelar") == 0)
                {
                    await proxyApi.DeleteAsync($"articulos/{row.Id}");
                    await PullDatosServidor();
                }
            }
        }

        private static void DesplegarModalDatos(Articulo? modelo)
        {
            bool isAlta = modelo == null;
            var dial = new Dialog(isAlta ? "Ficha Técnica: Inserción" : "Ficha Técnica: Actualización", 52, 14);

            var bSku = new TextField(isAlta ? "" : modelo!.Sku) { X = 15, Y = 1, Width = 32 };
            var bDesc = new TextField(isAlta ? "" : modelo!.Descripcion) { X = 15, Y = 3, Width = 32 };
            var bCost = new TextField(isAlta ? "0" : modelo!.Costo.ToString()) { X = 15, Y = 5, Width = 32 };
            var bStk = new TextField(isAlta ? "0" : modelo!.ExistenciaActual.ToString()) { X = 15, Y = 7, Width = 32, ReadOnly = !isAlta };

            dial.Add(new Label("Código SKU:") { X = 2, Y = 1 }, bSku, new Label("Detalle:") { X = 2, Y = 3 }, bDesc,
                     new Label("Costo:") { X = 2, Y = 5 }, bCost, new Label("Stock Fijo:") { X = 2, Y = 7 }, bStk);

            var btnConfirm = new Button("Confirmar");
            btnConfirm.Clicked += async () => {
                var ent = isAlta ? new Articulo() : modelo!;
                ent.Sku = bSku.Text?.ToString() ?? string.Empty;
                ent.Descripcion = bDesc.Text?.ToString() ?? string.Empty;
                ent.Costo = decimal.TryParse(bCost.Text?.ToString(), out decimal pC) ? pC : 0;
                ent.ExistenciaActual = int.TryParse(bStk.Text?.ToString(), out int pS) ? pS : 0;

                _ = isAlta ? await proxyApi.PostAsJsonAsync("articulos", ent) : await proxyApi.PutAsJsonAsync($"articulos/{ent.Id}", ent);
                Application.RequestStop();
                await PullDatosServidor();
            };

            var btnAbort = new Button("Abortar");
            btnAbort.Clicked += () => Application.RequestStop();

            dial.AddButton(btnConfirm);
            dial.AddButton(btnAbort);
            Application.Run(dial);
        }

        private static void LanzarFlujoStock()
        {
            if (filtradosArticulos.Count == 0) return;
            var current = filtradosArticulos[tuiListArticulos.SelectedItem];

            var dial = new Dialog("Inyectar Flujo de Unidades", 52, 11);
            var inModo = new TextField("0") { X = 2, Y = 2, Width = 12 };
            var inVol = new TextField("1") { X = 2, Y = 5, Width = 22 };

            dial.Add(new Label("Modo (0=Ingreso, 1=Egreso, 2=Ajuste):") { X = 2, Y = 1 }, inModo, new Label("Volumen numérico:") { X = 2, Y = 4 }, inVol);

            var btnApply = new Button("Aplicar");
            btnApply.Clicked += async () => {
                int.TryParse(inModo.Text.ToString(), out int t);
                int.TryParse(inVol.Text.ToString(), out int v);

                var tx = new TransaccionStock { Variante = (ModoMovimiento)Math.Clamp(t, 0, 2), Unidades = Math.Abs(v) };
                await proxyApi.PostAsJsonAsync($"articulos/{current.Id}/historial", tx);

                Application.RequestStop();
                await PullDatosServidor();
                await FetchHistorialArticulo();
            };

            var btnCancel = new Button("Cerrar");
            btnCancel.Clicked += () => Application.RequestStop();

            dial.AddButton(btnApply);
            dial.AddButton(btnCancel);
            Application.Run(dial);
        }
    }

    public class Articulo
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public int ExistenciaActual { get; set; }
    }

    public class TransaccionStock
    {
        public int Id { get; set; }
        public int ArticuloId { get; set; }
        public DateTime MomentoRegistro { get; set; } = DateTime.Now;
        public ModoMovimiento Variante { get; set; }
        public int Unidades { get; set; }
    }

    public enum ModoMovimiento { Alta = 0, Baja = 1, Recuento = 2 }
}