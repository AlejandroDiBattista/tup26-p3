#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(opt => {
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<CatalogoDb>(opt => opt.UseSqlite("Data Source=catalogo.db"));

var app = builder.Build();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<CatalogoDb>();
    db.Database.EnsureCreated();

    if (!db.Productos.Any()) {
        db.Productos.AddRange(
            new Producto { Codigo = "P001", Nombre = "Yerba Mate 500g", Precio = 1500m, Stock = 100 },
            new Producto { Codigo = "P002", Nombre = "Azucar 1kg", Precio = 900m, Stock = 60 },
            new Producto { Codigo = "P003", Nombre = "Cafe molido 250g", Precio = 2500m, Stock = 35 }
        );
        db.SaveChanges();
    }
}