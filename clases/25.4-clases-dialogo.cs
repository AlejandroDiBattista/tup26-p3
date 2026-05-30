#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using Terminal.Gui.App;       // Application, IApplication, Runnable
using Terminal.Gui.Drawing;   // LineStyle, colores y dibujo
using Terminal.Gui.Input;     // Key, Command
using Terminal.Gui.ViewBase;  // View, Pos, Dim
using Terminal.Gui.Views;     // Window, FrameView, Button, TextField, etc.

using( IApplication app = Application.Create().Init()){
    app.Run<Agenda>();
}

class Agenda : Window {
    public Agenda() {
        Title = "Agenda";

        Button boton     = new() { Text = "¡Confirmar!", X = Pos.Center(), Y = Pos.Center() };
        Label  resultado = new() { Text = "Resultado: ", X = Pos.Center(), Y = Pos.Bottom(boton) + 1 };

        boton.Accepting += (_,_) => {
            using Confirmar confirmar = new("¿Estás seguro de que quieres confirmar esta acción?");
            App!.Run(confirmar);
            resultado.Text = "Resultado " + (confirmar.Result==1 ? "¡Confirmado!" : "Cancelado.");
        };

        Add(boton);
        Add(resultado);
    }
}

class Confirmar : Dialog {
    public Confirmar(string Mensaje){
        Title  = "Confirmar acción";
        Width  = 60; Height = 10;

        Add(new Label() { Text = Mensaje, X = Pos.Center(), Y = 2 });

        AddButton(new Button(){Title = "Cancelar" });
        AddButton(new Button(){Title = "Confirmar"});
    }
}