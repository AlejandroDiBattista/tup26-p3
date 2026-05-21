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
        Title  = "Agenda";
        Width  = Dim.Fill();
        Height = Dim.Fill();

        Button boton = new() {
            Text = "¡Confirmar!",
            X = Pos.Center(),
            Y = Pos.Center()
        };

        Label resultado = new() {
            Text = "",
            X = Pos.Center(),
            Y = Pos.Bottom(boton) + 1
        };

        // Definimos un diálogo de confirmación que se mostrará al hacer click en el botón
        Dialog confirmar = new() {
            Title  = "Confirmar acción",
            Width  = 60,
            Height = 10
        };

        confirmar.Add(new Label() {
            Text = "¿Estás seguro de que quieres confirmar esta acción?",
            X = Pos.Center(),
            Y = 2
        });
        confirmar.AddButton(new Button(){Title = "Cancelar"});  // Resultado 0
        confirmar.AddButton(new Button(){Title = "Confirmar"}); // Resultado 1

        boton.Accepting += (_,_) => {
            // Apila el diálogo de confirmación y espera a que el usuario elija una opción
            App!.Run(confirmar);
            if (confirmar.Result == 1) {
                resultado.Text = "Resultado: ¡Confirmado!";
            } else {
                resultado.Text = "Resultado: Cancelado.";
            }
        };

        Add(boton, resultado);
    }
}
