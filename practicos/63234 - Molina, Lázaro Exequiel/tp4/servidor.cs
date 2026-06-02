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

app.MapGet("/productos", async (ServicioCatalogo servicio) =>
    Results.Ok(await servicio.ListarProductosAsync()));

app.MapGet("/productos/{id:int}", async (int id, ServicioCatalogo servicio) => {
    var producto = await servicio.BuscarProductoAsync(id);
    return producto is null
        ? Results.NotFound($"No se encontro el producto con id {id}.")
        : Results.Ok(producto);
});

app.MapPost("/productos", async (ProductoEntrada entrada, ServicioCatalogo servicio) => {
    var resultado = await servicio.CrearProductoAsync(entrada);
    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Created($"/productos/{resultado.Producto!.Id}", resultado.Producto);
});

app.MapPut("/productos/{id:int}", async (int id, ProductoEntrada entrada, ServicioCatalogo servicio) => {
    var resultado = await servicio.ActualizarProductoAsync(id, entrada);
    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Ok(resultado.Producto);
});

app.MapDelete("/productos/{id:int}", async (int id, ServicioCatalogo servicio) =>
    await servicio.BorrarProductoAsync(id)
        ? Results.NoContent()
        : Results.NotFound($"No se encontro el producto con id {id}."));

app.MapGet("/productos/{productoId:int}/movimientos", async (int productoId, ServicioCatalogo servicio) => {
    var movimientos = await servicio.ListarMovimientosAsync(productoId);
    return movimientos is null
        ? Results.NotFound($"No se encontro el producto con id {productoId}.")
        : Results.Ok(movimientos);
});

app.MapPost("/productos/{productoId:int}/movimientos", async (int productoId, MovimientoEntrada entrada, ServicioCatalogo servicio) => {
    var resultado = await servicio.RegistrarMovimientoAsync(productoId, entrada);
    return resultado.Error is not null
        ? Results.BadRequest(resultado.Error)
        : Results.Created($"/productos/{productoId}/movimientos/{resultado.Movimiento!.Id}", resultado);
});

app.Run("http://localhost:5050");

class ServicioCatalogo {
    private readonly TiendaDb db;

    public ServicioCatalogo(TiendaDb db) => this.db = db;
    public void PrepararBase() {
        db.Database.EnsureCreated();

        if (db.Productos.Any()) return;

        db.Productos.AddRange(
            new Producto { Codigo = "A100", Nombre = "Yerba Cachamate 500g", Precio = 1450m, Stock = 80 },
            new Producto { Codigo = "B220", Nombre = "Azucar  1kg", Precio = 890m, Stock = 45 },
            new Producto { Codigo = "C315", Nombre = "Cafe instantaneo 250g", Precio = 2600m, Stock = 30 }
        );

        db.SaveChanges();
    }

    public async Task<List<ProductoDto>> ListarProductosAsync() =>
        await db.Productos
            .OrderBy(p => p.Codigo)
            .Select(p => ProductoDto.Desde(p))
            .ToListAsync();

    public async Task<ProductoDto?> BuscarProductoAsync(int id) {
        var producto = await db.Productos.FindAsync(id);
        return producto is null ? null : ProductoDto.Desde(producto);
    }

    public async Task<ResultadoProducto> CrearProductoAsync(ProductoEntrada entrada) {
        var error = ValidarProducto(entrada);
        if (error is not null) return new(null, error);

        var codigo = entrada.Codigo.Trim().ToUpperInvariant();
        if (await db.Productos.AnyAsync(p => p.Codigo == codigo)) {
            return new(null, $"Ya existe un producto con codigo {codigo}.");
        }

        var producto = new Producto {
            Codigo = codigo,
            Nombre = entrada.Nombre.Trim(),
            Precio = entrada.Precio,
            Stock = entrada.Stock
        };

        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        return new(ProductoDto.Desde(producto), null);
    }
