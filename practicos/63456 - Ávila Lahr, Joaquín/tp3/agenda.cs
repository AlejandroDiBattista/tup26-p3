#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;

Console.OutputEncoding = System.Text.Encoding.UTF8;
string archivo = args.Length > 0 ? args[0] : "agenda.db";
SqliteAgendaStore store = new($"Data Source={archivo}");

try {
    store.Inicializar();
}
catch (Exception ex) {
    Console.WriteLine("Error al abrir la base de datos:");
    Console.WriteLine(ex.Message);
    return;
}

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store));
