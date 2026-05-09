#:property PublishAot=false

using System.Text.Json;
using System.Text.Json.Serialization;

string path = "tareas.json";

List<Tareas> tareas = new() {
    new Tareas(true, "Hacer la cama"),
    new Tareas(false, "Lavar los platos"),
    new Tareas(true, "Sacar la basura"),
    new Tareas(false, "Limpiar el baño")
};  

var opciones = new JsonSerializerOptions { WriteIndented = true, };

var json = JsonSerializer.Serialize(tareas, opciones);
Console.WriteLine(json);
File.WriteAllText(path, json);


// Leer JSON de un archivo
if (File.Exists(path)) {
    string jsonDesdeArchivo = File.ReadAllText(path);
    var tareasDesdeArchivo = JsonSerializer.Deserialize<List<Tareas>>(jsonDesdeArchivo);
    Console.WriteLine("\nTareas cargadas desde archivo:");
    tareasDesdeArchivo?.ForEach(t => Console.WriteLine($"- {t.Descripcion, -30} (Completa: {t.Completa})"));
}   

/// Leer JSON con JsonDocument 
/// 
using JsonDocument documento = JsonDocument.Parse(json);
Console.WriteLine("\nTareas leidas con JsonDocument:");
foreach (JsonElement tarea in documento.RootElement.EnumerateArray()) {
    bool   completa    = tarea.GetProperty("Completa").GetBoolean();
    string descripcion = tarea.GetProperty("Descripcion").GetString() ?? "";
    Console.WriteLine($"- {descripcion, -30} (Completa: {completa})");
}

using FileStream archivoJsonDocument = File.Create("tareas-jsondocument.json");
using var escritorJsonDocument = new Utf8JsonWriter(archivoJsonDocument, new JsonWriterOptions {
    Indented = true,
});
documento.RootElement.WriteTo(escritorJsonDocument);
escritorJsonDocument.Flush();

record Tareas(bool Completa, string Descripcion);

