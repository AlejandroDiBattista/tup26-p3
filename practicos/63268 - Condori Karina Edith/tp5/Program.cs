using Microsoft.EntityFrameworkCore;
using tp5.Components;
using tp5.Data;
using tp5.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
string connectionString = builder.Configuration.GetConnectionString("Agenda")
    ?? "Data Source=contactos.db";
builder.Services.AddDbContextFactory<AgendaDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<ContactoService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
