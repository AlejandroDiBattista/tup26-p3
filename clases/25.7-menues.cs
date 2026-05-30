#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using Terminal.Gui.App;       // Application, IApplication, Runnable
using Terminal.Gui.Drawing;   // LineStyle
using Terminal.Gui.Input;     // Key
using Terminal.Gui.ViewBase;  // View, Pos, Dim
using Terminal.Gui.Views;     // Window, Label, MenuBar, MenuItem

using IApplication app = Application.Create().Init();

Menu.DefaultBorderStyle = LineStyle.Rounded;

Runnable raiz = new() { };

Label mensaje = new() {
    Text = "Seleccioná una opción desde la barra superior.",
    X = 1, Y = 1,
    Width = Dim.Fill(2), Height = 2,
};

MenuBar menu = new(new MenuBarItem[] {
    new("Archivo", new MenuItem[] {
        new("_Importar", "Importar desde JSON",        () => mensaje.Text = "Archivo > Importar"),
        new("_Exportar", "Exportar a JSON",            () => mensaje.Text = "Archivo > Exportar"),
        null!,
        new("_Salir", "Salir de la aplicación",        () => app.RequestStop(),                   Key.S.WithCtrl)
    }),
    new("Contacto", new MenuItem[] {
        new("_Agregar", "Agrega un nuevo contacto",    () => mensaje.Text = "Contacto > Agregar", Key.A.WithCtrl),
        new("_Editar",  "Edita un contacto existente", () => mensaje.Text = "Contacto > Editar",  Key.E.WithCtrl),
        null!,
        new("_Borrar",  "Borra un contacto existente", () => mensaje.Text = "Contacto > Borrar",  Key.B.WithCtrl)
    })
});

Window ventana = new() {
    Title = "Agenda",
    X = 0, Y = 1,
};

ventana.Add(mensaje);

raiz.Add(menu, ventana);
app.Run(raiz);