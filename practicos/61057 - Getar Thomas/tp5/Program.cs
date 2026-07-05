using Microsoft.EntityFrameworkCore;
using tp5.Components;
using tp5.Data;
using tp5.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));

builder.Services.AddScoped<ContactoService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
