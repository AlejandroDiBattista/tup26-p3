# Trabajo Práctico 4 — `CatalogoREST`
## Aplicación TUI con API REST, persistencia SQLite y Entity Framework Core

---
> [!IMPORTANT]
> Plazo para entregar el TP4: **martes 2 de junio*
>*El trabajo es estrictamente individual y debe ser realizado en persona por el alumno*

## Descripción general

Desarrollar un sistema de administración de catálogo de productos compuesto por **dos aplicaciones independientes** que se ejecutan en paralelo:

- **`catalogo.cs`** — Interfaz de usuario de terminal (TUI) implementada con Terminal.Gui v2.
- **`servidor.cs`** — API REST implementada con ASP.NET Core Minimal API y Entity Framework Core + SQLite.

Para ejecutar debe iniciar el servidor en segundo plano y luego la aplicación TUI en primer plano.
Para lograrlo debe abrir dos terminales y ejecutar cada aplicación en uno de ellos.

---

## Modelo de datos

### `Producto`
| Campo        | Tipo               | Descripción                                 |
|--------------|--------------------|---------------------------------------------|
| `Id`         | `int`              | Identificador generado por la BD            |
| `Codigo`     | `string`           | Código alfanumérico único                   |
| `Nombre`     | `string`           | Nombre descriptivo del producto             |
| `Precio`     | `decimal`          | Precio unitario                             |
| `Stock`      | `int`              | Stock disponible actual                     |

### `MovimientoDeProducto`
| Campo        | Tipo               | Descripción                                 |
|--------------|--------------------|---------------------------------------------|
| `Id`         | `int`              | Identificador generado por la BD            |
| `ProductoId` | `int`              | Referencia al producto                      |
| `Tipo`       | `TipoMovimiento`   | `Compra`, `Venta` o `Ajuste`                |
| `Cantidad`   | `int`              | Unidades involucradas (positivo o negativo) |
| `Fecha`      | `DateTime`         | Fecha y hora del movimiento                 |

---

## Funcionalidades requeridas

### Servidor REST (`servidor.cs`)
Exponer los siguientes endpoints:

**Productos**
- `GET    /productos`         — listar todos los productos
- `GET    /productos/{id}`    — obtener un producto por id
- `POST   /productos`         — crear producto
- `PUT    /productos/{id}`    — modificar producto
- `DELETE /productos/{id}`    — eliminar producto

**Movimientos de stock**
- `GET  /productos/{productoId}/movimientos` — listar historial de un producto
- `POST /productos/{productoId}/movimientos` — registrar un movimiento de stock (compra, venta o ajuste)
    El movimiento debe indicar el tipo, la cantidad siempre es positiva; en una compra aumenta el stock, en una venta lo disminuye y en un ajuste establece el stock a un valor específico.

> Al registrar un movimiento de stock, el servidor debe **actualizar el stock del producto** en la misma operación.

### Aplicación TUI (`catalogo.cs`)

La interfaz debe implementar un layout **maestro/detalle**:

- **Panel izquierdo (maestro):** lista de productos con código, nombre, precio y stock actual.
- **Panel derecho (detalle):** historial de movimientos de stock del producto seleccionado (tipo, cantidad, fecha).
- Debe buscar los productos por código o nombre.
- Editar agregar o editar productos mediante una caja de diálogo.
- Registrar movimientos de stock mediante una caja de diálogo.
- Debera poder elegir las funciones desde un menú o mediante atajos de teclado.

Acciones disponibles desde la interfaz:

- **Productos:** agregar, modificar y eliminar.
- **Movimientos de stock:** registrar una compra, venta o ajuste para el producto seleccionado.

--- 

### Apuntes de clase relacionados:
- [ASP.NET Core Minimal API](../../apuntes/02.350-Tutorial-minimal-api.md)
- [Entity Framework Core](../../apuntes/02.360-Tutorial-entity-framework-core.md)
- [Terminal.Gui](../../apuntes/02.300-Terminal-gui.md)


### Documentación oficial de las tecnologías utilizadas:
- [Documentación de ASP.NET Core Minimal API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Documentación de Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Documentación de Terminal.Gui](https://gui-cs.github.io/Terminal.Gui/)
