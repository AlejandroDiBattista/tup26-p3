#:package Microsoft.Data.Sqlite@10.0.0

// using System.Data;
using Microsoft.Data.Sqlite;

// Crear una conexión a una base de datos SQLite (se crea el archivo si no existe).

// Es SQLite las base de datos son un archivo local, por lo que se especifica la ruta al archivo. 
// Si el archivo no existe, SQLite lo crea automáticamente.

using var connection = new SqliteConnection("Data Source=app.db");
// Tip: Se puede usar ":memory:" para crear una base de datos en memoria, que se pierde al cerrar la conexión.
// using var connection = new SqliteConnection("Data Source=:memory:");

connection.Open();

// Los comandos SQL se ejecutar con DBCommand.

var command = connection.CreateCommand();
// Preparo el comando.
Console.WriteLine("\n=== Creando tabla Personas... ===");
command.CommandText = """
        CREATE TABLE IF NOT EXISTS 
            Personas (
                Id        INTEGER PRIMARY KEY, 
                Nombre    TEXT, 
                Apellido  TEXT, 
                Edad      INTEGER
            )
    """;

// Lo ejecuto.
command.ExecuteNonQuery();
// ExecuteNonQuery() se usa para comandos que no devuelven resultados (CREATE, INSERT, UPDATE, DELETE).

// Como insertar datos...
// Comando INSERT
Console.WriteLine("\n=== Insertando datos... ===");
command.CommandText = """
    INSERT INTO Personas (Nombre, Apellido, Edad) 
        VALUES (@nombre, @apellido, @edad)
""";

// Los parámetros se agregan con AddWithValue, usando el mismo nombre que en la consulta SQL (con @).
// IMPORTANTE: Siempre usar parámetros para evitar inyecciones SQL y problemas con caracteres especiales.

command.Parameters.AddWithValue("@nombre", "María");
command.Parameters.AddWithValue("@apellido", "González");
command.Parameters.AddWithValue("@edad", 26);
command.ExecuteNonQuery();

// // Como borrar datos...
// command.CommandText = "DELETE FROM Personas WHERE Nombre = @nombre";
// command.Parameters.Clear(); // Limpiar parámetros anteriores
// command.Parameters.AddWithValue("@nombre", "María");
// command.ExecuteNonQuery();

// Como actualizar datos...
// IMPORTANTE: sin WHERE se actualizan todos los registros, así que siempre incluir una condición para evitar cambios masivos no deseados.

Console.WriteLine("\n=== Actualizando datos... ===");
command.CommandText = "UPDATE Personas SET Edad = @edad WHERE Nombre = @nombre";
command.Parameters.Clear();
command.Parameters.AddWithValue("@nombre", "María");
command.Parameters.AddWithValue("@edad", 27);
// new {nombre = "María", edad = 27} --- IGNORE ---
command.ExecuteNonQuery();

Console.WriteLine("\n=== Consultando datos... ===");
command.CommandText = """
    SELECT Id, Nombre, Apellido, Edad 
    FROM Personas 
    WHERE Edad >= @edadMinima
""";
command.Parameters.Clear();
command.Parameters.AddWithValue("@edadMinima", 18);

Console.WriteLine("\nPersonas mayores de 18 años:");
using var reader = command.ExecuteReader();
while (reader.Read()) {
    var id       = reader.GetInt32(0); // Índice de la columna (0 para Id, 1 para Nombre, etc.)
    var nombre   = reader.GetString(1);
    var apellido = reader.GetString(2);
    var edad     = reader.GetInt32(3);  
    Console.WriteLine($"Id: {id}, Nombre: {nombre}, Apellido: {apellido}, Edad: {edad}");
}
connection.Close();