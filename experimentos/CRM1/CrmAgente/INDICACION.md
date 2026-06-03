# Especificación — CRM con Agente IA

> Consigna para construir la aplicación **de una sola vez**. Describe el sistema
> final completo: qué hay que construir y con qué criterios, no cómo se llegó.

---

## Objetivo

Construí un **CRM mínimo pero funcional** para la materia *Programación III*
(UTN Tucumán). El foco pedagógico es doble:

1. **Modelo de dominio + acceso a datos** con EF Core sobre SQLite.
2. Un **agente de IA con *tool use***, cuya idea central a transmitir es:
   **el LLM razona, nuestro código ejecuta**. El modelo no ve la base de datos;
   solo decide qué herramienta llamar y con qué parámetros, y nuestro código
   ejecuta el método real sobre el dominio.

## Stack

- **.NET 10**, **Blazor Server** (componentes interactivos en el servidor).
- **EF Core + SQLite** (un archivo `crm.db` en la carpeta del proyecto).
- Agente sobre la **Responses API de OpenAI** (`POST /v1/responses`), llamada
  con `HttpClient` crudo (sin SDK), para que se vea el mecanismo por dentro.
- La API key se lee de configuración o de la variable de entorno
  `OPENAI_API_KEY`. **Nunca** hardcodeada. Si falta, el CRM funciona igual y
  solo el agente queda deshabilitado con un aviso claro.

---

## Modelo de dominio

Tres entidades con relaciones 1-N (`Contacto` es el centro):

- **Contacto**: `Id`, `Nombre`, `Empresa`, `Email`, `Telefono`, `CreadoEn`,
  y las colecciones `Oportunidades` y `Actividades`.
- **Oportunidad**: `Id`, `Descripcion`, `Monto` (decimal), `Etapa` (enum),
  `CreadoEn`, `ContactoId` + navegación a `Contacto`.
- **Actividad**: `Id`, `Tipo` (Llamada/Mail/Reunion/Nota), `Detalle`,
  `Fecha` (default `DateTime.Now`), `ContactoId` + navegación.
- **Etapa** (enum): `Prospecto, Contactado, Propuesta, Negociacion, Ganada, Perdida`.

## Capa de datos (EF Core)

- `CrmContext : DbContext` con un `DbSet` por entidad.
- En `OnModelCreating`: guardar `Etapa` como **texto** (`HasConversion<string>()`)
  y configurar **borrado en cascada** de oportunidades y actividades al borrar
  un contacto.
- Un método `Seed(db)` que carga datos de ejemplo **solo si la base está vacía**.
- En `Program.cs`: `EnsureCreated()` + `Seed()` al arrancar.

## Capa de servicios — regla de oro de la arquitectura

Toda la lógica de negocio vive en **un único `CrmService`**. Tanto los
componentes Blazor **como el agente** consumen estos mismos métodos. **No hay
lógica duplicada.** Si la capa de datos no funciona, el agente tampoco: no se
puede delegar en la IA el aprendizaje del modelo de datos.

Operaciones que debe ofrecer:

- **Contactos**: listar/buscar (por nombre o empresa), ficha 360 (con `Include`
  de oportunidades y actividades), crear, actualizar (pisando **solo** los
  campos provistos).
- **Actividades**: registrar una actividad para un contacto, con **fecha
  opcional** (si no se indica, `DateTime.Now`).
- **Oportunidades**: buscar (filtros opcionales por etapa y monto mínimo),
  crear (etapa inicial opcional, por defecto `Prospecto`), mover de etapa,
  eliminar, y listar para el pipeline. Los métodos que modifican una
  oportunidad devuelven la entidad afectada (para poder reaccionar a ella).

---

## Interfaz de usuario

**Layout.** Una **barra superior** con la marca y dos **solapas** de
navegación — *Contactos* y *Pipeline* — y, fija a la derecha, una columna con
el **panel del Agente IA** (visible en toda la app). Estética propia
(tipografías serif + sans, paleta cálida), no el look genérico de framework.

**Contactos (`/`).** Lista con búsqueda por texto y alta de contacto; click en
un ítem abre su ficha. Si el agente aplicó un filtro, mostrar un *banner* con la
cantidad y un botón "Ver todos", y resaltar/limitar la lista a esos contactos.

**Ficha (`/contacto/{id}`).** Vista 360 que compone las tres entidades: datos
del contacto, sus oportunidades con el total en juego, y el historial de
actividades (con **fecha y hora**). Permite dar de alta oportunidades y
actividades. Al entrar, registra este contacto como "activo" para el agente.

**Pipeline (`/pipeline`).** Tablero con una columna por etapa y el total por
columna. Incluye un **filtro de texto** que busca por descripción de la
oportunidad y también por datos del contacto (nombre/empresa). Cada tarjeta
tiene un selector para **mover la oportunidad de etapa**. Si el agente aplicó un
filtro, mostrar un *banner* con "Ver todas" y limitar el tablero a esas
oportunidades. El tablero se **actualiza solo** cuando el agente modifica datos.

---

## El agente IA

Implementá un `CrmAgent` que use la **Responses API** en un **ida y vuelta**
(round-trip):

1. El usuario pregunta en lenguaje natural.
2. Se manda la pregunta + las **descripciones de las herramientas** al modelo.
3. El modelo pide ejecutar una herramienta (`function_call`).
4. Nuestro código ejecuta el método real del `CrmService`.
5. Se le devuelve el resultado (`function_call_output`).
6. El modelo redacta la respuesta final en lenguaje natural.

Requisitos del bucle:

- Hasta ~5 vueltas (el modelo puede encadenar herramientas).
- Mantener el **historial** de la conversación entre mensajes, con un botón
  "Nueva" que lo reinicia (y limpia filtros/estados asociados).
- Si el modelo emite ítems `reasoning`, reincorporarlos al input junto al
  `function_call` (la API los exige).

### Herramientas (esquemas que el modelo recibe)

- `buscar_contactos(texto?)`
- `crear_contacto(nombre, empresa?, email?, telefono?)`
- `actualizar_contacto(contactoId, ...campos opcionales)` — actualiza solo lo provisto.
- `registrar_actividad(contactoId, tipo, detalle, fecha?)` — `fecha` en ISO 8601, opcional.
- `buscar_oportunidades(etapa?, montoMinimo?)` — devuelve `id`, contacto, descripción, monto y etapa.
- `crear_oportunidad(contactoId, descripcion, monto, etapa?)`
- `cambiar_etapa_oportunidad(oportunidadId, etapa)`
- `eliminar_oportunidad(oportunidadId)`

### Instrucciones (system prompt)

- **Identificar contactos:** usar el contacto activo si el usuario está en una
  ficha; si no, buscar por nombre/empresa; con un solo resultado proceder, con
  varios pedir que elija, sin resultados ofrecer crear. **Nunca pedir el ID al
  usuario.**
- **Oportunidades:** para cambiar etapa o eliminar, ubicarla primero con
  `buscar_oportunidades`; si un contacto tiene varias, desambiguar. Eliminar es
  **permanente**: confirmar ante cualquier duda.
- **Fechas:** inyectar en las instrucciones la **fecha y hora actual** para que
  el modelo resuelva referencias relativas ("hoy", "ayer", "anteayer",
  "mañana", "la semana pasada") y mande la fecha en ISO 8601; si el usuario no
  menciona fecha, omitir el parámetro.
- **No enumerar en texto** las listas de contactos candidatos ni de
  oportunidades: el panel ya las muestra; dar solo un resumen corto.
- Responder en **español rioplatense**, conciso, montos con separador de miles.

---

## Integración agente ↔ UI

La comunicación entre el agente y las pantallas se hace por **un servicio con
eventos** (`AgenteFiltroService`), no acoplando componentes. Debe coordinar:

- **Contacto activo** (el de la ficha abierta) y su refresco: si el agente
  cambia los datos del contacto que se está viendo, el encabezado del panel
  ("Viendo: …") se actualiza.
- **Filtro de contactos** que reflejan **exactamente** los contactos que la
  **respuesta actual** del agente referencia — ni los de turnos anteriores, ni
  menos. (El conjunto de referenciados se reinicia en cada pregunta.)
- **Filtro de oportunidades** con la misma regla, aplicado al Pipeline.
- **Desambiguación de contactos:** cuando una búsqueda devuelve varios, el panel
  los muestra como **lista clicable**; al elegir uno, el agente continúa la
  acción pendiente.
- **Presentación de oportunidades:** cuando el agente las lista, el panel las
  muestra como **tarjetas** (descripción, badge de etapa, contacto, monto).
- **Navegación dirigida:** las acciones sobre un contacto llevan a su **ficha**;
  las que referencian oportunidades llevan al **Pipeline** (y lo dejan filtrado).
- Las pantallas se **suscriben** a los cambios del servicio para refrescarse en
  vivo sin recargar.

---

## Restricciones pedagógicas

- **El LLM nunca toca la base.** Toda lectura/escritura pasa por `CrmService`.
- **Sin lógica duplicada** entre UI y agente.
- **Sin secretos hardcodeados**; el agente es una capa opcional encima del CRM.
- Código **comentado con intención didáctica** (explicar el *por qué*).
- Comunicación UI ↔ agente vía un **servicio con eventos**, no acoplando
  componentes entre sí.

## Criterios de aceptación

- La app compila y arranca; `crm.db` se crea con datos de ejemplo.
- Sin API key: el CRM funciona, el agente queda deshabilitado con aviso claro.
- Con API key: se puede consultar y **modificar** el CRM en lenguaje natural
  (contactos, actividades con fechas relativas, y oportunidades: crear, mover de
  etapa, eliminar).
- Contactos y Pipeline reflejan en vivo lo que el agente hace, mostrando
  **exactamente** los registros que el asistente referencia, y la navegación
  acompaña la acción (ficha o pipeline según corresponda).
