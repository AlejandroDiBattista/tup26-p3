using Microsoft.EntityFrameworkCore;
using AgendaWeb.Data;
using AgendaWeb.Services;
using tp5.Components;

var builder = WebApplication.CreateBuilder(args);

/*Agregar servicios para blazor*/

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

/*Registrar el DBContext con SQLite */
builder.Services.AddDbContext<AgendaContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));

builder.Services.AddScoped<ContactoService>();

var app = builder.Build();

// Crear la base de datos si no existe
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AgendaContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
