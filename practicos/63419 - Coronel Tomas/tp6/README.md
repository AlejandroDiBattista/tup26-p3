# TP6 - Asistente IA

Aplicacion de consola interactiva hecha en C# con Terminal.Gui y Microsoft.Extensions.AI.

## Ejecutar

Copiar `.env.example` como `.env` y completar la clave del proveedor que se quiera usar.

```powershell
dotnet run asistente.cs
```

Tambien se puede elegir proveedor:

```powershell
dotnet run asistente.cs -- gemini
dotnet run asistente.cs -- groq
dotnet run asistente.cs -- ollama
```

Para validar compilacion sin abrir la interfaz:

```powershell
dotnet run asistente.cs -- --check
```

## Uso

- Enter envia el mensaje.
- Esc cierra la aplicacion.
- El historial queda activo durante la sesion.
- El asistente puede listar, leer y escribir archivos del proyecto cuando se lo piden.
