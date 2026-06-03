using AgendaRazorHtmx;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args); // Crea el host de la aplicación y carga configuración, logging e inyección de dependencias.
string connectionString = "Data Source=./agenda.db"; // Define la ruta de SQLite relativa al directorio de ejecución.

builder.Services.AddRazorPages(); // Habilita Razor Pages para renderizar las vistas y los handlers.
builder.Services.AddDbContextFactory<AgendaDbContext>(options => options.UseSqlite(connectionString)); // Registra una fábrica para crear DbContext nuevos cuando haga falta.
builder.Services.AddScoped<ContactStore>(); // Registra el acceso a datos con un ciclo de vida por request.

var app = builder.Build(); // Construye la aplicación usando los servicios ya registrados.

using (IServiceScope scope = app.Services.CreateScope()) {
    IDbContextFactory<AgendaDbContext> dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AgendaDbContext>>(); // Obtiene la fábrica de contextos desde el contenedor.

    using AgendaDbContext dbContext = dbContextFactory.CreateDbContext(); // Crea un contexto efímero para inicializar la base.
    dbContext.Database.EnsureCreated(); // Crea la base y la tabla si todavía no existen.
    AgendaDbSeeder.Seed(dbContext); // Carga datos de ejemplo solo cuando la tabla está vacía.
}

app.UseHttpsRedirection(); // Redirige automáticamente HTTP a HTTPS.
app.UseStaticFiles(); // Sirve archivos estáticos como CSS, imágenes y scripts.
app.UseRouting(); // Activa el enrutamiento para resolver Razor Pages.

app.MapRazorPages(); // Expone las páginas Razor como endpoints HTTP.

app.Run(); // Arranca el servidor y deja la aplicación escuchando peticiones.

namespace AgendaRazorHtmx {
    public class Contact {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Email { get; set; } = "";
        public string Notas { get; set; } = "";
    }

    public record ContactInput(
        int Id,
        string Nombre,
        string Apellido,
        string Telefono,
        string Email,
        string Notas
    );

    public record ContactListViewModel(List<Contact> Contacts, int? SelectedId);

    public class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options) {
        public DbSet<Contact> Contacts => Set<Contact>();
    }

    public static class AgendaDbSeeder {
        public static void Seed(AgendaDbContext dbContext) {
            Contact[] sampleContacts = {
                new Contact { Nombre = "Ana",       Apellido = "Gómez",     Telefono = "3815551001", Email = "ana@mail.com",       Notas = "Cliente frecuente" },
                new Contact { Nombre = "Luis",      Apellido = "Pérez",     Telefono = "3815551002", Email = "luis@mail.com",      Notas = "Prefiere WhatsApp" },
                new Contact { Nombre = "María",     Apellido = "López",     Telefono = "3815551003", Email = "maria@mail.com",     Notas = "Contactar por la tarde" },
                new Contact { Nombre = "Jorge",     Apellido = "Martínez",  Telefono = "3815551004", Email = "jorge@mail.com",     Notas = "Contactar por la mañana" },
                new Contact { Nombre = "Sofía",     Apellido = "García",    Telefono = "3815551005", Email = "sofia@mail.com",     Notas = "Contactar por la noche" },
                new Contact { Nombre = "Carlos",    Apellido = "Rodríguez", Telefono = "3815551006", Email = "carlos@mail.com",    Notas = "Contactar por la noche" },
                new Contact { Nombre = "Lucía",     Apellido = "Fernández", Telefono = "3815551007", Email = "lucia@mail.com",     Notas = "Contactar por la mañana" },
                new Contact { Nombre = "Diego",     Apellido = "Sánchez",   Telefono = "3815551008", Email = "diego@mail.com",     Notas = "Contactar por la noche" },
                new Contact { Nombre = "Valentina", Apellido = "Ruiz",      Telefono = "3815551009", Email = "valentina@mail.com", Notas = "Enviar presupuesto este viernes" },
                new Contact { Nombre = "Mateo",     Apellido = "Castro",    Telefono = "3815551010", Email = "mateo@mail.com",     Notas = "Llamar luego de las 18 hs" },
                new Contact { Nombre = "Camila",    Apellido = "Herrera",   Telefono = "3815551011", Email = "camila@mail.com",    Notas = "Consulta por plan anual" },
                new Contact { Nombre = "Franco",    Apellido = "Morales",   Telefono = "3815551012", Email = "franco@mail.com",    Notas = "Pide recordatorio por email" },
                new Contact { Nombre = "Julieta",   Apellido = "Navarro",   Telefono = "3815551013", Email = "julieta@mail.com",   Notas = "Revisar datos fiscales" },
                new Contact { Nombre = "Tomás",     Apellido = "Silva",     Telefono = "3815551014", Email = "tomas@mail.com",     Notas = "Prefiere atención presencial" },
                new Contact { Nombre = "Agustina",  Apellido = "Vega",      Telefono = "3815551015", Email = "agustina@mail.com",  Notas = "Seguimiento la próxima semana" },
                new Contact { Nombre = "Bruno",     Apellido = "Ortega",    Telefono = "3815551016", Email = "bruno@mail.com",     Notas = "Interesado en servicio premium" },
                new Contact { Nombre = "Renata",    Apellido = "Molina",    Telefono = "3815551017", Email = "renata@mail.com",    Notas = "Derivada por un cliente actual" },
                new Contact { Nombre = "Simón",     Apellido = "Medina",    Telefono = "3815551018", Email = "simon@mail.com",     Notas = "Confirmar reunión del martes" }
            };

            if (dbContext.Contacts.Any()) { return; }

            dbContext.Contacts.AddRange(sampleContacts);
            dbContext.SaveChanges();
        }
    }

    public class ContactStore(IDbContextFactory<AgendaDbContext> dbContextFactory) {
        public List<Contact> GetAll(string? search = null) {
            using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();
            IQueryable<Contact> query = dbContext.Contacts
                .OrderBy(contact => contact.Apellido)
                .ThenBy(contact => contact.Nombre);

            if (!string.IsNullOrWhiteSpace(search)) {
                string term = search.Trim();

                query = query.Where(contact =>
                    EF.Functions.Like(contact.Nombre, $"%{term}%")
                    || EF.Functions.Like(contact.Apellido, $"%{term}%")
                    || EF.Functions.Like(contact.Telefono, $"%{term}%")
                    || EF.Functions.Like(contact.Email, $"%{term}%")
                    || EF.Functions.Like(contact.Notas, $"%{term}%")
                );
            }

            return query.ToList();
        }

        public Contact? GetById(int id) {
            using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();

            return dbContext.Contacts.SingleOrDefault(contact => contact.Id == id);
        }

        public Contact Save(ContactInput input) {
            using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();
            Contact contact;

            if (input.Id == 0) {
                contact = new Contact();
                dbContext.Contacts.Add(contact);
            } else {
                contact = dbContext.Contacts.Single(contact => contact.Id == input.Id);
            }

            contact.Nombre = input.Nombre;
            contact.Apellido = input.Apellido;
            contact.Telefono = input.Telefono;
            contact.Email = input.Email;
            contact.Notas = input.Notas;

            dbContext.SaveChanges();

            return contact;
        }

        public void Delete(int id) {
            using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();
            Contact? contact = dbContext.Contacts.SingleOrDefault(existingContact => existingContact.Id == id);

            if (contact is null) { return; }

            dbContext.Contacts.Remove(contact);
            dbContext.SaveChanges();
        }
    }
}
