using Microsoft.EntityFrameworkCore; // <-- AGREGAMOS ESTO
using tp5.Components;
using tp5.Data; // <-- AGREGAMOS ESTO

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- AGREGAMOS ESTO ACÁ PARA LA BASE DE DATOS ---
builder.Services.AddDbContextFactory<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));
// -----------------------------------------------

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();