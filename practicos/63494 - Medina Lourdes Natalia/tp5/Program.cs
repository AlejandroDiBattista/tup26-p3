using tp5.Components;
using tp5.Data;
using tp5.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string databasePath = Path.Combine(builder.Environment.ContentRootPath, "contactos.db");
builder.Services.AddDbContextFactory<AgendaDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<ContactoService>();