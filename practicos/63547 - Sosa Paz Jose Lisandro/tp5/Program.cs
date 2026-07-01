using tp5.Components;
using tp5.Data;
using tp5.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));
builder.Services.AddScoped<ContactoService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AgendaDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
