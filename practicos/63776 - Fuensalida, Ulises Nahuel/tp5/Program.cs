using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Components;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE SQLITE
builder.Services.AddDbContext<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));

// 2. CONFIGURACIÓN DE COMPONENTES BLAZOR MODERNOS (.NET 8+)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseStaticFiles();

// 3. ENRUTAMIENTO CORRECTO AL COMPONENTE APP
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();