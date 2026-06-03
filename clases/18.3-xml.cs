
#:property PublishAot=false

using System.Xml.Serialization;
using System.Xml.Linq;

// Aprovechemos la inferencia de tipo para usar new() en lugar de new Persona() al crear objetos de tipo Persona y Agenda, lo que hace el código más conciso y legible.

Agenda agenda = new () {
    Contactos = [
        new () { Nombre = "María", Apellido = "González",  Edad = 25 },
        new () { Nombre = "Juan",  Apellido = "Pérez",     Edad = 40 },
        new () { Nombre = "Ana",   Apellido = "Martínez",  Edad = 31 },
        new () { Nombre = "Luis",  Apellido = "Rodríguez", Edad = 28 },
        new () { Nombre = "Sofía", Apellido = "López",     Edad = 22 }
    ]
};

Directory.CreateDirectory("datos");

/// === XML ===
//
// Serializar Agenda en formato XML
var serializadorXml = new XmlSerializer(typeof(Agenda));
using (var archivoXml = File.Create("datos/18.3.agenda.xml")) {
    serializadorXml.Serialize(archivoXml, agenda);
}

// Leer Agenda desde formato XML
using (var archivoXml = File.OpenRead("datos/18.3.agenda.xml")) {
    var agendaXml = (Agenda)serializadorXml.Deserialize(archivoXml)!;

    Console.WriteLine("\n === Contactos cargados desde XML ===");
    foreach (var p in agendaXml.Contactos) {
        Console.WriteLine($"- Nombre: {p.Nombre}, Apellido: {p.Apellido}, Edad: {p.Edad}");
    }
}

/// === Repositorio de Contactos (con XML) ===

var x = new Repository<Persona>("datos/18.3.contactos.xml", new XmlFileSerializer<Persona>());
x.Create(new() { Nombre = "María", Apellido = "González", Edad = 25 });
x.Create(new() { Nombre = "Juan",  Apellido = "Pérez",    Edad = 40 });

var personasXml = x.ReadAll();
Console.WriteLine("\n === Contactos cargados desde XML ===");
foreach (var p in personasXml) {
    Console.WriteLine($"- Id: {p.Id}, Nombre: {p.Nombre}, Apellido: {p.Apellido}, Edad: {p.Edad}");
}
x.Update(new() { Id = 1, Nombre = "María", Apellido = "González", Edad = 26 });
x.Remove(2);

/// === XML sin clases ===

var agendaSinClases = new XElement("Agenda",
    new XElement("Contactos",
        new XElement("Contacto",
            new XElement("Nombre", "Ana"),
            new XElement("Apellido", "Martínez"),
            new XElement("Edad", 31)
        ),
        new XElement("Contacto",
            new XElement("Nombre", "Luis"),
            new XElement("Apellido", "Rodríguez"),
            new XElement("Edad", 28)
        )
    )
);

// <Agenda>
//   <Contactos>
//     <Contacto>
//       <Nombre>Ana</Nombre>
//       <Apellido>Martínez</Apellido>
//       <Edad>31</Edad>
//     </Contacto>
//     <Contacto>
//       <Nombre>Luis</Nombre>
//       <Apellido>Rodríguez</Apellido>
//       <Edad>28</Edad>
//     </Contacto>
//   </Contactos>
// </Agenda>



agendaSinClases.Save("datos/18.3.agenda-sin-clases.xml");

var agendaXmlSinClases = XElement.Load("datos/18.3.agenda-sin-clases.xml");

Console.WriteLine("\n === Agenda cargada desde XML sin clases ===");
foreach (var contacto in agendaXmlSinClases.Element("Contactos")!.Elements("Contacto")) {
    Console.WriteLine($$"""
            - Nombre:   {{contacto.Element("Nombre")!.Value}} 
              Apellido: {{contacto.Element("Apellido")!.Value}}, 
              Edad:     {{int.Parse(contacto.Element("Edad")!.Value)}}
        """ );
}

/// === Clases para XML ===

public class Agenda {
    public List<Persona> Contactos { get; set; } = [];
}

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

interface IRepository<T> where T : class {
    IEnumerable<T> ReadAll();
    T? ReadOne(int id);
    void Create(T item);
    void Update(T item);
    void Remove(int id);
}

interface IFileSerializer<T> where T : class {
    List<T> Read(string filePath);
    void Write(string filePath, IEnumerable<T> items);
}

class Repository<T> : IRepository<T> where T : class, IEntity {
    private readonly string filePath;
    private readonly IFileSerializer<T> serializer;

    public Repository(string filePath, IFileSerializer<T> serializer) {
        this.filePath = filePath;
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


class XmlFileSerializer<T> : IFileSerializer<T> where T : class {
    private static readonly XmlSerializer Serializer = new(typeof(List<T>));

    public List<T> Read(string filePath) {
        if (!File.Exists(filePath)) return [];

        using var stream = File.OpenRead(filePath);
        return (List<T>?)Serializer.Deserialize(stream) ?? [];
    }

    public void Write(string filePath, IEnumerable<T> items) {
        using var stream = File.Create(filePath);
        Serializer.Serialize(stream, items.ToList());
    }
}
