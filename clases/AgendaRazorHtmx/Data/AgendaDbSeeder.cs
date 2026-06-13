using AgendaRazorHtmx.Models;

namespace AgendaRazorHtmx.Data;

public static class AgendaDbSeeder {
    public static void Seed(AgendaDbContext dbContext) {
        if (dbContext.Contacts.Any()) { return; }

        (string Nombre, string Apellido, string Telefono, string Email, string Notas)[] sampleContacts = {
            ("Ana",       "Gómez",     "3815551001", "ana@mail.com",       "Cliente frecuente"),
            ("Luis",      "Pérez",     "3815551002", "luis@mail.com",      "Prefiere WhatsApp"),
            ("María",     "López",     "3815551003", "maria@mail.com",     "Contactar por la tarde"),
            ("Jorge",     "Martínez",  "3815551004", "jorge@mail.com",     "Contactar por la mañana"),
            ("Sofía",     "García",    "3815551005", "sofia@mail.com",     "Contactar por la noche"),
            ("Carlos",    "Rodríguez", "3815551006", "carlos@mail.com",    "Contactar por la noche"),
            ("Lucía",     "Fernández", "3815551007", "lucia@mail.com",     "Contactar por la mañana"),
            ("Diego",     "Sánchez",   "3815551008", "diego@mail.com",     "Contactar por la noche"),
            ("Valentina", "Ruiz",      "3815551009", "valentina@mail.com", "Enviar presupuesto este viernes"),
            ("Mateo",     "Castro",    "3815551010", "mateo@mail.com",     "Llamar luego de las 18 hs"),
            ("Camila",    "Herrera",   "3815551011", "camila@mail.com",    "Consulta por plan anual"),
            ("Franco",    "Morales",   "3815551012", "franco@mail.com",    "Pide recordatorio por email"),
            ("Julieta",   "Navarro",   "3815551013", "julieta@mail.com",   "Revisar datos fiscales"),
            ("Tomás",     "Silva",     "3815551014", "tomas@mail.com",     "Prefiere atención presencial"),
            ("Agustina",  "Vega",      "3815551015", "agustina@mail.com",  "Seguimiento la próxima semana"),
            ("Bruno",     "Ortega",    "3815551016", "bruno@mail.com",     "Interesado en servicio premium"),
            ("Renata",    "Molina",    "3815551017", "renata@mail.com",    "Derivada por un cliente actual"),
            ("Simón",     "Medina",    "3815551018", "simon@mail.com",     "Confirmar reunión del martes")
        };

        dbContext.Contacts.AddRange(sampleContacts.Select(c => new Contact {
            Nombre = c.Nombre, Apellido = c.Apellido, Telefono = c.Telefono, Email = c.Email, Notas = c.Notas
        }));
        dbContext.SaveChanges();
    }
}
