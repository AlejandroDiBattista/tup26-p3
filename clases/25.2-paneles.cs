#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using Terminal.Gui.App;       // Application, IApplication, Runnable
using Terminal.Gui.ViewBase;  // View, Pos, Dim
using Terminal.Gui.Views;     // Window, FrameView, Button, TextField, etc.

using( IApplication app = Application.Create().Init()){
    Window mainWindow = new() { Title = "Tutorial Terminal.Gui v2" };

    FrameView maestro = new() {
        Title = "Maestro",
        X = 0, Y = 0,
        Width = Dim.Percent(25), Height = Dim.Fill()
    };
    FrameView detalle = new() {
        Title = "Detalle",
        X = Pos.Right(maestro) + 5,
        Width = Dim.Fill(), Height = Dim.Fill()
    };
    mainWindow.Add(maestro, detalle);

    app.Run( mainWindow);
}
