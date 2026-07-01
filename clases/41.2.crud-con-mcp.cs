#:package ModelContextProtocol@1.4.0
#:package Microsoft.Extensions.Hosting@10.0.0
#:package Microsoft.EntityFrameworkCore.Sqlite@10.0.0
#:property PublishAot=false

using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var dbPath = Path.Combine(Environment.CurrentDirectory, "41.2-agenda.sqlite");

using (var db = new AgendaContext(dbPath)) {
    db.Database.EnsureCreated();
}

var builder = Host.CreateApplicationBuilder(args);

// En transporte stdio, stdout queda reservado para JSON-RPC.
// Cualquier log debe salir por stderr para no romper el protocolo MCP.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => {
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton(new AgendaService(dbPath));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AgendaTools>();

await builder.Build().RunAsync();

[McpServerToolType]
public class AgendaTools(AgendaService agenda) {
    [McpServerTool(Name = "crear_contacto")]
    [Description("Crea un contacto nuevo en la agenda persistida en SQLite.")]
    public ContactoDto CrearContacto(
        [Description("Nombre del contacto.")] string nombre,
        [Description("Apellido del contacto. Puede estar vacio.")] string apellido = "",
        [Description("Email del contacto. Puede estar vacio.")] string email = "",
        [Description("Telefono del contacto. Puede estar vacio.")] string telefono = "",
        [Description("Direccion postal del contacto. Puede estar vacia.")] string direccion = "",
        [Description("Notas libres sobre el contacto. Puede estar vacio.")] string notas = "") {
        return agenda.Crear(nombre, apellido, email, telefono, direccion, notas);
    }

    [McpServerTool(Name = "listar_contactos")]
    [Description("Lista contactos de la agenda. Si se indica busqueda, filtra por nombre, apellido, email o telefono.")]
    public IReadOnlyList<ContactoDto> ListarContactos(
        [Description("Texto opcional para buscar contactos.")] string busqueda = "",
        [Description("Cantidad maxima de contactos a devolver, entre 1 y 100.")] int limite = 20) {
        return agenda.Listar(busqueda, limite);
    }

    [McpServerTool(Name = "obtener_contacto")]
    [Description("Obtiene un contacto por su identificador.")]
    public ContactoDto ObtenerContacto(
        [Description("Identificador numerico del contacto.")] int id) {
        return agenda.Obtener(id);
    }

    [McpServerTool(Name = "actualizar_contacto")]
    [Description("Actualiza los datos de un contacto existente. Los campos null no se modifican.")]
    public ContactoDto ActualizarContacto(
        [Description("Identificador numerico del contacto a actualizar.")] int id,
        [Description("Nuevo nombre. Usar null para no cambiar.")] string? nombre = null,
        [Description("Nuevo apellido. Usar null para no cambiar.")] string? apellido = null,
        [Description("Nuevo email. Usar null para no cambiar.")] string? email = null,
        [Description("Nuevo telefono. Usar null para no cambiar.")] string? telefono = null,
        [Description("Nueva direccion. Usar null para no cambiar.")] string? direccion = null,
        [Description("Nuevas notas. Usar null para no cambiar.")] string? notas = null) {
        return agenda.Actualizar(id, nombre, apellido, email, telefono, direccion, notas);
    }

    [McpServerTool(Name = "eliminar_contacto")]
    [Description("Elimina un contacto de la agenda por su identificador.")]
    public OperacionDto EliminarContacto(
        [Description("Identificador numerico del contacto a eliminar.")] int id) {
        agenda.Eliminar(id);
        return new OperacionDto(true, $"Contacto {id} eliminado.");
    }
}

public class AgendaService(string dbPath) {
    public ContactoDto Crear(
        string nombre,
        string apellido,
        string email,
        string telefono,
        string direccion,
        string notas) {
        nombre = Requerido(nombre, "El nombre es obligatorio.");

        using var db = Abrir();
        var contacto = new Contacto {
            Nombre = nombre,
            Apellido = Normalizar(apellido),
            Email = Normalizar(email),
            Telefono = Normalizar(telefono),
            Direccion = Normalizar(direccion),
            Notas = Normalizar(notas),
            CreadoUtc = DateTime.UtcNow,
            ActualizadoUtc = DateTime.UtcNow
        };

        db.Contactos.Add(contacto);
        db.SaveChanges();

        return ContactoDto.Desde(contacto);
    }

    public IReadOnlyList<ContactoDto> Listar(string busqueda, int limite) {
        limite = Math.Clamp(limite, 1, 100);
        busqueda = Normalizar(busqueda);

        using var db = Abrir();
        IQueryable<Contacto> consulta = db.Contactos.AsNoTracking();

        if (busqueda.Length > 0) {
            consulta = consulta.Where(contacto =>
                contacto.Nombre.Contains(busqueda) ||
                contacto.Apellido.Contains(busqueda) ||
                contacto.Email.Contains(busqueda) ||
                contacto.Telefono.Contains(busqueda));
        }

        return consulta
            .OrderBy(contacto => contacto.Apellido)
            .ThenBy(contacto => contacto.Nombre)
            .ThenBy(contacto => contacto.Id)
            .Take(limite)
            .Select(contacto => ContactoDto.Desde(contacto))
            .ToList();
    }

    public ContactoDto Obtener(int id) {
        using var db = Abrir();
        var contacto = BuscarPorId(db, id);
        return ContactoDto.Desde(contacto);
    }

    public ContactoDto Actualizar(
        int id,
        string? nombre,
        string? apellido,
        string? email,
        string? telefono,
        string? direccion,
        string? notas) {
        using var db = Abrir();
        var contacto = BuscarPorId(db, id);

        if (nombre is not null) {
            contacto.Nombre = Requerido(nombre, "El nombre no puede quedar vacio.");
        }
        if (apellido is not null) {
            contacto.Apellido = Normalizar(apellido);
        }
        if (email is not null) {
            contacto.Email = Normalizar(email);
        }
        if (telefono is not null) {
            contacto.Telefono = Normalizar(telefono);
        }
        if (direccion is not null) {
            contacto.Direccion = Normalizar(direccion);
        }
        if (notas is not null) {
            contacto.Notas = Normalizar(notas);
        }

        contacto.ActualizadoUtc = DateTime.UtcNow;
        db.SaveChanges();

        return ContactoDto.Desde(contacto);
    }

    public void Eliminar(int id) {
        using var db = Abrir();
        var contacto = BuscarPorId(db, id);
        db.Contactos.Remove(contacto);
        db.SaveChanges();
    }

    AgendaContext Abrir() {
        var db = new AgendaContext(dbPath);
        db.Database.EnsureCreated();
        return db;
    }

    static Contacto BuscarPorId(AgendaContext db, int id) {
        var contacto = db.Contactos.Find(id);
        if (contacto is null) {
            throw new ArgumentException($"No existe un contacto con id {id}.");
        }

        return contacto;
    }

    static string Requerido(string valor, string mensaje) {
        valor = Normalizar(valor);
        if (valor.Length == 0) {
            throw new ArgumentException(mensaje);
        }

        return valor;
    }

    static string Normalizar(string? valor) {
        return valor?.Trim() ?? "";
    }
}

public class AgendaContext(string dbPath) : DbContext {
    public DbSet<Contacto> Contactos => Set<Contacto>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Contacto>(entity => {
            entity.ToTable("Contactos");
            entity.HasKey(contacto => contacto.Id);
            entity.HasIndex(contacto => contacto.Email);

            entity.Property(contacto => contacto.Nombre)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(contacto => contacto.Apellido).HasMaxLength(100);
            entity.Property(contacto => contacto.Email).HasMaxLength(200);
            entity.Property(contacto => contacto.Telefono).HasMaxLength(80);
            entity.Property(contacto => contacto.Direccion).HasMaxLength(250);
            entity.Property(contacto => contacto.Notas).HasMaxLength(1000);
        });
    }
}

public class Contacto {
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Notas { get; set; } = "";
    public DateTime CreadoUtc { get; set; }
    public DateTime ActualizadoUtc { get; set; }
}

public readonly record struct ContactoDto(
    int Id,
    string Nombre,
    string Apellido,
    string Email,
    string Telefono,
    string Direccion,
    string Notas,
    DateTime CreadoUtc,
    DateTime ActualizadoUtc) {
    public static ContactoDto Desde(Contacto contacto) {
        return new ContactoDto(
            contacto.Id,
            contacto.Nombre,
            contacto.Apellido,
            contacto.Email,
            contacto.Telefono,
            contacto.Direccion,
            contacto.Notas,
            contacto.CreadoUtc,
            contacto.ActualizadoUtc);
    }
}

public readonly record struct OperacionDto(bool Ok, string Mensaje);
