#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using System.Collections.ObjectModel;
using Terminal.Gui.App;       // Application, IApplication, Runnable
using Terminal.Gui.ViewBase;  // View, Pos, Dim
using Terminal.Gui.Views;     // Window, FrameView, Button, TextField, etc.

List<Contacto> agenda = [
    new("Ana",      "Andrade",   "(381)456-7890", 20),
    new("Bruno",    "Bianchi",   "(381)567-8901", 35),
    new("Carla",    "Cortés",    "(381)678-9012", 28),
    new("Diego",    "Díaz",      "(381)789-0123", 42),
    new("Elena",    "Espinosa",  "(381)890-1234", 30),
    new("Federico", "Fernández", "(381)901-2345", 45),
    new("Gabriela", "García",    "(381)012-3456", 25)
];

using( IApplication app = Application.Create().Init()){
    Window mainWindow= new() { Title = "Tutorial Terminal.Gui v2" };

    Label etiqueta   = new() { Text = "Buscar:", X = 1, Y = 1 };
    TextField buscar = new() { X = Pos.Right(etiqueta), Y = 1, Width = 43 };
    
    ListView maestro = new() { X = 1, Y = 3, Width = 50, Height = Dim.Fill(4) };
    Markdown detalle = new() { Text = "", X = Pos.Right(maestro) + 1, Y = 3, Width = 40, Height = Dim.Fill(4) };
    
    List<Contacto> filtrados = [];

    void Actualizar() {
        string texto = buscar.Text ?? "";
        filtrados = agenda.Where(c => c.Coincide(texto)).ToList();
        maestro.SetSource(new ObservableCollection<string>(filtrados.Select(c => c.ToString()).ToList()));
    }

    buscar.TextChanged += (_, _) => Actualizar();
    Actualizar();

    maestro.ValueChanged += (_, _) => {
        int indice = maestro.SelectedItem ?? 0;
        if (indice >= 0 && indice < filtrados.Count) {
            Contacto contacto = filtrados[indice];
            detalle.Text = $"""
                # Contacto

                **{contacto.NombreCompleto}**

                *Teléfono:* **{contacto.Telefono}**
                
                *Edad:* **{contacto.Edad}** años
                """;
        } else {
            detalle.Text = "_Sin contacto seleccionado._";
        }
    };

    mainWindow.Add(etiqueta, buscar, maestro, detalle);

    app.Run( mainWindow);
}

record Contacto(string Nombre, string Apellido, string Telefono, int Edad) {
    public string NombreCompleto => $"{Apellido}, {Nombre}";
    
    public bool Coincide(string texto) =>
        NombreCompleto.Contains(texto.Trim(), StringComparison.OrdinalIgnoreCase)
        || Telefono.Contains(texto.Trim());

    public override string ToString() => $" {NombreCompleto,-22} | {Telefono,10} | {Edad,2} años";
};