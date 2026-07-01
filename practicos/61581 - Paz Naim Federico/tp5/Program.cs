using Microsoft.EntityFrameworkCore;
using tp5.Components;
using tp5.Data;
using tp5.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<AgendaDbContext>(options =>
    options.UseSqlite("Data Source=contactos.db"));
builder.Services.AddScoped<ContactoService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AgendaDbContext>>();
    await AgendaDbInitializer.InitializeAsync(dbContextFactory);
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
