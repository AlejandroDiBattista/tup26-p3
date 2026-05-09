#:package Terminal.Gui@2.0.1-develop.1
#:package Microsoft.Data.Sqlite@10.0.0

using System.Runtime.CompilerServices;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

var usaArchivoPorDefecto = args.Length == 0;
var archivoPath = !usaArchivoPorDefecto
    ? Path.GetFullPath(args[0])
    : Path.Combine(SourceDirectory(), "agenda.json");

IUnitOfWork<Contacto> unidadDeTrabajo = CrearUnidadDeTrabajo(archivoPath);

using IApplication app = Application.Create();
app.Init();

using var root = new Runnable {
    Width  = Dim.Fill(),
    Height = Dim.Fill()
};

Menu.DefaultBorderStyle = LineStyle.Single;

var menu    = new MenuBar { /* ... */ };
var ventana = new Window  { /* ... */ };
// Configurar ventana, barra de menú, lista, etc.

root.Add(menu, ventana);

Actualizar();
app.Run(root);


static string SourceDirectory([CallerFilePath] string sourcePath = "") {
    return Path.GetDirectoryName(sourcePath) ?? Directory.GetCurrentDirectory();
}

static IUnitOfWork<Contacto> CrearUnidadDeTrabajo(string path) {
    return Path.GetExtension(path).ToLowerInvariant() switch {
        ".json" => new UnitOfWork<Contacto>(new JsonContactoStore(path)),
        ".db"   => new UnitOfWork<Contacto>(new SqliteContactoStore(path)),
        _       => throw new ArgumentException("La agenda debe usar un archivo .json o .db.", nameof(path)),
    };
}

void Actualizar(int? seleccionarId = null) {
    // TODO: cargar contactos desde unidadDeTrabajo, aplicar filtro de búsqueda, actualizar vista y mantener seleccionado el contacto con id igual a seleccionarId si existe.
    // ActualizarTitulo();
    // lista.SetNeedsDraw();
}

public interface IEntity {
    int Id { get; set; }
}

public sealed class Contacto : IEntity {
    public int Id { get; set; }
    public string Nombre    { get; set; } = "";
    public string Apellido  { get; set; } = "";
    public string Domicilio { get; set; } = "";
    public string Telefonos { get; set; } = ""; // Separados por coma, ej: "12345678,87654321"
}

public interface IRepository<T> where T : class, IEntity {
    IReadOnlyList<T> ReadAll();
    T? ReadOne(int id);
    void Create(T item);
    void Update(T item);
    void Remove(int id);
}

public interface IUnitOfWork<T> : IRepository<T> where T : class, IEntity {
    bool HasPendingChanges { get; }
    void SaveChanges();
}

public interface IStore<T> where T : class, IEntity {
    IReadOnlyList<T> Load();
    void SaveChanges(IEnumerable<StoreChange<T>> changes);
}

public enum StoreChangeKind { Insert, Update, Delete, }
public sealed record StoreChange<T>(StoreChangeKind Kind, int Id, T? Entity = null) where T : class, IEntity;

public enum TrackingState { Unchanged, Added, Modified, Deleted }
public sealed record TrackedEntity<T>(T Entity, TrackingState State) where T : class, IEntity;

public sealed class UnitOfWork<T> : IUnitOfWork<T> where T : class, IEntity {
    private readonly IStore<T> store;
    private readonly Dictionary<int, TrackedEntity<T>> tracking = new();

    public UnitOfWork(IStore<T> store) {
        this.store = store;
    }

    public bool HasPendingChanges => false;

    public IReadOnlyList<T> ReadAll() {
        // TODO: cargar entidades desde el store y aplicar tracking en memoria.
        _ = store;
        return [];
    }

    public T? ReadOne(int id) {
        // TODO: recuperar una entidad por id desde el seguimiento en memoria.
        return null;
    }

    public void Create(T item) {
        // TODO: asignar id y registrar el alta como cambio pendiente.
    }

    public void Update(T item) {
        // TODO: marcar la entidad como modificada.
    }

    public void Remove(int id) {
        // TODO: marcar la entidad como eliminada.
    }

    public void SaveChanges() {
        // TODO: traducir el tracking a StoreChange<T> y delegar en el store.
    }
}

public sealed class JsonContactoStore : IStore<Contacto> {
    public JsonContactoStore(string path) {
        Path = path;
    }

    private string Path { get; }

    public IReadOnlyList<Contacto> Load() {
        // TODO: cargar contactos desde JSON usando Path.
        return [];
    }

    public void SaveChanges(IEnumerable<StoreChange<Contacto>> changes) {
        // TODO: persistir los cambios en formato JSON usando Path.
    }
}

public sealed class SqliteContactoStore : IStore<Contacto> {
    public SqliteContactoStore(string path) {
        Path = path;
    }

    private string Path { get; }

    public IReadOnlyList<Contacto> Load() {
        // TODO: cargar contactos desde SQLite usando ADO.NET y Path.
        return [];
    }

    public void SaveChanges(IEnumerable<StoreChange<Contacto>> changes) {
        // TODO: persistir altas, bajas y modificaciones en SQLite usando ADO.NET.
    }
}
