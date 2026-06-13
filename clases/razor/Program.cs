using Microsoft.EntityFrameworkCore;
using razor.Data;
using razor.Repositories;
using razor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AgendaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Agenda")));

builder.Services.AddScoped<IContactoRepository, ContactoRepository>();
builder.Services.AddScoped<IContactoService, ContactoService>();

builder.Services.AddRazorPages();
var app = builder.Build();

await AgendaSeeder.SeedAsync(app.Services);

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
