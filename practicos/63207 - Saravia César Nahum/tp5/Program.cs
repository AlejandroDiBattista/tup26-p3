using tp5.Components;
using tp5.Data;
using tp5.Services;
using Microsoft.EntityFrameWorkCore;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ContactosContext>(FileOptions => options.UseSqlite("Data Source=contactos.db"));

builder.Services.AddScoped<ContactoService>();

var app = builder.Build();

if(!app.Enviroment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrores: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
