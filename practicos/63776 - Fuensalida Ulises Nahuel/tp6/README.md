# AsistenteIA

Aplicacion de consola interactiva en C# para chatear por terminal con OpenRouter usando una API compatible con OpenAI. Usa Terminal.Gui v2 para la interfaz TUI y Microsoft.Extensions.AI con `IChatClient` para abstraer el cliente de IA.

## Paquetes usados

- `Terminal.Gui` 2.4.11
- `Microsoft.Extensions.AI` 10.3.0
- `Microsoft.Extensions.AI.OpenAI` 10.3.0

El proyecto apunta a `net10.0`. Se usa `Terminal.Gui` 2.4.11 porque esta rama requiere .NET 10 y permite trabajar con una version actual de Terminal.Gui v2.

## Requisitos

- .NET SDK 10
- Una API key de OpenRouter

## Configuracion

Copia `.env.example` a `.env` y completa la clave:

```env
OPENROUTER_API_KEY=tu-api-key-de-openrouter
OPENROUTER_BASE_URL=https://openrouter.ai/api/v1
OPENROUTER_MODEL=openai/gpt-4o-mini
AI_ENABLE_TOOLS=true
```

La clave nunca se hardcodea: la aplicacion carga `.env` si existe y luego lee variables de entorno.

OpenRouter permite elegir muchos modelos. Revisá modelos disponibles en:

https://openrouter.ai/models

Si elegis un modelo que no soporta tool calling correctamente, podes desactivar herramientas para probar solo chat:

```env
AI_ENABLE_TOOLS=false
```

## Ejecucion

```bash
dotnet restore
dotnet run
```

Para verificar configuracion sin abrir la interfaz:

```bash
dotnet run -- --check
```

Por defecto, Terminal.Gui elige el driver disponible para tu consola. Si queres probar un driver especifico:

```powershell
$env:TERMINAL_GUI_DRIVER="WindowsDriver"
dotnet run
```

Si un driver configurado falla, limpia la variable y volve a ejecutar:

```powershell
Remove-Item Env:TERMINAL_GUI_DRIVER
dotnet run
```

Controles:

- `Enter`: enviar mensaje
- `Esc`: cerrar la aplicacion
- Flechas, PageUp/PageDown o mouse: desplazarse por la conversacion

## Herramientas por function calling

El modelo recibe estas funciones mediante `AIFunctionFactory` y `ChatOptions.Tools`:

- `leer-archivo`: lee un archivo de texto.
- `escribir-archivo`: crea o sobrescribe un archivo de texto.
- `listar-archivos`: lista archivos y carpetas de un directorio.

Ejemplos de uso:

- `lee notas.txt`
- `guarda esto en salida.md`
- `que archivos hay en esta carpeta`

## Estructura

- `Program.cs`: carga `.env`, lee `AGENTS.md`, crea el `IChatClient` para OpenRouter y arranca Terminal.Gui.
- `MainWindow.cs`: ventana principal, layout, eventos de teclado, estado del input y streaming en UI.
- `ChatService.cs`: historial completo de la sesion, envio al modelo, streaming y opciones con herramientas.
- `FileTools.cs`: funciones C# expuestas al modelo con `AIFunctionFactory`.
- `MarkdownRenderer.cs`: convierte mensajes visibles a texto Markdown.
- `ChatMessageViewModel.cs`: modelo simple para renderizar turnos.
- `AGENTS.md`: prompt de sistema.
- `.env.example`: ejemplo de variables de entorno.
