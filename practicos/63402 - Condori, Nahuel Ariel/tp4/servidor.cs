app.run("http://localhost:5050");
app.MapGet("/productos", (CatalogoRepositorio repositorio) => {
    return Result.Ok(repo.ListarProductos());
});
app.MapGet("/productos/{id:int}", (int id, CatalogoRepositorio repositorio) => {
    var producto = repositorio.ObtenerProducto(id);
    if (producto == null) {
        return Result.Error("Producto no encontrado");
    }
    return Result.Ok(producto);
}); // get =consulta
app.MapPost("/productos", (ProductoDto dto, CatalogoRepositorio repo) => {
    var producto = repo.CrearProducto(dto);
    return Result.Created($"/productos/{producto.Id}", producto);
}); // post = crear
app.MapPut("/productos/{id:int}", (int id, ProductoDto, CatalogoRepositorio repo) => {
    var producto = repo.ActualizarProducto(id, dto);
    if (producto == null) {
        return Result.Error("Producto no encontrado");
    }
    return Result.Ok(producto);
}); // put = modifica
app.MapDelete("/productos/{id:int}", (int id, CatalogoRepositorio repo) => {
    bool eliminado = repo.EliminarProducto(id);
    if (!eliminado) {
        return Result.Error("Producto no encontrado");
    }
    return Result.NoContent();
}); // delete = borra
app.MapGet("/productos/{productoId:int}/movimientos", (int productoId, catalogoRepositorio repo) => {
    var movimientos = repo.ListarMovimientosDeProducto(productoId);

    return Result.Ok(movimientos);
});
app.MapPost("/productos/{productoId:int}/movimientos", (int productoId, MovimientoDeProductoDto dto, CatalogoRepositorio repo) => {
    var movimiento = repo.RegistrarMovimientoDeProducto(productoId, dto);
    if (movimiento == null) {
        return Result.Error("Producto no encontrado");
    }
    return Result.Created($"/productos/{productoId}/movimientos/{movimiento.Id}", movimiento);
});

class Producto {
    public int Id { get; set; }
    public string Codigo { get; set; }
    public String Nombre { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}

enum TipoMovimiento {
    Compra,
    Ventea,
    Ajuste
}

class MovimientoDeProducto {
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public DateTime Fecha { get; set; }
    public int Cantidad { get; set; }
    public TipoMovimiento Tipo { get; set; }
}
