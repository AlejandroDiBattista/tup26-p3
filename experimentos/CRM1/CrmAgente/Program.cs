using Microsoft.EntityFrameworkCore;
using CrmAgente.Data;
using CrmAgente.Services;
using CrmAgente.Agent;
using CrmAgente.Components;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server: componentes interactivos renderizados en el servidor.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// EF Core con SQLite. La base es un archivo crm.db en la carpeta del proyecto.
builder.Services.AddDbContext<CrmContext>(options =>
    options.UseSqlite("Data Source=crm.db"));

// Nuestra capa de servicios y el agente.
builder.Services.AddScoped<CrmService>();
builder.Services.AddScoped<AgenteFiltroService>();
builder.Services.AddScoped<CrmAgent>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Crea la base y carga datos de ejemplo al arrancar.
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<CrmContext>();
    db.Database.EnsureCreated();
    CrmContext.Seed(db);
}

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
}

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
