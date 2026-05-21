#:sdk Microsoft.NET.Sdk.Web
#:property JsonSerializerIsReflectionEnabledByDefault=true

const string host = "http://localhost:5001";

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(host);
var app = builder.Build();

List<Producto> productos = [
    new Producto(1, "Teclado", 15000),
    new Producto(2, "Mouse", 10000)
];

app.MapGet("/productos", () => {
    return Results.Ok(productos);
});

app.MapGet("/productos/{id:int}", (int id) => {
    Producto? producto = productos.FirstOrDefault(p => p.Id == id);

    if (producto is null) { return Results.NotFound(); }

    return Results.Ok(producto);
});

app.MapPost("/productos", (CrearProductoDto dto) => {
    if (string.IsNullOrWhiteSpace(dto.Nombre)) { return Results.BadRequest("El nombre es obligatorio."); }
    if (dto.Precio <= 0)                       { return Results.BadRequest("El precio debe ser mayor que cero."); }

    int nuevoId = productos.Count == 0 ? 1 : productos.Max(p => p.Id) + 1;

    Producto producto = new Producto(nuevoId, dto.Nombre, dto.Precio);
    productos.Add(producto);

    return Results.Created($"/productos/{producto.Id}", producto);
});

app.MapPut("/productos/{id:int}", (int id, ActualizarProductoDto dto) => {
    if (string.IsNullOrWhiteSpace(dto.Nombre)) { return Results.BadRequest("El nombre es obligatorio."); }
    if (dto.Precio <= 0)                       { return Results.BadRequest("El precio debe ser mayor que cero."); }

    int indice = productos.FindIndex(p => p.Id == id);
    if (indice == -1) { return Results.NotFound(); }

    Producto productoActualizado = new Producto(id, dto.Nombre, dto.Precio);
    productos[indice] = productoActualizado;

    return Results.Ok(productoActualizado);
});

app.MapDelete("/productos/{id:int}", (int id) => {
    Producto? producto = productos.FirstOrDefault(p => p.Id == id);

    if (producto is null) { return Results.NotFound(); }

    productos.Remove(producto);

    return Results.NoContent();
});

app.Run();

public record Producto(int Id, string Nombre, decimal Precio);

public record CrearProductoDto(string Nombre, decimal Precio);

public record ActualizarProductoDto(string Nombre, decimal Precio);
