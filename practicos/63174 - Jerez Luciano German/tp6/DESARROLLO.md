# Desarrollo TP6 - AsistenteIA

## Objetivo y alcance

El objetivo fue completar la aplicación file-based de C# entregada como punto de partida para convertirla en un asistente conversacional de terminal. La solución implementa una interfaz TUI con Terminal.Gui v2, conversación con `IChatClient` de Microsoft.Extensions.AI, streaming de respuestas, historial de sesión, renderizado Markdown y herramientas de sistema de archivos expuestas al modelo mediante function calling.

El alcance se mantiene en la carpeta del TP6. No se modifica el prompt de sistema ni los archivos de configuración provistos; la aplicación lee `AGENTS.md` y `.env` en tiempo de ejecución.

## Requisitos interpretados

- Cargar el mensaje de sistema desde `AGENTS.md`.
- Leer proveedor, URL, clave y modelo desde variables de entorno cargadas por `.env`.
- Usar `IChatClient` para desacoplar la aplicación del proveedor concreto.
- Mostrar una ventana de pantalla completa con panel de conversación y panel de entrada.
- Enviar el mensaje del usuario con Enter o con el botón `Enviar`.
- Mostrar la respuesta del asistente en streaming, fragmento por fragmento.
- Mantener el historial completo de la sesión para cada nueva consulta.
- Renderizar la conversación como Markdown.
- Deshabilitar entrada y botón mientras el asistente responde.
- Cerrar limpiamente con Esc.
- Exponer las herramientas `leer-archivo`, `escribir-archivo` y `listar-archivos`.
- Limitar las operaciones de archivos al directorio del TP para evitar accesos accidentales fuera del proyecto.

## Arquitectura y decisiones de diseño

La solución se mantiene en `asistente.cs` porque el enunciado entrega una aplicación C# file-based. Para sostener responsabilidades claras dentro de un único archivo se separaron las partes por funciones locales y tipos pequeños:

- Configuración inicial: carga `.env`, valida `AGENTS.md`, normaliza el endpoint y crea el cliente.
- Cliente de IA: `OpenAIClient` se adapta a `IChatClient` y luego se envuelve con `ChatClientBuilder.UseFunctionInvocation()` para habilitar llamadas automáticas a herramientas.
- Estado conversacional: `mensajes` conserva el historial enviado al modelo, incluyendo el mensaje de sistema; `conversacion` conserva solo los turnos visibles.
- UI: `Window`, `FrameView`, `Markdown`, `TextField` y `Button` definen los dos paneles pedidos.
- Streaming: `StreamAssistantResponseAsync` acumula fragmentos y actualiza el Markdown mediante `app.Invoke`.
- Herramientas: `FileSystemTools` contiene las tres funciones y resuelve rutas contra la carpeta del TP.

Se descartó crear un proyecto `.csproj` tradicional porque el punto de partida y el enunciado piden explícitamente una app file-based con paquetes declarados en el archivo.

## Estructura de archivos

- `asistente.cs`: aplicación principal, configuración, interfaz, streaming, historial y herramientas.
- `AGENTS.md`: prompt de sistema leído al iniciar.
- `.env`: configuración local del proveedor y modelo.
- `.env.ejemplo`: plantilla de configuración.
- `image.png`: referencia visual del enunciado.
- `enunciado.md`: consigna del trabajo.
- `DESARROLLO.md`: documentación de implementación y pruebas.

## Implementación paso a paso

Primero se reemplazó la consulta fija inicial por una configuración reutilizable. La aplicación valida que exista `AGENTS.md`, toma el proveedor desde el primer argumento o usa `openai`, lee variables como `OPENAI_API_URL`, `OPENAI_API_KEY` y `OPENAI_MODEL`, y crea el `IChatClient`.

Luego se agregaron las herramientas con `AIFunctionFactory.Create`. Los nombres publicados son exactamente `leer-archivo`, `escribir-archivo` y `listar-archivos`, y se entregan al modelo en `ChatOptions.Tools`. El cliente se envuelve con `UseFunctionInvocation()` para que Microsoft.Extensions.AI ejecute las herramientas cuando el modelo las solicite.

Después se construyó la interfaz. El panel superior renderiza Markdown y ocupa casi toda la pantalla. El panel inferior contiene un `TextField` y un botón `Enviar`. El botón es el destino por defecto de aceptación, por lo que Enter desde la entrada dispara el envío. Esc invoca `app.RequestStop()` y cierra la aplicación.

El envío agrega el mensaje del usuario al historial visible y al historial del modelo. A continuación agrega un turno visible del asistente en estado de escritura y llama a `GetStreamingResponseAsync`. Cada fragmento recibido actualiza el turno visible del asistente. Al terminar, el texto completo se agrega al historial de `ChatMessage` como respuesta del asistente.

El auto-scroll se aplica solo si el usuario está cerca del final de la conversación. Si el usuario se desplazó hacia arriba para leer mensajes anteriores, los fragmentos nuevos no fuerzan el desplazamiento.

## Modelo de datos y estado

`VisibleMessage` representa un turno renderizable con autor y contenido Markdown. Se usa para evitar mostrar el mensaje de sistema en pantalla.

`List<ChatMessage> mensajes` es el historial que se envía al modelo. Empieza con el mensaje de sistema y luego acumula turnos de usuario y asistente. Este historial permite que cada consulta nueva tenga contexto de la sesión.

`respuestaEnCurso` bloquea envíos superpuestos. Mientras su valor indica una respuesta activa, la entrada y el botón quedan deshabilitados.

## Herramientas de archivos

`FileSystemTools` recibe como raíz el directorio actual desde el que se ejecuta la aplicación. La aplicación valida que ese directorio contenga `AGENTS.md`, por lo que el uso esperado es ejecutar desde la carpeta del TP6.

Las rutas se resuelven con `Path.GetFullPath` y `Path.GetRelativePath`. Se rechazan rutas absolutas y rutas que escapen con `..`. Esta decisión permite cumplir el pedido de operar sobre archivos del proyecto sin permitir accesos fuera del práctico.

Comportamiento:

- `leer-archivo`: devuelve el contenido de un archivo de texto o informa que no existe.
- `escribir-archivo`: crea carpetas intermedias si hacen falta y escribe el contenido indicado.
- `listar-archivos`: lista archivos y carpetas, marcando carpetas con `/`.

## Configuración del endpoint

El `.env.ejemplo` usa URLs compatibles con Chat Completions, por ejemplo rutas terminadas en `/chat/completions`. El SDK `OpenAIClientOptions.Endpoint` espera el endpoint base del servicio. Para aceptar ambos formatos, `NormalizeOpenAIEndpoint` recorta solo el sufijo `/chat/completions` si está presente y conserva el host y cualquier base path anterior, como `/v1`.

## Validaciones y manejo de errores

- Si falta `AGENTS.md`, la aplicación falla con un mensaje que indica ejecutar desde la carpeta del TP6.
- Si falta la variable `{PROVEEDOR}_API_URL`, se lanza una excepción explícita.
- Si el modelo o proveedor falla durante streaming, se muestra el error en el turno del asistente y se reactivan los controles.
- Si una herramienta recibe una ruta vacía o fuera del TP, se rechaza la operación.
- Durante una respuesta activa no se aceptan mensajes nuevos.

## Instrucciones para compilar y ejecutar

Desde la carpeta del TP:

```bash
dotnet build asistente.cs
dotnet run asistente.cs
```

Para usar otro proveedor configurado en `.env`, pasar el nombre como primer argumento:

```bash
dotnet run asistente.cs -- groq
dotnet run asistente.cs -- ollama
```

La aplicación debe ejecutarse desde esta carpeta para que encuentre `AGENTS.md` y limite correctamente las herramientas al directorio del TP.

## Pruebas realizadas

Compilación:

```bash
dotnet build asistente.cs
```

Resultado: compilación exitosa sin errores.

Arranque y cierre de TUI:

```bash
dotnet run asistente.cs
```

Resultado: la ventana se abrió, mostró el panel de conversación, el campo de mensaje y el botón `Enviar`. La tecla Esc cerró la aplicación con código de salida 0.

Verificación de configuración local:

- Se comprobó que existe una clave configurada en `.env` sin exponer su valor.
- No se envió un prompt real al modelo durante la prueba automática para evitar consumo innecesario de API.

Verificaciones de Git:

- Todos los cambios del trabajo están dentro de `practicos/63174 - Jerez, Luciano Germán/tp6`.
- La rama contiene más de cinco commits parciales descriptivos.

## Limitaciones y supuestos

- Las herramientas trabajan con archivos de texto. No están pensadas para binarios.
- La aplicación asume que se ejecuta desde la carpeta del TP6.
- La validación automática realizada no envió mensajes al proveedor; el flujo de streaming queda implementado y compilado, pero la prueba con modelo real depende de una clave válida, disponibilidad del proveedor y modelo configurado.
