using tp5.Components;
using Microsoft.EntityFrameworkCore;
using tp5.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options) { }
    public DbSet<Contacto> Contactos { get; set; } = null!;
}