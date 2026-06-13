using AgendaRazorHtmx.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaRazorHtmx.Data;

public class AgendaDbContext(DbContextOptions<AgendaDbContext> options) : DbContext(options) {
    public DbSet<Contact> Contacts => Set<Contact>();
}
