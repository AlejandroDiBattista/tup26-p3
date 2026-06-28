#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@*
#:property PublishAot=false

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var armador = WebApplication.CreateBuilder(args);
armador.Services.AddDbContext<ContextoCatalogo>(cfg => cfg.UseSqlite("Data Source=catalogo.db"));

var api = armador.Build();

// crear tablas si no existen todavia
using (var ambito = api.Services.CreateScope())
    ambito.ServiceProvider.GetRequiredService<ContextoCatalogo>().Database.EnsureCreated();

api.MapPost("/productos", async (Articulo datosNuevos, ContextoCatalogo db) =>
{
    // no permitir duplicados
    if (await db.Articulos.AnyAsync(a => a.Clave == datosNuevos.Clave))
        return Results.BadRequest("Ya existe un producto con ese código.");

    db.Articulos.Add(datosNuevos);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{datosNuevos.Id}", datosNuevos);
});

api.MapGet("/productos", async (ContextoCatalogo db) =>
    await db.Articulos.ToListAsync());

api.MapPut("/productos/{id}", async (int id, Articulo cambios, ContextoCatalogo db) =>
{
    var art = await db.Articulos.FindAsync(id);
    if (art is null) return Results.NotFound();

    if (await db.Articulos.AnyAsync(a => a.Clave == cambios.Clave && a.Id != id))
        return Results.BadRequest("Ya existe otro producto con ese código.");

    art.Clave = cambios.Clave;
    art.Descripcion = cambios.Descripcion;
    art.Importe = cambios.Importe;
    art.Existencia = cambios.Existencia;

    await db.SaveChangesAsync();
    return Results.Ok(art);
});

api.MapGet("/productos/{id}", async (int id, ContextoCatalogo db) =>
{
    var art = await db.Articulos.FindAsync(id);
    return art is null ? Results.NotFound() : Results.Ok(art);
});

api.MapDelete("/productos/{id}", async (int id, ContextoCatalogo db) =>
{
    var art = await db.Articulos.FindAsync(id);
    if (art is null) return Results.NotFound();

    db.Articulos.Remove(art);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapPost("/productos/{productoId}/movimientos", async (int productoId, Operacion op, ContextoCatalogo db) =>
{
    var art = await db.Articulos.FindAsync(productoId);
    if (art is null) return Results.NotFound();

    if (op.Unidades <= 0)
        return Results.BadRequest("La cantidad debe ser mayor a cero.");

    switch (op.Clase)
    {
        case ClaseOperacion.Entrada:
            art.Existencia += op.Unidades;
            break;
        case ClaseOperacion.Salida:
            if (art.Existencia < op.Unidades)
                return Results.BadRequest("Stock insuficiente para realizar la venta.");
            art.Existencia -= op.Unidades;
            break;
        case ClaseOperacion.Regularizacion:
            art.Existencia = op.Unidades;
            break;
    }

    op.ArticuloId = productoId;
    op.Momento = DateTime.Now;

    db.Operaciones.Add(op);
    await db.SaveChangesAsync();
    return Results.Created($"/productos/{productoId}/movimientos/{op.Id}", op);
});

api.MapGet("/productos/{productoId}/movimientos", async (int productoId, ContextoCatalogo db) =>
{
    if (!await db.Articulos.AnyAsync(a => a.Id == productoId))
        return Results.NotFound();

    var lista = await db.Operaciones
        .Where(o => o.ArticuloId == productoId)
        .OrderByDescending(o => o.Momento)
        .ToListAsync();

    return Results.Ok(lista);
});

api.Run("http://localhost:5050");

// modelos

class ContextoCatalogo : DbContext
{
    public ContextoCatalogo(DbContextOptions<ContextoCatalogo> config) : base(config) { }

    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<Operacion> Operaciones => Set<Operacion>();
}

class Operacion
{
    public int Id { get; set; }
    public int ArticuloId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaseOperacion Clase { get; set; }

    public int Unidades { get; set; }
    public DateTime Momento { get; set; } = DateTime.Now;

    public Articulo? Articulo { get; set; }
}

class Articulo
{
    public int Id { get; set; }
    public string Clave { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Importe { get; set; }
    public int Existencia { get; set; }
}

enum ClaseOperacion { Entrada, Salida, Regularizacion }
