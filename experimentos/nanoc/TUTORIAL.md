# NanoC

## Objetivo

En este tutorial vamos a construir una mini aplicacion de consola en C# que usa:

- Microsoft Agent Framework
- La API de OpenAI
- Tools locales para leer archivos, escribir archivos y ejecutar comandos

La app final se llama `NanoC` y funciona como un agente local de programacion. El usuario escribe pedidos en consola y el agente puede responder usando el modelo y, cuando hace falta, invocar tools que viven en nuestro proceso.

---

## 1. Primeros principios

Antes de escribir codigo, hace falta entender el modelo mental correcto.

### 1.1 Un LLM no ejecuta cosas

Un modelo de lenguaje no "abre archivos" ni "corre comandos" por si solo. Lo unico que hace es producir tokens.

Cuando parece que "usa una tool", en realidad ocurre esto:

1. El modelo recibe el prompt, el historial y la definicion de las tools.
2. El modelo decide que necesita una tool.
3. El framework detecta ese pedido.
4. Tu programa ejecuta la tool real.
5. El resultado vuelve al modelo.
6. El modelo redacta la respuesta final.

La consecuencia importante es esta:

- El modelo decide.
- Tu aplicacion ejecuta.
- El framework orquesta el ida y vuelta.

### 1.2 Que agrega un agent framework

Podrias hablar con OpenAI directamente, pero tendrias que implementar:

- historial de mensajes
- tools y sus schemas
- loop de tool calling
- sesion
- conversion entre formatos

Agent Framework te evita esa parte repetitiva. Vos definis:

- instrucciones
- tools
- provider
- modo de sesion

Y el framework resuelve la mecanica.

### 1.3 Que es una tool

Una tool es una funcion de C# que expones al modelo como una capacidad.

Ejemplos:

- `ReadFile(path)`
- `WriteFile(path, content)`
- `RunShell(command)`

La diferencia entre una funcion comun y una tool es que la tool:

- tiene nombre
- tiene descripcion
- tiene parametros descriptos
- puede ser llamada por el agente

### 1.4 El principio mas importante

No le des al agente mas poder del que necesita.

Por eso en `NanoC`:

- restringimos archivos al workspace
- bloqueamos algunos comandos peligrosos
- ponemos timeout al shell

El aprendizaje de agentes no es solo "hacer que ande". Tambien es "diseñar limites correctos".

---

## 2. Arquitectura de la app

La app tiene cuatro piezas:

### 2.1 Configuracion

Lee:

- `OPENAI_API_KEY`
- `OPENAI_MODEL`
- `NANOC_WORKSPACE`

Esto evita hardcodear secretos y hace reproducible la app.

### 2.2 Prompt base

El archivo `AGENTS.MD` define la personalidad tecnica del agente. Luego el programa le agrega reglas operativas:

- trabajar en cierto workspace
- leer antes de modificar
- no inventar resultados
- responder corto

Separar prompt y codigo es una buena practica porque:

- podes iterar instrucciones sin recompilar
- versionas el prompt junto al codigo
- haces visible el contrato del agente

### 2.3 Tools

Creamos una clase `WorkspaceTools` con tres metodos:

- `ReadFile`
- `WriteFile`
- `RunShell`

Luego esos metodos se transforman en tools mediante `AIFunctionFactory.Create(...)`.

### 2.4 Loop interactivo

La CLI:

1. crea el agente
2. crea una sesion
3. lee una linea del usuario
4. la envia al agente
5. imprime la respuesta
6. repite

Ademas agregamos comandos locales:

- `/help`
- `/reset`
- `/workspace`
- `/model`

Estos no pasan por el modelo. Los resuelve directamente la app.

---

## 3. Crear el proyecto paso a paso

### 3.1 Crear la aplicacion

```bash
dotnet new console -n nanoc
cd nanoc
```

### 3.2 Instalar el paquete necesario

```bash
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
```

Ese paquete trae la integracion entre Agent Framework y OpenAI.

### 3.3 Crear el prompt

Archivo: `AGENTS.MD`

```text
Eres un programador experto en C#, .NET y automatizacion de tareas de desarrollo.

Trabajas como un asistente tecnico sobrio y preciso.

Tu objetivo es ayudar al usuario a programar dentro del workspace {workspace}.
```

La marca `{workspace}` se reemplaza en tiempo de ejecucion.

### 3.4 Crear la configuracion

La clase `NanoConfiguration` resuelve:

- la API key
- el modelo
- el workspace
- la ruta del prompt

Primer principio:

- La configuracion es una frontera entre tu app y el entorno.
- No mezcles secretos con logica.

### 3.5 Crear el loader del prompt

La clase `PromptLoader`:

1. lee `AGENTS.MD`
2. reemplaza `{workspace}`
3. concatena reglas operativas

Esto permite separar:

- identidad del agente
- restricciones operativas

### 3.6 Crear las tools

En `WorkspaceTools` implementamos:

#### `ReadFile`

Lee un archivo UTF-8 dentro del workspace.

Aprendizaje:

- una tool no tiene por que ser compleja
- una tool chica y bien definida suele funcionar mejor que una tool multiproposito

#### `WriteFile`

Escribe contenido y crea carpetas si faltan.

Aprendizaje:

- si la app interactua con el sistema de archivos, conviene que cada tool haga una sola cosa
- eso vuelve mas predecible el comportamiento del agente

#### `RunShell`

Ejecuta `zsh -lc "<comando>"` en el workspace.

Tambien:

- bloquea comandos peligrosos
- limita a 30 segundos
- captura `stdout`, `stderr` y `exit code`

Aprendizaje:

- una tool que interactua con el sistema operativo necesita politicas
- "tool poderosa" sin limites equivale a "agente dificil de controlar"

### 3.7 Transformar metodos en tools

```csharp
tools:
[
    AIFunctionFactory.Create(tools.ReadFile),
    AIFunctionFactory.Create(tools.WriteFile),
    AIFunctionFactory.Create(tools.RunShell)
]
```

Ese es el puente entre:

- tu codigo C#
- el mundo del modelo

### 3.8 Crear el agente

```csharp
var openAiClient = new OpenAIClient(apiKey);
var responsesClient = openAiClient.GetResponsesClient();

var agent = responsesClient.AsAIAgent(
    model: model,
    instructions: instructions,
    name: "NanoC",
    description: "Agente de consola para programacion asistida.",
    tools: [...]);
```

Idea central:

- `OpenAIClient` habla con OpenAI
- `ResponsesClient` usa la Responses API
- `AsAIAgent(...)` envuelve ese cliente en un agente con sesion y tools

### 3.9 Crear una sesion

```csharp
var session = await agent.CreateSessionAsync();
```

La sesion guarda la continuidad conversacional.

Sin sesion, cada mensaje seria independiente.

Con sesion, el agente puede recordar el contexto de la charla.

### 3.10 Ejecutar el loop

```csharp
var response = await agent.RunAsync(
    new ChatMessage(ChatRole.User, input),
    session);
```

Esto le da al agente:

- el mensaje del usuario
- el historial previo asociado a la sesion

Y devuelve un `AgentResponse`, cuyo texto final se lee con `response.Text`.

---

## 4. Como reproducir la app

### 4.1 Variables de entorno

macOS / Linux:

```bash
export OPENAI_API_KEY="sk-..."
export OPENAI_MODEL="gpt-4.1-mini"
export NANOC_WORKSPACE="/ruta/a/tu/proyecto"
```

Si `NANOC_WORKSPACE` no esta definido, la app usa el directorio actual.

### 4.2 Ejecutar

```bash
dotnet run
```

### 4.3 Casos de prueba sugeridos

#### Caso 1. Lectura de archivo

Prompt:

```text
Lee Program.cs y resumime su estructura.
```

Que deberia pasar:

- el agente deberia usar `read_file`
- luego resumir el contenido

#### Caso 2. Escritura de archivo

Prompt:

```text
Crea un archivo TODO.md con una lista de tres mejoras para esta app.
```

Que deberia pasar:

- el agente deberia usar `write_file`
- el archivo deberia quedar creado en el workspace

#### Caso 3. Uso de shell

Prompt:

```text
Ejecuta ls -la y decime que archivos hay en el proyecto.
```

Que deberia pasar:

- el agente deberia usar `run_shell`
- luego interpretar la salida

#### Caso 4. Iteracion sobre codigo

Prompt:

```text
Inspecciona Program.cs y proponeme una refactorizacion de bajo riesgo.
```

Que deberia pasar:

- primero lectura
- luego razonamiento
- opcionalmente escritura si se lo pedis

#### Caso 5. Reset de contexto

Comando:

```text
/reset
```

Que deberia pasar:

- se crea una nueva sesion
- se pierde el historial conversacional anterior

---

## 5. Por que esta app enseña mucho mas que "usar OpenAI"

Esta mini app enseña cinco ideas clave.

### 5.1 Separacion entre modelo y sistema

El modelo no es la aplicacion completa.

La aplicacion completa es:

- modelo
- tools
- reglas
- memoria
- validaciones
- UX

### 5.2 Diseñar interfaces para un agente

Una tool es una interfaz.

Si una interfaz es:

- ambigua
- demasiado grande
- demasiado peligrosa

el agente se vuelve menos confiable.

### 5.3 Los prompts son configuracion, no magia

El prompt mejora el comportamiento, pero no reemplaza:

- buenos nombres de tools
- buenas descripciones
- validaciones reales

### 5.4 La sesion cambia el tipo de aplicacion

Sin sesion tenes "preguntas y respuestas".

Con sesion tenes "asistente conversacional".

Ese cambio es conceptual, no cosmetico.

### 5.5 Un agente util necesita fronteras

Toda app agentica seria tiene que responder:

- que puede leer
- que puede escribir
- que comandos puede correr
- cuanto tiempo puede ejecutarse
- que pasa cuando falla

Eso no es un detalle. Es parte central del diseño.

---

## 6. Mejoras posibles

Una vez que entiendas esta base, podes extenderla.

### 6.1 Persistencia de sesion

Guardar la sesion a disco para reanudar conversaciones.

### 6.2 Mas tools especializadas

Ejemplos:

- `list_files`
- `search_text`
- `run_tests`
- `build_project`

### 6.3 Aprobacion humana

Pedir confirmacion antes de ejecutar ciertas tools sensibles.

### 6.4 Interfaz web

Mover la experiencia de CLI a una UI web o desktop.

### 6.5 Agentes multiples

Separar responsabilidades:

- agente planificador
- agente editor
- agente verificador

---

## 7. Resumen conceptual

Si tuvieras que quedarte con una sola idea, que sea esta:

> Un agente no es solo un modelo. Es un sistema que combina razonamiento, memoria, herramientas y limites operativos.

`NanoC` es pequeño, pero ya muestra la estructura esencial de una app agentica real:

- provider de IA
- agente
- tools
- sesion
- loop interactivo
- seguridad minima

Cuando entiendas esta arquitectura, vas a poder construir asistentes mas grandes sin depender de ejemplos magicos.

---

## 8. Archivos del proyecto

- `Program.cs`: implementacion principal
- `AGENTS.MD`: prompt base del agente
- `nanoc.csproj`: referencia a paquetes y copia del prompt al output
- `TUTORIAL.md`: esta guia

---

## 9. Ejecucion rapida

```bash
cd nanoc
export OPENAI_API_KEY="sk-..."
export OPENAI_MODEL="gpt-4.1-mini"
export NANOC_WORKSPACE="$(pwd)"
dotnet run
```

Prompt de ejemplo:

```text
Lee Program.cs, explicame la arquitectura y crea un archivo NOTES.md con tres mejoras posibles.
```
