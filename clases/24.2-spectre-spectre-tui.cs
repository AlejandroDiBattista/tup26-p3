#!/usr/bin/env -S dotnet run
#:package Spectre.Console.Cli@0.55.0
#:property PublishAot=false

using Spectre.Console;

Console.Clear();
// Muestra la portada.
var figlet = new FigletText("Pizza Ahora!");
AnsiConsole.Write(figlet);

// Texto con formato y colores.
AnsiConsole.MarkupLine("[green]Las pizzas mas [bold]ricas[/][/]\n\n");

// Ingreso de datos con formato.
var nombre = AnsiConsole.Ask<string>("Cual es tu [green]nombre[/]?");
AnsiConsole.MarkupLine($"Bienvenido, [blue]{nombre}[/]!\n");

// Tablas con bordes y estilos.
var table = new Table()
    .RoundedBorder()
    .Title("Menú de la pizzería", new Style(foreground: Color.Magenta, decoration: Decoration.Bold))
    .Caption("[grey]Pizzas disponibles hoy[/]");

table.AddColumn("Pizza")
    .AddColumn("Ingredientes")
    .AddColumn("Precio", col => col.RightAligned());
  
table.AddRow("Muzzarella", "Salsa de tomate, muzzarella y orégano",  "$8.500");
table.AddRow("Napolitana", "Muzzarella, tomate fresco y ajo",        "$9.800");
table.AddRow("Fugazzeta",  "Muzzarella y cebolla",                   "$9.500");
table.AddRow("Especial",   "Muzzarella, jamón, morrón y aceitunas", "$10.900");
  
AnsiConsole.Write(table);

// Elección de opciones con prompts interactivos.
var size = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Que tamaño de pizza [green]quiere[/]?")
        .AddChoices("Pequeña", "Mediana", "Grande", "Extra Grande"));
  
AnsiConsole.MarkupLine($"Tu [red]tamaño[/] de pizza es : [yellow]{size}[/]");

// Elección de múltiples opciones.
var toppings = AnsiConsole.Prompt(
    new MultiSelectionPrompt<string>()
        .Title("Que [green]ingredientes[/] te gustaria?")
        .NotRequired()
        .InstructionsText("[grey](Presiona [blue]<space>[/] para alternar, [green]<enter>[/] para confirmar)[/]")
        .AddChoices("Pepperoni", "Champiñones", "Salchicha", "Cebolla", "Pimientos verdes", "Aceitunas negras"));
AnsiConsole.MarkupLine($"Ingredientes: [yellow]{string.Join(", ", toppings)}[/]");

// Confirmación de acciones.
if(AnsiConsole.Confirm("Desea agregar [green]salsa extra[/]?")) {
    AnsiConsole.MarkupLine("Salsa extra agregada.");
} else {
    AnsiConsole.MarkupLine("No se agrego salsa extra.");
}

AnsiConsole.Status()
    .Start("Cocinando Pizza...", ctx => {
        Thread.Sleep(500);
  
        ctx.Status("Cortando ingredientes...");
        Thread.Sleep(1000);
  
        ctx.Status("Horneando...");
        Thread.Sleep(1000);
    });
  
AnsiConsole.MarkupLine("\n[green][bold]Pizza esta lista![/][/]");

AnsiConsole.Progress().Start(ctx => {
    var task = ctx.AddTask("Cerrando el local", maxValue: 100);

    while (!ctx.IsFinished) {
        task.Increment(1);
        Thread.Sleep(20);
    }
});

