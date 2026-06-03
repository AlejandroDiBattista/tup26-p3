#!/usr/bin/env -S dotnet run
//
// Ejemplo completo: consultar una tabla de alumnos con LINQ + SQLite.
//
// Requisitos:
// 1. Tener .NET 10 SDK instalado.
// 2. Ejecutar este archivo desde su carpeta con:
//    dotnet run 17.3.linq-sqlite.cs
//
// El ejemplo hace estos pasos:
// 1. Crea una base SQLite local llamada alumnos.sqlite.
// 2. Crea la tabla Alumnos.
// 3. Inserta datos de ejemplo.
// 4. Ejecuta una consulta LINQ que EF Core traduce a SQL.
// 5. Muestra el SQL generado y los resultados.

#:package Microsoft.EntityFrameworkCore.Sqlite@10.0.0
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using static System.Console;

var dbPath = Path.Combine(Environment.CurrentDirectory, "alumnos.sqlite");
Clear();
WriteLine($"Base SQLite: \n\n{dbPath}");

using var context = new EscuelaContext(dbPath);

// Rehacemos la base en cada ejecución para que el ejemplo sea repetible.
context.Database.EnsureDeleted();
context.Database.EnsureCreated();

CargarDatos(context);

var consulta = context.Alumnos
	.Where(alumno => alumno.Aprobado && alumno.Edad >= 21)
	.OrderBy(alumno => alumno.Nombre)
	.Select(alumno => new AlumnoResumen(alumno.Legajo, alumno.Nombre, alumno.Edad));

// Formulación equivalente con sintaxis de consulta:
var consultaSQL = from alumno in context.Alumnos
    where alumno.Aprobado && alumno.Edad >= 21
    orderby alumno.Nombre
    select new AlumnoResumen(alumno.Legajo, alumno.Nombre, alumno.Edad);

MostrarConsulta("== SQL generado por la consulta LINQ ==", consulta);
MostrarConsulta("== SQL generado por la consulta LINQ con sintaxis de consulta ==", consultaSQL);

var resultados = consulta.ToList();

WriteLine();
WriteLine("Resultado de la consulta:");
foreach (var alumno in resultados) {
	WriteLine($"- {alumno.Legajo} | {alumno.Nombre,-20} | {alumno.Edad} años");
}

var promedioAprobados = context.Alumnos
	.Where(alumno => alumno.Aprobado)
	.Average(alumno => alumno.Edad);

WriteLine();
WriteLine($"Edad promedio de los aprobados: {promedioAprobados:0.0}");

static void CargarDatos(EscuelaContext context) {
	if (context.Alumnos.Any()) {
		return;
	}

	context.Alumnos.AddRange(
		new Alumno { Nombre = "Ana",     Edad = 20, Aprobado = true,  Legajo = 51001 },
		new Alumno { Nombre = "Bruno",   Edad = 24, Aprobado = true,  Legajo = 51002 },
		new Alumno { Nombre = "Carla",   Edad = 22, Aprobado = false, Legajo = 51003 },
		new Alumno { Nombre = "Diego",   Edad = 21, Aprobado = true,  Legajo = 51004 },
		new Alumno { Nombre = "Elena",   Edad = 19, Aprobado = true,  Legajo = 51005 },
		new Alumno { Nombre = "Facundo", Edad = 25, Aprobado = true,  Legajo = 51006 }
	);

	context.SaveChanges();
}

static void MostrarConsulta(string titulo, IQueryable<AlumnoResumen> consulta) {
	WriteLine(titulo);
    WriteLine();
    foreach(var linea in consulta.ToQueryString().Split("\n")) {
        WriteLine($"  {linea}");
    }
	WriteLine();
}

class Alumno {
	public int Id { get; set; }
	public int Legajo { get; set; }
	public string Nombre { get; set; } = string.Empty;
	public int Edad { get; set; }
	public bool Aprobado { get; set; }
}

readonly record struct AlumnoResumen(int Legajo, string Nombre, int Edad);

class EscuelaContext(string dbPath) : DbContext {
	public DbSet<Alumno> Alumnos => Set<Alumno>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		optionsBuilder.UseSqlite($"Data Source={dbPath}");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<Alumno>(entity => {
			entity.ToTable("Alumnos");
			entity.HasKey(alumno => alumno.Id);
			entity.HasIndex(alumno => alumno.Legajo).IsUnique();

			entity.Property(alumno => alumno.Nombre)
				.IsRequired()
				.HasMaxLength(100);
		});
	}
}


