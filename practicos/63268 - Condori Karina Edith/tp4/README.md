# TP4 - CatalogoREST

Sistema para administrar productos y movimientos de stock.

- `servidor.cs`: API REST, Entity Framework Core y SQLite.
- `catalogo.cs`: interfaz de terminal con Terminal.Gui v2.

## Ejecucion

Abrir dos terminales en esta carpeta.

Terminal 1:

```powershell
dotnet run servidor.cs
```

Terminal 2:

```powershell
dotnet run catalogo.cs
```

El servidor escucha en `http://localhost:5050` y crea `catalogo.db`.

## Operaciones de productos

| Operacion | API | Interfaz |
|---|---|---|
| Agregar | `POST /productos` | `F2` |
| Modificar | `PUT /productos/{id}` | `F3` o `Enter` |
| Eliminar | `DELETE /productos/{id}` | `Supr` |
| Listar | `GET /productos` | `F6` actualiza |

La interfaz tambien permite buscar por codigo o nombre y acceder a las
operaciones desde el menu superior.

## Como funciona el alta paso por paso

1. `AgregarProducto()` abre `ProductoDialog`.
2. El dialogo pide codigo, nombre, precio y stock.
3. `ProductoDialog.Validar()` controla los datos antes de enviarlos.
4. `ApiClient.CrearProductoAsync()` envia un `POST /productos`.
5. El servidor ejecuta el bloque `MapPost`.
6. `ValidarProducto()` vuelve a validar los datos en el servidor.
7. Se comprueba que el codigo no este repetido.
8. Entity Framework agrega el objeto con `db.Productos.Add(producto)`.
9. `SaveChangesAsync()` guarda el registro en SQLite.
10. La TUI recarga la lista y selecciona el nuevo producto.

## Como funciona la modificacion paso por paso

1. `EditarProducto()` obtiene el producto seleccionado.
2. Abre `ProductoDialog` con los campos ya completos.
3. `ApiClient.EditarProductoAsync()` envia un `PUT /productos/{id}`.
4. El servidor busca el registro con `db.Productos.FindAsync(id)`.
5. Controla que el nuevo codigo no pertenezca a otro producto.
6. Asigna codigo, nombre, precio y stock al objeto encontrado.
7. `SaveChangesAsync()` actualiza la fila en SQLite.
8. La TUI recarga la lista conservando la seleccion.

## Como funciona la eliminacion paso por paso

1. `EliminarProducto()` obtiene el producto seleccionado.
2. `MessageBox.Query()` solicita confirmacion.
3. `ApiClient.EliminarProductoAsync()` envia `DELETE /productos/{id}`.
4. El servidor busca el producto por id.
5. `db.Productos.Remove(producto)` marca el registro para eliminar.
6. `SaveChangesAsync()` confirma la baja.
7. Los movimientos asociados se eliminan en cascada.
8. La TUI vuelve a cargar los productos.

## Movimientos de stock

- Compra: suma la cantidad al stock.
- Venta: resta la cantidad y rechaza stock negativo.
- Ajuste: establece el stock en la cantidad indicada.

El movimiento y el stock se guardan dentro de una transaccion.

## Si el profesor pide agregar un campo

Ejemplo: agregar `Descripcion`.

1. Agregar la propiedad en la clase `Producto` de `servidor.cs`.
2. Agregar el dato al record `ProductoDatos`.
3. Asignarlo en los bloques `MapPost` y `MapPut`.
4. Agregarlo a `ProductoDto` y `ProductoDatos` en `catalogo.cs`.
5. Crear un `TextField` en `ProductoDialog`.
6. Incluirlo al construir `Datos`.
7. Borrar `catalogo.db` durante el desarrollo para regenerar la estructura,
   o crear una migracion si se deben conservar los datos.
8. Compilar ambos archivos y repetir las solicitudes de `pruebas.http`.

## Si el profesor pide cambiar una validacion

La validacion debe actualizarse en dos lugares:

1. `ProductoDialog.Validar()` para avisar inmediatamente en la interfaz.
2. `ValidarProducto()` en el servidor para proteger la API aunque se invoque
   sin usar la TUI.

## Pruebas

`pruebas.http` permite probar cada endpoint con la extension REST Client de
Visual Studio Code. Ejecutar las solicitudes en orden; el archivo reutiliza
automaticamente el id devuelto al crear el producto.

Antes de entregar:

```powershell
dotnet build servidor.cs
dotnet build catalogo.cs
git status
git diff
```
