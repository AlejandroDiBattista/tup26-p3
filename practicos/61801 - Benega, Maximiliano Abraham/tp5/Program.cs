using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using tp5.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<tp5.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public class AgendaDbContext : DbContext
{
    public AgendaDbContext(DbContextOptions<AgendaDbContext> options) : base(options) { }
    
    public DbSet<Contacto> Contactos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Contacto>();
        entity.ToTable("Contactos");
        entity.Property(c => c.CorreoElectronico).HasColumnName("Email");

    }
}