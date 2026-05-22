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
using System.Data.Common;
using Dapper.Contrib.Extensions;


string destino = args.FirstOrDefault() ?? ":memory:";
using SqliteAgendaStore agendaStore = new(destino);
agendaStore.Inicializar();
using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(agendaStore, destino));
