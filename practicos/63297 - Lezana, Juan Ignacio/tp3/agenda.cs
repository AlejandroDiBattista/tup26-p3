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

[Table("Contactos")]
public sealed class Ctc {
    [Key] public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tels { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Fav { get; set; }
    public Ctc Clonar() => new() { Id = Id, Nombre = Nombre, Tels = Tels, Email = Email, Notas = Notas, Fav = Fav };
}
public sealed class SqliteAlmacenCtc : IDisposable {

    readonly SqliteConnection cn;

    public SqliteAlmacenCtc(string arch) {
        cn = new(new SqliteConnectionStringBuilder {
            DataSource = arch
        }.ConnectionString);

        cn.Open();
    }

    public void CrearTablas() => cn.Execute("""
        CREATE TABLE IF NOT EXISTS Contactos(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Tels TEXT NOT NULL DEFAULT '',
            Email TEXT NOT NULL DEFAULT '',
            Notas TEXT NOT NULL DEFAULT '',
            Fav INTEGER NOT NULL DEFAULT 0
        );
    """);

    public IEnumerable<Ctc> ObtenerTodos()
        => cn.GetAll<Ctc>();

    public Ctc Agregar(Ctc ctc) {
        ctc.Id = 0;
        ctc.Id = Convert.ToInt32(cn.Insert(ctc));
        return ctc;
    }
}
public void Modificar(Ctc ctc) {
    Validar(ctc);
    cn.Update(ctc);
}

public void Borrar(Ctc ctc)
    => cn.Delete(ctc);

static void Validar(Ctc ctc) {

    if (string.IsNullOrWhiteSpace(ctc.Nombre))
        throw new InvalidOperationException(
            "El nombre no puede estar vacío."
        );

    if (!string.IsNullOrWhiteSpace(ctc.Email)
        && !ctc.Email.Contains('@'))
        throw new InvalidOperationException(
            "El email debe contener @."
        );
}
using IApplication apl = Application.Create().Init();
apl.Run(new VentanaCtc(almacen, archBd));

public sealed class VentanaCtc : Runnable {

    readonly ListView listaVis;
    readonly TextView panelDet;
    readonly Label barEstado;

    public VentanaCtc(
        SqliteAlmacenCtc almacen,
        string bd
    ) {

        Title = "Agenda - Terminal.Gui";

        Width = Dim.Fill();
        Height = Dim.Fill();

        listaVis = new() {
            X = 0,
            Y = 3,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1),
            Title = "Contactos"
        };

        panelDet = new() {
            X = Pos.Right(listaVis) + 1,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            Title = "Detalle"
        };

        barEstado = new() {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "Listo."
        };

        Add(listaVis, panelDet, barEstado);
    }
}
readonly TextField cajaBusq;
bool soloFav;

cajaBusq = new() {
    X = 10,
    Y = 1,
    Width = Dim.Fill(1)
};

cajaBusq.TextChanged += (_, _) => ActVista();

void ActVista() {

    string texto = cajaBusq.Text?.ToString() ?? "";

    visibles.Clear();

    visibles.AddRange(
        listaCtc.Where(ctc =>
            Coincide(ctc, texto)
            && (!soloFav || ctc.Fav)
        )
        .OrderBy(ctc => ctc.Nombre)
    );

    filas.Clear();

    foreach (Ctc ctc in visibles)
        filas.Add(
            $"{(ctc.Fav ? "*" : " ")} " +
            $"{ctc.Nombre} - {ctc.Tels}"
        );

    listaVis.SetSource(filas);
}