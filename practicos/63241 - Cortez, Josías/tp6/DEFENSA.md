# Defensa TP6

## Idea general

La aplicacion es un asistente conversacional de terminal. La interfaz esta hecha
con Terminal.Gui y la comunicacion con el modelo se hace mediante
`IChatClient`, que viene de Microsoft.Extensions.AI.

## Partes principales

- Arranque: carga `.env`, valida URL, modelo y API key, lee `AGENTS.md` y crea el cliente.
- Historial: `List<ChatMessage>` guarda el mensaje de sistema y cada turno del usuario y del asistente.
- Interfaz: `ChatWindow` arma el panel de conversacion, el campo de entrada y el boton Enviar.
- Streaming: `GetStreamingResponseAsync` entrega fragmentos y cada fragmento actualiza el Markdown.
- Tools: `leer-archivo`, `escribir-archivo` y `listar-archivos` son funciones C# registradas con `AIFunctionFactory`.
- Seguridad: las rutas se resuelven con `Path.GetFullPath` y se rechaza cualquier acceso fuera del directorio del TP.

## Preguntas probables

### Como recuerda la conversacion?

El modelo no guarda memoria. La aplicacion guarda todos los mensajes en
`mensajes` y manda la lista completa en cada consulta.

### Para que sirve AGENTS.md?

Es el mensaje de sistema. Define tono, idioma y reglas del asistente sin
tener que recompilar el programa.

### Para que sirve IChatClient?

Permite que la logica de la app dependa de una interfaz comun y no del SDK
concreto de un proveedor.

### Que hace UseFunctionInvocation?

Agrega una capa que detecta pedidos de herramientas del modelo, ejecuta la
funcion C# correspondiente y devuelve el resultado al modelo.

### Por que se bloquea la entrada mientras responde?

Para evitar dos envios al mismo tiempo y mantener ordenado el historial.
