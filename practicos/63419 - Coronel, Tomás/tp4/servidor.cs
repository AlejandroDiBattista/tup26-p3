#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CatalogoDb>(opt =>
    opt.UseSqlite("Data Source=catalogo.db"));

builder.Services.AddScoped<CatalogoRepositorio>();

var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
var repo = scope.ServiceProvider.GetRequiredService<CatalogoRepositorio>();
repo.Iniciar();
}



app.MapGet("/productos", (CatalogoRepositorio repo) =>
{
return Results.Ok(repo.TraerProductos());
});

app.MapGet("/productos/{id}", (int id, CatalogoRepositorio repo) =>
{
var producto = repo.TraerProducto(id);

return producto is null
    ? Results.NotFound()
    : Results.Ok(producto);
});

app.MapPost("/productos", (ProductoDto dto, CatalogoRepositorio repo) =>
{
var producto = repo.AgregarProducto(dto);

return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id}", (int id, ProductoDto dto, CatalogoRepositorio repo) =>
{
return repo.ModificarProducto(id, dto)
    ? Results.Ok()
    : Results.NotFound();
});

app.MapDelete("/productos/{id}", (int id, CatalogoRepositorio repo) =>
{
return repo.EliminarProducto(id)
    ? Results.Ok()
    : Results.NotFound();
});

