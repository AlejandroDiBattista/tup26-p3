#!/usr/bin/dotnet run

#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property PublishAot=false
#:property PublishTrimmed=false
#:property TrimMode=copyused
#:property EnableTrimAnalyzer=false

#:package Terminal.Gui@2.0.0-v2-develop.400
#:package Microsoft.Data.Sqlite@9.0.0
#:package Dapper@2.1.35
#:package Dapper.Contrib@2.0.78


using System;
using Terminal.Gui;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;



Application.Init();

Window ventana = new Window()
{
    Title = "Agenda"
};

Application.Run(ventana);
Application.Shutdown();