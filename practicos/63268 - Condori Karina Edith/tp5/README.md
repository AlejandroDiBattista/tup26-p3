# TP5 - AgendaWeb

Aplicacion Blazor para administrar una agenda de contactos con Entity Framework
Core y SQLite.

## Funciones

- Listar los contactos de `contactos.db`.
- Buscar por nombre, apellido, telefono, correo o empresa.
- Ver todos los datos de un contacto.
- Crear contactos.
- Modificar contactos.
- Eliminar contactos con confirmacion.
- Validar los campos obligatorios y el formato del correo.

La conexion normal se define en `appsettings.json` como
`Data Source=contactos.db`. Se puede reemplazar mediante configuracion para
probar con otra base sin modificar la original.

## Como ejecutar

```powershell
cd "F:\reporsitorio karina\tup26-p3\practicos\63268 - Condori, Karina Edith\tp5"
dotnet restore
dotnet run
```

Abrir la direccion que aparece en la terminal, normalmente:

```text
http://localhost:5276
```

El archivo `tp5.csproj` incluye una configuracion para que .NET 10.0.201 pueda
procesar los recursos web aunque la ruta del alumno contenga una coma.

## Estructura

### `Models/Contacto.cs`

Representa una fila de la tabla `Contactos`. Sus atributos como `[Required]` y
`[EmailAddress]` se usan para validar el formulario.

### `Data/AgendaDbContext.cs`

Es el contexto de Entity Framework Core. Expone `DbSet<Contacto> Contactos`,
que representa la tabla de la base SQLite.

### `Services/ContactoService.cs`

Contiene la logica de aplicacion:

- `BuscarAsync`: consulta y filtra.
- `CrearAsync`: agrega un contacto.
- `ActualizarAsync`: modifica un contacto existente.
- `EliminarAsync`: elimina por id.
- `Copiar`: crea una copia para poder cancelar una edicion.

### `Components/Pages/Home.razor`

Es la pantalla principal maestro/detalle. Mantiene el contacto seleccionado,
la busqueda y el modo actual: detalle, alta o edicion.

### `Components/ContactoForm.razor`

Formulario reutilizable para crear y editar. `EditForm` ejecuta las
validaciones del modelo antes de llamar al servicio.

### `Components/DatoContacto.razor`

Componente pequeno que muestra un dato con icono, etiqueta y valor.

## Flujo para crear

1. El usuario pulsa `Nuevo contacto`.
2. `Home.razor` crea un objeto `Contacto` vacio.
3. `ContactoForm` muestra sus campos.
4. `EditForm` valida los datos.
5. `ContactoService.CrearAsync` agrega el objeto.
6. `SaveChangesAsync` lo guarda en SQLite.
7. La lista se actualiza y selecciona el contacto nuevo.

## Flujo para modificar

1. El usuario selecciona un contacto y pulsa `Editar`.
2. Se crea una copia para no cambiar la lista antes de guardar.
3. El formulario permite modificar los datos.
4. El servicio busca la fila por `Id`.
5. Copia los nuevos valores y ejecuta `SaveChangesAsync`.

## Flujo para eliminar

1. El usuario pulsa `Eliminar`.
2. La interfaz solicita confirmacion.
3. El servicio busca el contacto por `Id`.
4. `Remove` lo marca para eliminar.
5. `SaveChangesAsync` confirma la eliminacion.

## Si el profesor pide agregar un campo

1. Agregar la propiedad a `Models/Contacto.cs`.
2. Configurarla en `AgendaDbContext` si requiere una regla especial.
3. Agregar el control a `ContactoForm.razor`.
4. Mostrarla en `Home.razor`.
5. Copiarla en `ContactoService.Copiar` y `ActualizarAsync`.
6. Actualizar la base con una migracion o recrearla durante el desarrollo.

## Verificacion

```powershell
dotnet build
git status
git diff
```
