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