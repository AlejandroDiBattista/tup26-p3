#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using Terminal.Gui.App;       // Application, IApplication, Runnable
using Terminal.Gui.ViewBase;  // View, Pos, Dim
using Terminal.Gui.Views;     // Window, FrameView, Button, TextField, etc.

using( IApplication app = Application.Create().Init()){
    Window mainWindow = new() { Title = "Tutorial Terminal.Gui v2" };

    Button editar    = new() { Title = "Editar contacto",    X = Pos.Center(), Y = Pos.Center() };
    Label  resultado = new() { Text  = "Resultado: Ninguno", X = Pos.Center(), Y = Pos.Bottom(editar) };
    
    // editar.Accepting += (_,_) => {
    //     using Editar dialog = new();
    //     app.Run(dialog);
    //     if (dialog.Result is not null) {
    //         resultado.Text = $"Resultado: {dialog.Result}";
    //     }
    // };

    editar.Accepting += (_,_) => {
        app.Run<Editar>();
        resultado.Text = $"Resultado: {app.GetResult<Contacto>()}";
    };

    mainWindow.Add(editar);
    mainWindow.Add(resultado);

    app.Run( mainWindow);
}

record Contacto(string Nombre, string Telefono) {
    public override string ToString() => $"{Nombre} ({Telefono})";
};

class Editar : Dialog<Contacto?> {
    public Editar(){
        Title = "Editar Contacto";
        Width = 60; Height = 10;

        // Agrego el campo Nombre
        Label labelNombre       = new() { Text = "  Nombre:", X = 1, Y = 1 };
        TextField inputNombre   = new() { X = Pos.Right(labelNombre) + 1, Y = 1, Width = 30 };
        Add(labelNombre, inputNombre);

        // Agrego el campo Teléfono
        Label labelTelefono     = new() { Text = "Teléfono:", X = 1, Y = 3 };
        TextField inputTelefono = new() { X = Pos.Right(labelTelefono) + 1, Y = 3, Width = 15 };
        Add(labelTelefono, inputTelefono);

        // Si cancelo retorno null
        Button btnCancelar = new() { Title = "Cancelar" };
        btnCancelar.Accepting += (_,_) => Result = null;
        AddButton(btnCancelar);

        // Si confirmo, valido y retorno el contacto
        Button btnConfirmar = new() { Title = "Confirmar" };
        btnConfirmar.Accepting += (_, e) => {
            if(string.IsNullOrWhiteSpace(inputNombre.Text) || inputNombre.Text.Length < 3) {
                e.Handled = true; // Evito que el diálogo se cierre automáticamente
                inputNombre.SetFocus();
                return;
            }

            if(string.IsNullOrWhiteSpace(inputTelefono.Text) || !inputTelefono.Text.All( char.IsDigit )) {
                e.Handled = true; // Evito que el diálogo se cierre automáticamente
                inputTelefono.SetFocus();
                return;
            }
            
            Result = new Contacto(inputNombre.Text, inputTelefono.Text);
        };
        AddButton(btnConfirmar);
    }
}