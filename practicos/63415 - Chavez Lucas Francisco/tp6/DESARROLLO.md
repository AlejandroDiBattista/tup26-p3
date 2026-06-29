# Desarrollo TP6 - AsistenteIA

## Objetivo y alcance

El objetivo fue completar el punto de partida del TP6 para transformarlo en una aplicacion de consola interactiva con Terminal.Gui v2, Microsoft.Extensions.AI y un proveedor compatible con OpenAI. La solucion permite conversar con un modelo, conservar contexto durante la sesion, mostrar respuestas en streaming, renderizar Markdown y exponer herramientas de archivos mediante function calling.

El alcance queda limitado al directorio del proyecto desde el que se ejecuta la aplicacion. Las herramientas no permiten leer ni escribir rutas fuera de ese directorio.

## Requisitos interpretados

- Cargar el prompt de sistema desde `AGENTS.md`.
- Leer configuracion del proveedor desde variables de entorno.
- Crear un `IChatClient` compatible con OpenAI.
- Mantener historial con roles sistema, usuario y asistente.
- Enviar mensajes con Enter o con el boton Enviar.
- Mostrar la respuesta del asistente a medida que llega.
- Deshabilitar entrada y boton durante una respuesta en curso.
- Renderizar la conversacion como Markdown.
- Salir limpiamente con Esc.
- Exponer las herramientas `leer-archivo`, `escribir-archivo` y `listar-archivos`.
- Documentar y verificar la solucion.

## Arquitectura y decisiones

La implementacion se mantiene en `asistente.cs` porque el proyecto inicial es una file-based app de C#. Para conservar separacion de responsabilidades, el archivo define clases internas con roles claros:

- `AssistantConfig`: lee variables de entorno, valida proveedor, modelo y endpoint.
- `ProjectFileTools`: declara las herramientas entregadas al modelo con `AIFunctionFactory`.
- `ChatSession`: conserva el historial real y ejecuta `GetStreamingResponseAsync`.
- `AssistantWindow`: contiene la interfaz Terminal.Gui y delega la conversacion a `ChatSession`.

Se agrego la referencia explicita a `Microsoft.Extensions.AI@10.4.0` porque la invocacion automatica de herramientas se construye con `AsBuilder().UseFunctionInvocation().Build()`, API que no estaba disponible solamente con el paquete OpenAI.

La URL del proveedor se normaliza para aceptar valores de `.env.ejemplo` terminados en `/chat/completions`. El SDK de OpenAI espera el endpoint base compatible, por eso se remueve ese sufijo antes de crear `OpenAIClientOptions`.

## Herramientas de archivos

Las herramientas se exponen con nombres pedidos por el enunciado:

- `leer-archivo`: lee un archivo de texto relativo al directorio del proyecto.
- `escribir-archivo`: crea o sobrescribe un archivo de texto relativo al proyecto.
- `listar-archivos`: lista carpetas y archivos de un directorio relativo.

Todas las rutas pasan por `ResolveProjectPath`. Si una ruta intenta salir del directorio del proyecto con `..` o con una ruta absoluta externa, la herramienta devuelve un error en vez de operar sobre el sistema de archivos.

## Interfaz

La ventana principal usa pantalla completa y divide el espacio en dos zonas:

- Panel superior `Markdown` para el historial de conversacion.
- Panel inferior con `TextField`, boton `Enviar` y estado.

El envio se puede iniciar con Enter o con el boton. Mientras el asistente responde, la entrada y el boton quedan deshabilitados. La conversacion hace auto-scroll al agregar nuevos turnos y deja de forzarlo si el usuario interactua con el panel de conversacion.

## Manejo de errores

- Si faltan variables de entorno, el arranque muestra un mensaje claro.
- Si el modelo no devuelve texto, se agrega una respuesta explicita al historial.
- Si la respuesta supera dos minutos, se informa tiempo agotado.
- Si una llamada falla, `ChatSession` retira el ultimo mensaje de usuario para no dejar historial inconsistente.
- Las herramientas capturan errores de archivo y devuelven mensajes legibles al modelo.

## Como compilar y ejecutar

Desde la raiz del repositorio:

```bash
dotnet build "practicos/63415 - Chavez Lucas Francisco/tp6/asistente.cs"
```

Desde la carpeta del TP:

```bash
cd "practicos/63415 - Chavez Lucas Francisco/tp6"
cp .env.ejemplo .env
```

Editar `.env` con la clave y el modelo del proveedor elegido. Luego ejecutar:

```bash
dotnet run asistente.cs
```

Para usar otro proveedor configurado en `.env`, pasar su nombre:

```bash
dotnet run asistente.cs -- GROQ
dotnet run asistente.cs -- OLLAMA
```

Para validar configuracion sin abrir la interfaz:

```bash
dotnet run asistente.cs -- --check
dotnet run asistente.cs -- GROQ --check
```

## Pruebas realizadas

- `dotnet build "practicos/63415 - Chavez Lucas Francisco/tp6/asistente.cs"`: compilacion correcta.
- Verificacion estatica de requisitos contra el enunciado: historial, streaming, Markdown, salida con Esc, bloqueo de entrada y herramientas.
- Validacion de que los cambios Git quedaron dentro de `practicos/63415 - Chavez Lucas Francisco/tp6`.

No se ejecuto una conversacion real contra un proveedor remoto para evitar depender de una API key personal o de conectividad externa durante la entrega. La app queda preparada para hacerlo con las variables de entorno configuradas.

## Limitaciones y supuestos

- El historial se conserva solo en memoria durante la sesion.
- Las herramientas operan sobre archivos de texto; no intentan interpretar binarios.
- El proveedor elegido debe implementar streaming y function calling de forma compatible con el SDK OpenAI/MEAI.
- La tecla Esc cierra la aplicacion completa; no se usa para cancelar una respuesta en curso.
