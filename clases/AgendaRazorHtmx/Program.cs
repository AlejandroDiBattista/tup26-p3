using AgendaRazorHtmx.Data;
using AgendaRazorHtmx.Repositories;
using AgendaRazorHtmx.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
string connectionString = "Data Source=./agenda.db";

builder.Services.AddRazorPages();
builder.Services.AddDbContextFactory<AgendaDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IContactService, ContactService>();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope()) {
    IDbContextFactory<AgendaDbContext> dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AgendaDbContext>>();

    using AgendaDbContext dbContext = dbContextFactory.CreateDbContext();
    dbContext.Database.EnsureCreated();
    AgendaDbSeeder.Seed(dbContext);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.Run();
