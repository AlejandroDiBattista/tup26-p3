# Agenda Razor HTMX

Aplicación maestro-detalle hecha con:

- ASP.NET Core Razor Pages
- HTMX
- Partial Views
- Almacenamiento en memoria
- Sin JavaScript propio
- Sin Minimal API

## Ejecutar

```bash
dotnet run
```

Abrir:

```txt
https://localhost:xxxx/Agenda
```

O también:

```txt
http://localhost:xxxx/Agenda
```

## Flujo

- `/Agenda` devuelve la página completa.
- `/Agenda?handler=List` devuelve el fragmento de lista.
- `/Agenda?handler=Detail&id=1` devuelve el formulario del contacto.
- `/Agenda?handler=Save` guarda alta/edición.
- `/Agenda?handler=Delete&id=1` elimina un contacto.

Los datos están en memoria, por eso se reinician al reiniciar la aplicación.
