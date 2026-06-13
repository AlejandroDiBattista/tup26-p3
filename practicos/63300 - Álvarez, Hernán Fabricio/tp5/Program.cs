using Microsoft.EntityFrameworkCore;
using AgendaWeb.Data;

using tp5.Components;

var builder = WebApplication.CreateBuilder(args);

/*Agregar servicios para blazor*/

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

/*Registrar el DBContext con SQLite */
builder.Services.AddDbContext<AgendaContext>(options =>
    options.UseSqlite("Data Source=agenda.db"));



var app = builder.Build();

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
