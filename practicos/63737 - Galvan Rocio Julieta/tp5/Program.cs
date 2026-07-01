using Microsoft.EntityFrameworkCore;
using tp5.Components;
using tp5.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgendaDbContext>();
    db.Database.EnsureCreated();
}
app.Run();