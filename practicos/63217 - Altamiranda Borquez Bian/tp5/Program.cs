using Microsoft.EntipyFrameworkCore;
using tp5.Components;
using tp5.Data;
using tp5.Services;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(builder.Environment.ContentRootPath, "contactos.db")}")
);
builder.Services.AddScoped<ServicioContactos>(); 
var app = builder.Build();
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();
