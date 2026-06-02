#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opt => {
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<TiendaDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));
builder.Services.AddScoped<ServicioCatalogo>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var servicio = scope.ServiceProvider.GetRequiredService<ServicioCatalogo>();
    servicio.PrepararBase();
}