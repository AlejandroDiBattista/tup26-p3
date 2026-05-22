#!/usr/bin/env dotnet  
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Text.Json;
using System.Collections.ObjectModel;

string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store;
try
{
    store = new SqliteAgendaStore(dbPath);
    store.EnsureSchema();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error al abrir la base de datos '{dbPath}': {ex.Message}");
    Environment.Exit(1);
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));