TP5: AgendaWeb

Agenda de contactos hecha con Blazor (Interactive Server), Entity Framework Core 10 y SQLite.


ejecutar con:
Abrí una terminal dentro de la carpeta AgendaWeb 
Restaurá las dependencias (descarga EF Core desde NuGet):
   
   dotnet restore

Ejecutá la aplicación:
  
   dotnet run
 
   o, para recompilar automáticamente al guardar:
  
   dotnet watch run

4. La consola te va a mostrar la dirección, por ejemplo http://localhost:5000.
   

Estructura
Modelos/Persona.cs — entidad + validaciones (DataAnnotations).
Datos/LibretaDbContext.cs — DbContext de EF Core (mapea la entidad a la tabla Contactos).
Servicios/IPersonaServicio.cs + PersonaServicio.cs — lógica de acceso a datos.
Components/Pages/Contactos.razor — vista maestro/detalle con CRUD + búsqueda.
Components/FormularioPersona.razor — formulario reutilizable (alta/edición) con paleta verde azulada.
contactos.db — base SQLite con 20 contactos de ejemplo.


