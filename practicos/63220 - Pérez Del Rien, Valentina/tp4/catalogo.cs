#!/usr/bin/env -S dotnet run
#:sdk Microsoft.NET.Sdk
#:package Terminal.Gui@2.4.3

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;
using System.Text.Json;
using System.Text;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization.Metadata;

using IApplication app = Application.Create().Init();

app.Run(new Window() { 
    Title = "Catálogo de Productos",
    Width = Dim.Fill(), 
    Height = Dim.Fill() 
});