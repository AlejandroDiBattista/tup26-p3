# Clase Razor

## 1. Hola mundo basico

Se creo un proyecto Razor Pages con:

```bash
dotnet new webapp
```

El ejemplo principal esta en `Pages/Index.cshtml`.

Cambios realizados:

- Se definio una variable C# embebida en `Pages/Index.cshtml`.
- Se mostro esa variable con `@mensaje`.
- Se movio el CSS a `wwwroot/css/site.css`.
- Se simplifico `Pages/Shared/_Layout.cshtml` para sacar menu, footer, Bootstrap y scripts.
- Se eliminaron las paginas extra de la plantilla, dejando solo `Index.cshtml` y `_Layout.cshtml`.

Regla para los ejemplos de clase:

- Usar C# embebido en el `.cshtml` cuando sea posible, para que el ejemplo sea directo.
- Usar estilo K&R en C#: la llave de apertura va en la misma linea de `class`, `method`, `if`, etc.

## 2. Persistencia con EF Core y SQLite

Se agregaron clases para preparar una agenda persistida:

- `Models/Contacto.cs`: modelo con `Id`, `Nombre`, `Apellido`, `Telefono`, `Email` opcional y `Favorito`.
- `Data/AgendaDbContext.cs`: contexto de EF Core con la tabla `Contactos`.
- `Data/AgendaSeeder.cs`: crea la base SQLite y carga 10 contactos de ejemplo si la tabla esta vacia.
- `Repositories/IContactoRepository.cs`: contrato del repositorio.
- `Repositories/ContactoRepository.cs`: acceso a datos con EF Core.
- `Services/IContactoService.cs`: contrato del servicio CRUD.
- `Services/ContactoService.cs`: logica de alta, baja, modificacion y consulta.

Tambien se agrego:

- Paquete `Microsoft.EntityFrameworkCore.Sqlite` en `razor.csproj`.
- Connection string `Agenda` en `appsettings.json`.
- Registro de `AgendaDbContext`, repositorio y servicio en `Program.cs`.
- Uso de `IContactoService` desde `Pages/Index.cshtml` para mostrar los contactos persistidos.
- Vista maestro-detalle en `Pages/Index.cshtml`: lista escroleable a la izquierda y detalle simple a la derecha con etiqueta/valor.
- Se eliminaron las paginas extra: en `Pages` quedan solo `Index.cshtml` y `Shared/_Layout.cshtml`.

Para probar:

```bash
dotnet run
```
