#:property PublishAot=false

using System.Text.Json;

// === C# ===                               // === JavaScript ===
//
string? nulo = null;                        // let nulo     = null;
var numero   = 10;                          // let numero   = 10; 
var cadena   = "alejandro";                 // let cadena   = "alejandro"; 
var booleano = true;                        // let booleano = true; 
var array = new[] { 1, 2, 3, 4, 5 };        // let arreglo  = [1, 2, 3, 4, 5]; 
var objeto = new {                          // let objeto   = {
    Nulo   = nulo,                          //     "nulo": nulo,
    Numero = numero,                        //     "numero": numero,
    Cadena = cadena,                        //     "cadena": cadena,    
    Array = array,                          //     "array": array,  
    Booleano = booleano                     //     "booleano": booleano 
};                                          // };

var opcion = new JsonSerializerOptions {
    WriteIndented = true,
};

string json = JsonSerializer.Serialize(objeto, opcion);

Console.WriteLine(json);
File.WriteAllText("datos/18.2.objeto.json", json);

var multilinea = $"""
    Esto es un 
    texto que ocupa
    multiples lineas {1+2}
    puende tener "comillas" y 'comillas simples' sin problemas.
""";

var nombre   = "Alejandro";
var apellido = "Battista";
var edad     = 30;

// $$"""  Representa una cadena interpolada con formato de texto 
// sin formato (raw string literal) que permite incluir expresiones 
// interpoladas y mantener el formato original del texto, incluyendo 
// saltos de línea y caracteres especiales, sin necesidad de escapar 
// comillas o caracteres especiales.

var persona = $$"""
{
    "nombre": "{{nombre}}",
    "apellido": "{{apellido}}",
    "edad": {{edad}},
    "hobbies": ["programar", "leer", "viajar"]
}
""";

File.WriteAllText("datos/18.2.persona.json", persona);

var personaDesdeJson = JsonSerializer.Deserialize<Persona>(persona);
Console.WriteLine($"Nombre: {personaDesdeJson?.Nombre}, Apellido: {personaDesdeJson?.Apellido}, Edad: {personaDesdeJson?.Edad}");

/// === Repositorio de Contactos (con JSON) ===

var j = new Repository<Persona>("datos/18.2.contactos.json", new JsonFileSerializer<Persona>());
j.Create(new() { Nombre = "María", Apellido = "González", Edad = 25 });
j.Create(new() { Nombre = "Juan",  Apellido = "Pérez",    Edad = 40 });

// var personas = j.ReadAll();
foreach (var p in j.ReadAll()) {
    Console.WriteLine($"- Id: {p.Id}, Nombre: {p.Nombre}, Apellido: {p.Apellido}, Edad: {p.Edad}");
}

j.Update(new() { Id = 1, Nombre = "María", Apellido = "González", Edad = 26 });
j.Remove(2);


/// === JSON sin clases ===

JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

string filePath = "datos/18.2.contactos.json";

if (!File.Exists(filePath)) {
    string agendaInicial = """
    [
        { "id": 1, "nombre": "María", "apellido": "González", "edad": 25 },
        { "id": 2, "nombre": "Juan", "apellido": "Pérez", "edad": 40 }
    ]
    """;

    File.WriteAllText(filePath, agendaInicial);
}

json = File.ReadAllText(filePath);

using JsonDocument documento = JsonDocument.Parse(json);
JsonElement agenda = documento.RootElement;

foreach (JsonElement contacto in agenda.EnumerateArray()) {
    int Id = contacto.GetProperty("id").GetInt32();
    string Nombre = contacto.GetProperty("nombre").GetString() ?? "";
    string Apellido = contacto.GetProperty("apellido").GetString() ?? "";
    int Edad = contacto.GetProperty("edad").GetInt32();

    Console.WriteLine($"- Id: {Id}, Nombre: {Nombre}, Apellido: {Apellido}, Edad: {Edad}");
}

string jsonSerializado = JsonSerializer.Serialize(agenda, Options);
File.WriteAllText("datos/18.2.agenda-sin-clases.json", jsonSerializado);


/// === Clases para JSON ===

public class Persona : IEntity {
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public int Edad { get; set; }
}

// == Clases para Repositorio ===

interface IEntity {
    int Id { get; set; }
}

interface IRepository<T> where T : IEntity {
    IEnumerable<T> ReadAll();
    T? ReadOne(int id);
    void Create(T item);
    void Update(T item);
    void Remove(int id);
}


interface IFileSerializer<T> where T : class {
    IEnumerable<T> Read(string filePath);
    void Write(string filePath, IEnumerable<T> items);
}

class Repository<T> : IRepository<T> where T : class, IEntity {
    private readonly string filePath;
    private readonly IFileSerializer<T> serializer;

    public Repository(string filePath, IFileSerializer<T> serializer) {
        this.filePath   = filePath;
        this.serializer = serializer;
    }

    public IEnumerable<T> ReadAll() {
        return serializer.Read(filePath);
    }

    public T? ReadOne(int id) {
        var items = ReadAll();
        return items.FirstOrDefault(item => item.Id.Equals(id));
    }

    public void Create(T item) {
        var items = ReadAll().ToList();
        item.Id = items.Count > 0 ? items.Max(i => i.Id) + 1 : 1;
        items.Add(item);
        Save(items);
    }

    public void Update(T item) {
        var items = ReadAll().ToList();
        int index = items.FindIndex(i => i.Id.Equals(item.Id));
        if (index != -1) {
            items[index] = item;
            Save(items);
        }
    }

    public void Remove(int id) {
        var items = ReadAll().ToList();
        items.RemoveAll(i => i.Id.Equals(id));
        Save(items);
    }

    private void Save(IEnumerable<T> items) {
        serializer.Write(filePath, items);
    }
}


class JsonFileSerializer<T> : IFileSerializer<T> where T : class {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public IEnumerable<T> Read(string filePath) {
        if (!File.Exists(filePath)) return [];

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<T>>(json, Options) ?? [];
    }

    public void Write(string filePath, IEnumerable<T> items) {
        string json = JsonSerializer.Serialize(items, Options);
        File.WriteAllText(filePath, json);
    }
}

