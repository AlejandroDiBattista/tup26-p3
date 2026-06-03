#!/usr/bin/env dotnet
#:property PublishAot=false

#:package Microsoft.Data.Sqlite@*
#:package Dapper@*
#:package Dapper.Contrib@*

using Microsoft.Data.Sqlite;
using Dapper;
using System.Data.Common;
using Dapper.Contrib.Extensions;

using DbConnection db = new SqliteConnection("Data Source=contactos.db");
db.Open();

using DbCommand commando = db.CreateCommand();
commando.CommandText = """
    CREATE TABLE IF NOT EXISTS Contactos (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Nombre TEXT NOT NULL,
        Apellido TEXT NOT NULL,
        Telefono TEXT NOT NULL,
        Email TEXT NOT NULL
    );
    """;
commando.ExecuteNonQuery();

var adri = new Contacto(){ Nombre = "Adrián",   Apellido = "Andrade",  Telefono = "555-0001", Email = "adrian@example.com"   };
db.Insert(adri);

db.Insert<Contacto>(new(){ Nombre = "Betina",   Apellido = "Benites",  Telefono = "555-0002", Email = "betina@example.com"   });
db.Insert<Contacto>(new(){ Nombre = "Carlos",   Apellido = "Cabrera",  Telefono = "555-0003", Email = "carlos@example.com"   });
db.Insert<Contacto>(new(){ Nombre = "Diana",    Apellido = "Díaz",     Telefono = "555-0004", Email = "diana@example.com"    });
db.Insert<Contacto>(new(){ Nombre = "Eduardo",  Apellido = "Escobar",  Telefono = "555-0005", Email = "eduardo@example.com"  });
db.Insert<Contacto>(new(){ Nombre = "Fernanda", Apellido = "Figueroa", Telefono = "555-0006", Email = "fernanda@example.com" });
db.Insert<Contacto>(new(){ Nombre = "Gustavo",  Apellido = "González", Telefono = "555-0007", Email = "gustavo@example.com"  });
db.Insert<Contacto>(new(){ Nombre = "Hilda",    Apellido = "Herrera",  Telefono = "555-0008", Email = "hilda@example.com"    });
db.Insert<Contacto>(new(){ Nombre = "Ignacio",  Apellido = "Ibáñez",   Telefono = "555-0009", Email = "ignacio@example.com"  });
db.Insert<Contacto>(new(){ Nombre = "Julia",    Apellido = "Jiménez",  Telefono = "555-0010", Email = "julia@example.com"    });

foreach (Contacto c in db.GetAll<Contacto>()) {
    Console.WriteLine($"{c.Id}: {c.Nombre} {c.Apellido} - {c.Telefono} - {c.Email}");
}

var carlos = db.Get<Contacto>(3);
db.Delete(carlos);

// Dapper (Convierte automáticamente entre filas de la base de datos y objetos C#)

int contactos = db.QuerySingle<int>("""
    SELECT COUNT(*)
    FROM Contactos;
    """);
Console.WriteLine($"Quedan {contactos} contactos en la base de datos.");

var contactosServidor = db.Query<Contacto>("""
    SELECT * FROM Contactos 
    WHERE Nombre > @nombre
    """, new { nombre = "Carlos" }).ToList();

Console.WriteLine("\n== Contactos filtrados en el servidor ==");
Console.WriteLine("ID Nombre               Apellido             Teléfono     Email");
foreach (Contacto c in contactosServidor) {
    Console.WriteLine($"{c.Id,3}: {c.Nombre,-20} {c.Apellido,-20} - {c.Telefono} - {c.Email}");
}

var contactosClientes = db.GetAll<Contacto>()
                .Where(c => string.Compare(c.Nombre, "Carlos") > 0);

Console.WriteLine("\n== Contactos filtrados en el cliente ==");
Console.WriteLine("ID Nombre               Apellido             Teléfono     Email");
foreach (Contacto c in contactosClientes) {
    Console.WriteLine($"{c.Id,3}: {c.Nombre,-20} {c.Apellido,-20} - {c.Telefono} - {c.Email}");
}


class Contacto {
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Email { get; set; } = "";
}
