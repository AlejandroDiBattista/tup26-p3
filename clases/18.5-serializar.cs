#:property PublishAot=false
using System.Text.Json;
using System.Xml.Serialization;


var serializer = FileSerializerFactory.Create<Persona>("datos/18.4.contactos.json");
var db = new Repository<Persona>(serializer);

db.Create(new() { Nombre = "Ana",  Apellido = "Araujo",   Edad = 25 });
db.Create(new() { Nombre = "Beto", Apellido = "Benites",  Edad = 40 });
db.Create(new() { Nombre = "Caro", Apellido = "Cabrera",  Edad = 31 });
db.Create(new() { Nombre = "Dani", Apellido = "Díaz",     Edad = 28 });
db.Create(new() { Nombre = "Ema",  Apellido = "Espinoza", Edad = 22 });

Console.WriteLine("\n === Contactos cargados desde JSON ===");
foreach (var p in db.ReadAll()) {
    Console.WriteLine($"- Id: {p.Id,2}, Nombre: {p.Nombre, -20}, Apellido: {p.Apellido,-20}, Edad: {p.Edad}");
}

var c = db.ReadOne(3);
if (c != null) {
    c.Edad = 32;
    db.Update(c);
}

db.Remove(2);

Console.WriteLine(".");

public class Persona : IEntity {
    public int    Id       { get; set; }
    public string Nombre   { get; set; } = "";
    public string Apellido { get; set; } = "";
    public int    Edad     { get; set; }
}

// == Clases para Repositorio ===

// Esta es la base en común que debe tener cualquier entidad que queramos manejar en el
// repositorio, para poder asignarle un Id único.

interface IEntity {
    int Id { get; set; }
}

// El repositorio es una clase genérica que nos permite manejar cualquier tipo de entidad que implemente IEntity,
// y que se encargue de leer y escribir los datos en un archivo utilizando un serializador.

interface IRepository<T> where T : IEntity {
    IEnumerable<T> ReadAll();
    T? ReadOne(int id);
    void Create(T item);
    void Update(T item);
    void Remove(int id);
}

// Esta interfaz define los métodos que debe implementar cualquier clase que se encargue de serializar y deserializar objetos de tipo T en un archivo.
interface IFileSerializer<T> where T : class {
    IEnumerable<T> Read();
    void Write(IEnumerable<T> items);
}


// == Implementación de Repositorio con Serialización ===

// Esta clase es una fábrica de serializadores, que nos permite crear un serializador adecuado según la extensión del archivo.
static class FileSerializerFactory {
    public static IFileSerializer<T> Create<T>(string filePath) where T : class {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch {
            ".json" => new JsonFileSerializer<T>(filePath),
            ".xml"  => new XmlFileSerializer<T>(filePath),
            _       => throw new NotSupportedException($"No hay serializador para la extensión '{ext}'.")
        };
    }
}

// Esta clase es una implementación del repositorio que utiliza un serializador para leer y escribir los datos en un archivo.

class Repository<T> : IRepository<T> where T : class, IEntity {
    private readonly IFileSerializer<T> serializer;
    private Dictionary<int, T> datos = new();

    public Repository(IFileSerializer<T> serializer) {
        this.serializer = serializer;
        foreach (var item in serializer.Read()) {
            datos[item.Id] = item;
        }
    }

    public IEnumerable<T> ReadAll() {
        return datos.Values;
    }

    public T? ReadOne(int id) {
        return datos.GetValueOrDefault(id);
    }

    public void Create(T item) {
        item.Id = datos.Count > 0 ? datos.Values.Max(i => i.Id) + 1 : 1;
        datos[item.Id] = item;
        Save(datos.Values);
    }

    public void Update(T item) {
        if (datos.ContainsKey(item.Id)) {
            datos[item.Id] = item;
            Save(datos.Values);
        }
    }

    public void Remove(int id) {
        if (datos.Remove(id)) {
            Save(datos.Values);
        }
    }

    private void Save(IEnumerable<T> items) {
        serializer.Write(items);
    }
}


class JsonFileSerializer<T> : IFileSerializer<T> where T : class {
    private string filePath;

    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public JsonFileSerializer(string filePath) {
        this.filePath = filePath;
    } 

    public IEnumerable<T> Read() {
        if (!File.Exists(filePath)) return [];

        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<List<T>>(stream, Options) ?? [];
    }

    public void Write(IEnumerable<T> items) {
        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, items, Options);
    }
}

class XmlFileSerializer<T> : IFileSerializer<T> where T : class {
    private string filePath;
    private static readonly XmlSerializer Serializer = new(typeof(List<T>));

    public XmlFileSerializer(string filePath) {
        this.filePath = filePath;
    }

    public IEnumerable<T> Read() {
        if (!File.Exists(filePath)) return [];

        using var stream = File.OpenRead(filePath);
        return (IEnumerable<T>?)Serializer.Deserialize(stream) ?? [];
    }

    public void Write(IEnumerable<T> items) {
        using var stream = File.Create(filePath);
        Serializer.Serialize(stream, items.ToList());
    }
}
