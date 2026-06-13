using RazorHolaMundo.Data;
using RazorHolaMundo.Repositories;
using RazorHolaMundo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AgendaDbContext>(options => options.UseSqlite("Data Source=agenda.db"));
builder.Services.AddScoped<ContactoRepository>();
builder.Services.AddScoped<AgendaService>();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope()) {
    AgendaDbContext dbContext = scope.ServiceProvider.GetRequiredService<AgendaDbContext>();
    dbContext.Database.EnsureCreated();
    AgendaDbSeeder.Seed(dbContext);
}

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.Run();
