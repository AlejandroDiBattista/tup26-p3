# Requerimientos

Crear una app C# autocontenida en un unico archivo `WhatsAppBot.cs`, ejecutable con `dotnet run WhatsAppBot.cs`.

## Dependencias

- .NET 10 SDK con soporte file-based apps.
- `wacli` instalado y autenticado previamente con WhatsApp.
- Variable de entorno `OPENAI_API_KEY` configurada.
- Paquetes NuGet declarados en el archivo:
  - `Microsoft.Extensions.AI`
  - `Microsoft.Extensions.AI.OpenAI`
  - `OpenAI`

## Configuracion

- Contacto fijo en constante: `+5493815343458`.
- Modelo: `gpt-5.5`.
- Prompt del asistente tomado desde `AGENTS.md`.
- Archivo de agenda administrado por el asistente: `CONTACTOS.md`.

## Comportamiento

- Iniciar `wacli sync --follow --refresh-contacts` en segundo plano.
- Al arrancar, leer los ultimos mensajes del contacto y marcarlos como ya vistos sin imprimirlos.
- Monitorear periodicamente mensajes nuevos del contacto.
- Enviar cada mensaje nuevo a un `IChatClient` de `Microsoft.Extensions.AI`.
- Permitir al asistente leer, escribir y listar archivos del workspace.
- Enviar la respuesta por WhatsApp con `wacli send text`.
- Registrar en consola cada mensaje de usuario y asistente.
- Si recibe `/finalizar`, cerrar el bot y detener el proceso de sincronizacion de `wacli`.

## Estructura

- Configuracion: constantes al inicio del archivo.
- Flujo principal: iniciar servicios, crear el cliente de chat, leer mensajes nuevos, responder y salir solo con `/finalizar`.
- `SecretaryAgent`: encapsula el cliente MEAI, las opciones, el historial y la llamada al modelo.
- `AgentFiles`: herramientas del asistente para leer, escribir y listar archivos.
- `WhatsAppClient`: encapsula toda la comunicacion con `wacli`.
