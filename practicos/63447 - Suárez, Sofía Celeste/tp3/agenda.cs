#!/usr/bin/env dotnet
#:property PublishAot=false
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow());

public sealed class AgendaWindow : Runnable {

    private SqliteAgendaStore _store;

    private List<Contacto> _contactos = new();
    private List<Contacto> _contactosFiltrados = new();

    private ListView _listView = null!;
    private Label _lblDetalles = null!;

    private TextField _txtBusqueda = null!;
    private bool _soloFavoritos = false;

    public AgendaWindow() {

        Title = "AGENDA DE CONTACTOS";
        BorderStyle = LineStyle.Single;
        Width = Dim.Fill();
        Height = Dim.Fill();

        Menu.DefaultBorderStyle = LineStyle.Single;

        var args = Environment.GetCommandLineArgs();
        string dbPath = args.Length > 2 ? args.Last() : "agenda.db";

        try {
           _store = new SqliteAgendaStore(dbPath);
            BuildLayout();
            LoadData();
        }
        catch (Exception ex) {
           MessageBox.Query(
               App!,
               "Error de Base de Datos",
               $"No se pudo abrir la base de datos.\n\n{ex.Message}",
               "OK"
            );

           Environment.Exit(1);
        }
        
    }
    private void BuildLayout() {
        MenuBar menu = new() {
        Menus = [
           new MenuBarItem ("_Archivo", [
              new MenuItem(
                   "_Importar JSON",
                   "Ctrl+I",
                    () => Importar()
                ),
              new MenuItem(
                   "_Exportar JSON",
                   "Ctrl+E",
                   () => Exportar()
                ),
               null!,
             new MenuItem(
                   "_Salir",
                   "Ctrl+X",
                   () => SolicitarSalir()
                )
            ]),

            new MenuBarItem("_Contactos", [
                new MenuItem(
                 "_Nuevo",
                  "F2",
                  () => NuevoContacto()
                ),
                new MenuItem(
                   "_Editar",
                   "F3",
                    () => EditarContacto()
                ),
                new MenuItem(
                  "_Eliminar",
                  "Del",
                   () => EliminarContacto()
                )
            ]),

            new MenuBarItem("_Ver", [
              new MenuItem(
                  "_Solo favoritos",
                   "",
                   () => {
                      _soloFavoritos = !_soloFavoritos;
                       AplicarFiltros();
                    }
                )
            ]),

            new MenuBarItem("_Ayuda", [
                new MenuItem(
                   "_Acerca de",
                   "",
                   () => {
                       MessageBox.Query(
                           App!,
                           "Acerca de",
                           "Agenda de Contactos",
                           "OK"
                       );
                   }
                )
            ])
        ]};
    }
    Label lblBuscar = new() {
        Text = "Buscar:",
        X = 1,
        Y = 2
    };

    _txtBusqueda = new TextField() {
        X = 10,
        Y = 2,
        Width = 30,
        CanFocus = true
    };

    _txtBusqueda.TextChanged += (_, _) => {
        AplicarFiltros();
    };

    FrameView panelLista = new() {
        Title = "Contactos",
        X = 0,
        Y = 4,
        Width = Dim.Percent(40),
        Height = Dim.Fill()
    };

    _listView = new ListView() {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        CanFocus = true
    };

    _listView.ValueChanged += (_, _) => {
        MostrarDetalles();
    };

    panelLista.Add(_listView);

     FrameView panelDetalle = new() {

        Title = "Detalles",

        X = Pos.Right(panelLista),
        Y = 4,

        Width = Dim.Fill(),
        Height = Dim.Fill()
    };

    _lblDetalles = new Label() {

        X = 1,
        Y = 1,

        Width = Dim.Fill(1),
        Height = Dim.Fill(1)
    };

    panelDetalle.Add(_lblDetalles);
        Add(
        menu,
        lblBuscar,
        _txtBusqueda,
        panelLista,
     panelDetalle
    );

    private void LoadData() {
        _contactos = _store.GetAll();
        AplicarFiltros();
    }

    private void AplicarFiltros() {
        string filtro = _txtBusqueda.Text?.ToString()?.Trim().ToLower() ?? "";
        _contactosFiltrados = _contactos
            .Where(c => {
                bool coincideBusqueda =
                    string.IsNullOrWhiteSpace(filtro)
                    || c.Nombre.ToLower().Contains(filtro)
                    || c.Telefonos.ToLower().Contains(filtro)
                    || c.Email.ToLower().Contains(filtro);
                bool coincideFavorito =
                !_soloFavoritos
                || c.Favorito;
                return coincideBusqueda && coincideFavorito;
            })
            .ToList();
        var items = _contactosFiltrados
            .Select(c => $"{(c.Favorito ? "★ " : "  ")}{c.Nombre}")
            .ToList();
           _listView.SetSource<string>(
               new ObservableCollection<string>(items)
            );
        MostrarDetalles();
    }

    private void MostrarDetalles() {
        int idx = _listView.SelectedItem ?? -1;
        if (idx >= 0 && idx < _contactosFiltrados.Count) {
            var c = _contactosFiltrados[idx];
            _lblDetalles.Text =
            $"Nombre: {c.Nombre}\n" +
            $"Email: {c.Email}\n" +
            $"Teléfonos: {c.Telefonos}\n" +
            $"Favorito: {(c.Favorito ? "Sí" : "No")}\n\n" +
            $"Notas:\n{c.Notas}";
        }
        else {
            _lblDetalles.Text = "(Ningún contacto seleccionado)";
        }
    }

    private void AlternarFavorito() {
        int idx = _listView.SelectedItem ?? -1;
        if (idx < 0 || idx >= _contactosFiltrados.Count)
         return;
        var contacto = _contactosFiltrados[idx];
        contacto.Favorito = !contacto.Favorito;
        _store.Update(contacto);
        LoadData();
    }

    private void NuevoContacto() {
        var dialog = new ContactoDialog();
        App!.Run(dialog);
        if (dialog.Resultado != null) {
            _store.Insert(dialog.Resultado);
            LoadData();
        }
    }

    private void EditarContacto() {
        int idx = _listView.SelectedItem ?? -1;
        if (idx < 0 || idx >= _contactosFiltrados.Count)
        return;
        var original = _contactosFiltrados[idx];
        var dialog = new ContactoDialog(original);
        App!.Run(dialog);
        if (dialog.Resultado != null) {
            var confirmar =
            new ConfirmarDialog($"¿Guardar los cambios en {original.Nombre}?");
            App!.Run(confirmar);
            if (confirmar.Confirmado) {
                _store.Update(dialog.Resultado);
                LoadData();
            }
        }
    }

    private void EliminarContacto() {
        int idx = _listView.SelectedItem ?? -1;
        if (idx < 0 || idx >= _contactosFiltrados.Count)
            return;
        var contacto = _contactosFiltrados[idx];
        var dialog =
            new ConfirmarDialog($"¿Eliminar a {contacto.Nombre}?");
        App!.Run(dialog);
        if (dialog.Confirmado) {
            _store.Delete(contacto);
            LoadData();
        }
    }

    private void Importar() {
        var confirmar =
        new ConfirmarDialog("¿Desea importar los contactos desde el archivo JSON?");
        App!.Run(confirmar);
        if (confirmar.Confirmado) {
            List<Contacto> importados;
           try {
                importados = JsonAgendaIO.Importar("contactos.json");
            }
            catch (FileNotFoundException) {
               MessageBox.Query(
                 App!,
                 "Error",
                 "El archivo JSON no existe.",
                 "OK"
               );
                return;
           }
           catch (JsonException) {
              MessageBox.Query(
                  App!,
                  "Error",
                  "El archivo JSON tiene un formato inválido.",
                   "OK"
                );
                return;
            }
            catch (Exception ex) {
              MessageBox.Query(
                  App!,
                  "Error",
                  $"No se pudo importar el archivo.\n\n{ex.Message}",
                 "OK"
                );
             return;
            }
            foreach (var c in importados) {
                c.Id = 0;
                _store.Insert(c);
            }
            LoadData();
        }
    }

    private void Exportar() {
        try {
          SaveDialog saveDialog = new() {
              Title = "Exportar JSON"
            };
            App!.Run(saveDialog);
            if (saveDialog.Canceled)
            return;
            string path = saveDialog.FileName?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(path))
            return;
            if (!path.EndsWith(".json"))
            path += ".json";
            JsonAgendaIO.Exportar(path, _contactos);
            MessageBox.Query(
              App!,
               "Exportación",
               "Contactos exportados correctamente.",
               "OK"
           );
        }
        catch (Exception ex) {
           MessageBox.Query(
               App!,
               "Error",
               $"No se pudo exportar el JSON.\n\n{ex.Message}",
               "OK"
           );
        }
       
    }

    private void SolicitarSalir() {
            App!.RequestStop();
        }
        protected override bool OnKeyDown(Key key) {
        if (key == Key.CursorUp || key == Key.CursorDown) {
            MostrarDetalles();
        }

        if (key == Key.X.WithCtrl) {
            SolicitarSalir();
            return true;
        }

        if (key == Key.F2) {
            NuevoContacto();
            return true;
        }

        if (key == Key.F3) {
            EditarContacto();
            return true;
        }

        if (key == Key.F4) {
            _txtBusqueda.SetFocus();
            return true;
        }

        if (key == Key.DeleteChar) {
            EliminarContacto();
            return true;
        }

        if (key == Key.I.WithCtrl) {
            Importar();
            return true;
        }

        if (key == Key.E.WithCtrl) {
            Exportar();
            return true;
        }
        return base.OnKeyDown(key);
    }
}
