# TP5: AgendaWeb

Agenda de contactos hecha con **Blazor (Interactive Server)**, **Entity Framework Core 10** y **SQLite**.

## Requisitos
- SDK de **.NET 10** instalado. Verificá con:
  ```bash
  dotnet --version
  ```
  Debe mostrar `10.x.x`.

## Cómo ejecutar
1. Abrí una terminal dentro de la carpeta `AgendaWeb` (donde está `AgendaWeb.csproj`).
2. Restaurá las dependencias (descarga EF Core desde NuGet):
   ```bash
   dotnet restore
   ```
3. Ejecutá la aplicación:
   ```bash
   dotnet run
   ```
   o, para recompilar automáticamente al guardar:
   ```bash
   dotnet watch run
   ```
4. La consola te va a mostrar la dirección, por ejemplo `http://localhost:5000`.
   Abrí esa URL en el navegador y entrá a **/contactos**.

## Estructura
- `Modelos/Contacto.cs` — entidad + validaciones (DataAnnotations).
- `Datos/AgendaDbContext.cs` — DbContext de EF Core.
- `Servicios/IContactoService.cs` + `ContactoService.cs` — lógica de acceso a datos.
- `Components/Pages/Contactos.razor` — vista maestro/detalle con CRUD + búsqueda.
- `Components/FormularioContacto.razor` — formulario reutilizable (alta/edición).
- `contactos.db` — base SQLite con 20 contactos de ejemplo.

## Notas
- La base se abre con la cadena `Data Source=contactos.db` (archivo en la raíz del proyecto).
- Si tu cátedra te dio un `contactos.db` oficial, reemplazá el de esta carpeta por el de ellos
  (las columnas deben llamarse: Id, Nombre, Apellido, Telefono, Email, Empresa, Cargo,
  Direccion, FechaNacimiento, Notas).
