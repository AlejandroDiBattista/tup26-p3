using tp5.Components;
using tp5.Datos;
using tp5.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AgendaContexto>(opt =>
    opt.UseSqlite("Data Source=contactos.db"));
builder.Services.AddScoped<AgendaServicio>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AgendaContexto>>();
    using var bd = factory.CreateDbContext();
    bd.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
