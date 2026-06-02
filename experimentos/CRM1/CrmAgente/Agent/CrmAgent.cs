using System.Globalization;
using System.Text;
using System.Text.Json;
using CrmAgente.Models;
using CrmAgente.Services;

namespace CrmAgente.Agent;

// ============================================================
//  EL AGENTE
// ------------------------------------------------------------
//  Idea central a transmitir en clase:
//
//  El LLM NO consulta la base de datos. No sabe SQL, no ve las
//  tablas. Lo unico que hace es DECIDIR que herramienta llamar y
//  con que parametros. Nuestro codigo ejecuta la herramienta de
//  verdad (que por debajo usa EF Core) y le devuelve los datos al
//  modelo para que los redacte en lenguaje natural.
//
//  Resumen: el LLM razona, nuestro codigo ejecuta.
//
//  El flujo es un IDA Y VUELTA (round-trip):
//    1. Usuario pregunta en lenguaje natural
//    2. Mandamos pregunta + descripcion de herramientas al modelo
//    3. El modelo pide ejecutar una herramienta (function_call)
//    4. Nuestro codigo ejecuta el metodo real del CrmService
//    5. Le devolvemos el resultado al modelo (function_call_output)
//    6. El modelo redacta la respuesta final en lenguaje natural
//
//  Usa la Responses API de OpenAI (POST /v1/responses).
// ============================================================
public class CrmAgent {
    private readonly CrmService crm;
    private readonly AgenteFiltroService filtro;
    private readonly HttpClient http;
    private readonly string apiKey;

    private const string Modelo = "gpt-5.5";

    public CrmAgent(CrmService crm, AgenteFiltroService filtro, IHttpClientFactory httpFactory, IConfiguration config) {
        this.crm = crm;
        this.filtro = filtro;
        this.http = httpFactory.CreateClient();
        // La API key se lee de configuracion / variable de entorno.
        // Nunca se hardcodea en el codigo.
        this.apiKey = config["OPENAI_API_KEY"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? "";
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(this.apiKey);

    // Historial acumulado de la conversacion. Persiste entre llamadas a PreguntarAsync
    // para que el agente recuerde el contexto de turnos anteriores.
    private readonly List<object> historial = new();

    // Contactos que referencia la RESPUESTA ACTUAL (los de la pregunta en curso:
    // busquedas, altas, actualizaciones, actividades). Se acumulan dentro de un mismo
    // turno (por si una pregunta toca varios) pero se vacian al empezar cada pregunta,
    // para que el filtro de la pantalla de Contactos muestre EXACTAMENTE los mismos
    // contactos que el asistente esta mostrando, ni mas ni menos.
    private readonly HashSet<int> contactosReferidos = new();

    // Idem para las oportunidades que referencia la respuesta actual: el filtro del
    // Pipeline muestra EXACTAMENTE las que el asistente esta mostrando.
    private readonly HashSet<int> oportunidadesReferidas = new();

    public void LimpiarHistorial() {
        this.historial.Clear();
        this.contactosReferidos.Clear();
        this.oportunidadesReferidas.Clear();
        this.filtro.LimpiarFiltro();
        this.filtro.LimpiarFiltroOportunidades();
        this.filtro.LimpiarCandidatos();
        this.filtro.LimpiarOportunidadesMostradas();
    }

    // Suma contactos a los referenciados por la respuesta actual y reaplica el filtro
    // (los contactos de esta pregunta hasta el momento).
    private void ReferenciarContactos(IEnumerable<int> ids) {
        foreach (var id in ids) this.contactosReferidos.Add(id);
        this.filtro.SetFiltro(this.contactosReferidos);
    }

    // Idem para oportunidades: reaplica el filtro del Pipeline con las de esta respuesta
    // y, si hay al menos una, pide mostrar la pagina Pipeline para que se vea filtrada.
    private void ReferenciarOportunidades(IEnumerable<int> ids) {
        foreach (var id in ids) this.oportunidadesReferidas.Add(id);
        this.filtro.SetFiltroOportunidades(this.oportunidadesReferidas);
        if (this.oportunidadesReferidas.Count > 0)
            this.filtro.SolicitarNavegacionPipeline();
    }

    // ---- Definicion de las herramientas que el modelo puede usar ----
    // Esto es un ESQUEMA (descripcion), no codigo. Le dice al LLM que
    // funciones existen, que hacen y que parametros aceptan. El modelo
    // usa estas descripciones para decidir cual llamar.
    private static object[] DefinirHerramientas() => new object[] {
        new {
            type = "function",
            name = "buscar_contactos",
            description = "Busca contactos por nombre o empresa. Devuelve id, nombre, empresa, email y telefono. " +
                          "Usar para encontrar el contactoId antes de actualizar datos o registrar una actividad.",
            parameters = new {
                type = "object",
                properties = new {
                    texto = new {
                        type = "string",
                        description = "Texto a buscar en nombre o empresa (opcional; sin texto devuelve todos)"
                    }
                }
            }
        },
        new {
            type = "function",
            name = "crear_contacto",
            description = "Crea un nuevo contacto en el CRM.",
            parameters = new {
                type = "object",
                properties = new {
                    nombre   = new { type = "string", description = "Nombre completo del contacto" },
                    empresa  = new { type = "string", description = "Empresa u organizacion" },
                    email    = new { type = "string", description = "Email" },
                    telefono = new { type = "string", description = "Telefono" }
                },
                required = new[] { "nombre" }
            }
        },
        new {
            type = "function",
            name = "actualizar_contacto",
            description = "Actualiza los datos de un contacto. IMPORTANTE: incluir SOLO los campos que se quieren cambiar. No incluir los demas campos ni mandarlos vacios. Los campos omitidos se dejan intactos.",
            parameters = new {
                type = "object",
                properties = new {
                    contactoId = new { type = "integer", description = "ID del contacto (usar buscar_contactos para obtenerlo)" },
                    nombre     = new { type = "string",  description = "Nuevo nombre (opcional)" },
                    empresa    = new { type = "string",  description = "Nueva empresa (opcional)" },
                    email      = new { type = "string",  description = "Nuevo email (opcional)" },
                    telefono   = new { type = "string",  description = "Nuevo telefono (opcional)" }
                },
                required = new[] { "contactoId" }
            }
        },
        new
        {
            type = "function",
            name = "registrar_actividad",
            description = "Registra una actividad en el historial de un contacto: llamada, mail, reunion o nota.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    contactoId = new { type = "integer", description = "ID del contacto" },
                    tipo = new
                    {
                        type = "string",
                        @enum = new[] { "Llamada", "Mail", "Reunion", "Nota" },
                        description = "Tipo de actividad"
                    },
                    detalle = new { type = "string", description = "Descripcion de lo que se hizo o hablo" },
                    fecha = new
                    {
                        type = "string",
                        description = "Fecha y hora de la actividad en formato ISO 8601 (ej: 2026-05-29T14:30). " +
                                      "OPCIONAL: si se omite se usa la fecha y hora actual. Resolve referencias " +
                                      "relativas como 'ayer', 'anteayer', 'mañana' o 'la semana pasada' usando la " +
                                      "FECHA Y HORA ACTUAL indicada en las instrucciones, y pasa el resultado en ISO 8601."
                    }
                },
                required = new[] { "contactoId", "tipo", "detalle" }
            }
        },
        new
        {
            type = "function",
            name = "buscar_oportunidades",
            description = "Busca oportunidades de venta filtrando por etapa del pipeline y/o monto minimo. " +
                          "Devuelve la lista con id, contacto, descripcion, monto y etapa. " +
                          "Usar para obtener el oportunidadId antes de cambiar su etapa.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    etapa = new
                    {
                        type = "string",
                        @enum = new[] { "Prospecto", "Contactado", "Propuesta", "Negociacion", "Ganada", "Perdida" },
                        description = "Etapa del pipeline por la que filtrar (opcional)"
                    },
                    montoMinimo = new
                    {
                        type = "number",
                        description = "Monto minimo de la oportunidad (opcional)"
                    }
                }
            }
        },
        new
        {
            type = "function",
            name = "cambiar_etapa_oportunidad",
            description = "Cambia la etapa (estado del pipeline) de una oportunidad de venta. " +
                          "Usar buscar_oportunidades primero para obtener el oportunidadId.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    oportunidadId = new { type = "integer", description = "ID de la oportunidad (usar buscar_oportunidades para obtenerlo)" },
                    etapa = new
                    {
                        type = "string",
                        @enum = new[] { "Prospecto", "Contactado", "Propuesta", "Negociacion", "Ganada", "Perdida" },
                        description = "Nueva etapa del pipeline"
                    }
                },
                required = new[] { "oportunidadId", "etapa" }
            }
        },
        new
        {
            type = "function",
            name = "crear_oportunidad",
            description = "Crea una nueva oportunidad de venta para un contacto. " +
                          "Usar buscar_contactos primero para obtener el contactoId (o el contacto activo).",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    contactoId  = new { type = "integer", description = "ID del contacto duenio de la oportunidad" },
                    descripcion = new { type = "string",  description = "Descripcion de la oportunidad (que se vende)" },
                    monto       = new { type = "number",  description = "Monto estimado de la oportunidad" },
                    etapa = new
                    {
                        type = "string",
                        @enum = new[] { "Prospecto", "Contactado", "Propuesta", "Negociacion", "Ganada", "Perdida" },
                        description = "Etapa inicial (opcional; por defecto Prospecto)"
                    }
                },
                required = new[] { "contactoId", "descripcion", "monto" }
            }
        },
        new
        {
            type = "function",
            name = "eliminar_oportunidad",
            description = "Elimina (borra) una oportunidad de venta. Es permanente. " +
                          "Usar buscar_oportunidades primero para obtener el oportunidadId.",
            parameters = new
            {
                type = "object",
                properties = new
                {
                    oportunidadId = new { type = "integer", description = "ID de la oportunidad a eliminar (usar buscar_oportunidades para obtenerlo)" }
                },
                required = new[] { "oportunidadId" }
            }
        }
    };

    // ---- Ejecuta la herramienta que el modelo pidio ----
    // Aca es donde nuestro codigo real corre. Tomamos el nombre de la
    // tool y sus parametros, y llamamos al metodo correspondiente del
    // CrmService (que usa EF Core y toca SQLite de verdad).
    private async Task<string> EjecutarHerramientaAsync(string nombre, JsonElement input) {
        if (nombre == "buscar_contactos") {
            string? texto = null;
            if (input.TryGetProperty("texto", out var t) && t.ValueKind == JsonValueKind.String)
                texto = t.GetString();

            var resultados = await this.crm.ListarContactosAsync(texto);
            if (resultados.Count > 0)
                this.ReferenciarContactos(resultados.Select(c => c.Id));

            // Si hay AMBIGUEDAD (mas de un contacto coincide), publicamos los
            // candidatos para que el panel los muestre como lista clicable y el
            // usuario elija cual. Con un solo resultado no hace falta elegir.
            if (resultados.Count > 1)
                this.filtro.SetCandidatos(resultados.Select(c =>
                    new AgenteFiltroService.ContactoResumen(c.Id, c.Nombre, c.Empresa)));
            else
                this.filtro.LimpiarCandidatos();

            return JsonSerializer.Serialize(resultados.Select(c => new {
                id = c.Id, nombre = c.Nombre, empresa = c.Empresa,
                email = c.Email, telefono = c.Telefono
            }));
        }

        if (nombre == "crear_contacto") {
            var nomb = input.TryGetProperty("nombre",   out var n)  ? n.GetString()  ?? "" : "";
            var emp  = input.TryGetProperty("empresa",  out var e)  ? e.GetString()  ?? "" : "";
            var mail = input.TryGetProperty("email",    out var em) ? em.GetString() ?? "" : "";
            var tel  = input.TryGetProperty("telefono", out var tf) ? tf.GetString() ?? "" : "";

            var contacto = await this.crm.CrearContactoAsync(nomb, emp, mail, tel);
            this.ReferenciarContactos(new[] { contacto.Id });
            this.filtro.SolicitarNavegacion(contacto.Id);

            return JsonSerializer.Serialize(new {
                id = contacto.Id, nombre = contacto.Nombre,
                mensaje = "Contacto creado exitosamente."
            });
        }

        if (nombre == "actualizar_contacto") {
            var cid  = input.TryGetProperty("contactoId", out var id) ? id.GetInt32() : 0;
            var nomb = input.TryGetProperty("nombre",     out var n)  && n.ValueKind  == JsonValueKind.String ? n.GetString()  : null;
            var emp  = input.TryGetProperty("empresa",    out var e)  && e.ValueKind  == JsonValueKind.String ? e.GetString()  : null;
            var mail = input.TryGetProperty("email",      out var em) && em.ValueKind == JsonValueKind.String ? em.GetString() : null;
            var tel  = input.TryGetProperty("telefono",   out var tf) && tf.ValueKind == JsonValueKind.String ? tf.GetString() : null;

            var contacto = await this.crm.ActualizarContactoAsync(cid, nomb, emp, mail, tel);
            if (contacto is null)
                return JsonSerializer.Serialize(new { error = $"No se encontro contacto con ID {cid}." });

            this.ReferenciarContactos(new[] { contacto.Id });
            // Si el usuario esta viendo la ficha de ESTE contacto, refrescamos los
            // datos del contexto para que el encabezado del chat ("Viendo: ...")
            // muestre el nombre/empresa nuevos y no los viejos.
            if (this.filtro.ContactoActualId == contacto.Id)
                this.filtro.SetContactoActual(contacto.Id, contacto.Nombre, contacto.Empresa);
            this.filtro.SolicitarNavegacion(contacto.Id);

            return JsonSerializer.Serialize(new {
                id = contacto.Id, nombre = contacto.Nombre, empresa = contacto.Empresa,
                mensaje = "Contacto actualizado."
            });
        }

        if (nombre == "registrar_actividad") {
            var cid     = input.TryGetProperty("contactoId", out var id) ? id.GetInt32()   : 0;
            var tipo    = input.TryGetProperty("tipo",       out var ti) ? ti.GetString()  ?? "Nota" : "Nota";
            var detalle = input.TryGetProperty("detalle",    out var d)  ? d.GetString()   ?? "" : "";

            // Fecha opcional. El modelo la manda en ISO 8601; si no viene o no parsea,
            // la dejamos en null y el servicio usa la fecha y hora actual.
            DateTime? fecha = null;
            if (input.TryGetProperty("fecha", out var f) && f.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(f.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                fecha = parsed;

            var actividad = await this.crm.RegistrarActividadAsync(cid, tipo, detalle, fecha);
            this.ReferenciarContactos(new[] { cid });
            this.filtro.SolicitarNavegacion(cid);

            return JsonSerializer.Serialize(new {
                id = actividad.Id, tipo = actividad.Tipo,
                fecha = actividad.Fecha.ToString("dd/MM/yyyy HH:mm"),
                mensaje = "Actividad registrada."
            });
        }

        if (nombre == "buscar_oportunidades") {
            Etapa? etapa = null;
            if (input.TryGetProperty("etapa", out var e) &&
                e.ValueKind == JsonValueKind.String &&
                Enum.TryParse<Etapa>(e.GetString(), out var parsed))
                etapa = parsed;

            decimal? montoMinimo = null;
            if (input.TryGetProperty("montoMinimo", out var m) && m.ValueKind == JsonValueKind.Number)
                montoMinimo = m.GetDecimal();

            var resultados = await this.crm.BuscarOportunidadesAsync(etapa, montoMinimo);
            this.ReferenciarContactos(resultados.Select(o => o.ContactoId).Distinct());
            this.ReferenciarOportunidades(resultados.Select(o => o.Id));

            // Publicamos las oportunidades para que el panel las muestre como tarjetas
            // (bien presentadas) en vez de texto plano.
            if (resultados.Count > 0)
                this.filtro.SetOportunidadesMostradas(resultados.Select(o =>
                    new AgenteFiltroService.OportunidadResumen(
                        o.Id,
                        o.Contacto?.Empresa ?? o.Contacto?.Nombre ?? "?",
                        o.Descripcion, o.Monto, o.Etapa.ToString())));

            return JsonSerializer.Serialize(resultados.Select(o => new {
                id          = o.Id,
                contacto    = o.Contacto?.Empresa ?? o.Contacto?.Nombre ?? "?",
                descripcion = o.Descripcion,
                monto       = o.Monto,
                etapa       = o.Etapa.ToString()
            }));
        }

        if (nombre == "cambiar_etapa_oportunidad") {
            var oid = input.TryGetProperty("oportunidadId", out var id) ? id.GetInt32() : 0;

            if (!(input.TryGetProperty("etapa", out var e) && e.ValueKind == JsonValueKind.String &&
                  Enum.TryParse<Etapa>(e.GetString(), out var etapa)))
                return JsonSerializer.Serialize(new { error = "Etapa invalida." });

            var op = await this.crm.MoverEtapaAsync(oid, etapa);
            if (op is null)
                return JsonSerializer.Serialize(new { error = $"No se encontro oportunidad con ID {oid}." });

            this.ReferenciarContactos(new[] { op.ContactoId });
            this.ReferenciarOportunidades(new[] { op.Id });

            return JsonSerializer.Serialize(new {
                id = op.Id, etapa = op.Etapa.ToString(),
                mensaje = "Etapa de la oportunidad actualizada."
            });
        }

        if (nombre == "crear_oportunidad") {
            var cid  = input.TryGetProperty("contactoId",  out var id) ? id.GetInt32()    : 0;
            var desc = input.TryGetProperty("descripcion", out var d)  ? d.GetString() ?? "" : "";
            var mont = input.TryGetProperty("monto",        out var m) && m.ValueKind == JsonValueKind.Number ? m.GetDecimal() : 0m;

            var etapa = Etapa.Prospecto;
            if (input.TryGetProperty("etapa", out var et) && et.ValueKind == JsonValueKind.String &&
                Enum.TryParse<Etapa>(et.GetString(), out var parsedEtapa))
                etapa = parsedEtapa;

            var op = await this.crm.CrearOportunidadAsync(cid, desc, mont, etapa);
            this.ReferenciarContactos(new[] { op.ContactoId });
            this.ReferenciarOportunidades(new[] { op.Id });

            return JsonSerializer.Serialize(new {
                id = op.Id, descripcion = op.Descripcion, monto = op.Monto,
                etapa = op.Etapa.ToString(), mensaje = "Oportunidad creada."
            });
        }

        if (nombre == "eliminar_oportunidad") {
            var oid = input.TryGetProperty("oportunidadId", out var id) ? id.GetInt32() : 0;

            var op = await this.crm.EliminarOportunidadAsync(oid);
            if (op is null)
                return JsonSerializer.Serialize(new { error = $"No se encontro oportunidad con ID {oid}." });

            this.ReferenciarContactos(new[] { op.ContactoId });

            return JsonSerializer.Serialize(new {
                id = op.Id, descripcion = op.Descripcion,
                mensaje = "Oportunidad eliminada."
            });
        }

        return "{\"error\":\"herramienta desconocida\"}";
    }

    // ---- El loop principal del agente ----
    public async Task<string> PreguntarAsync(string pregunta) {
        if (!this.Configurado) {
            return "El agente no esta configurado. Falta definir OPENAI_API_KEY.";
        }

        // Cada vez que el usuario manda algo nuevo (o elige un candidato), los
        // candidatos pendientes de la vuelta anterior dejan de tener sentido.
        this.filtro.LimpiarCandidatos();
        this.filtro.LimpiarOportunidadesMostradas();

        // Reiniciamos los contactos y oportunidades referenciados: los filtros de
        // Contactos y Pipeline deben reflejar SOLO los de esta respuesta (los mismos
        // que el asistente muestra), no los acumulados de toda la conversacion.
        this.contactosReferidos.Clear();
        this.oportunidadesReferidas.Clear();

        // Arrancamos con el historial de la conversacion acumulada mas el nuevo mensaje.
        var input = new List<object>(this.historial) {
            new { role = "user", content = pregunta }
        };

        // Hasta 5 vueltas: el modelo puede encadenar varias herramientas.
        for (var vuelta = 0; vuelta < 5; vuelta++) {
            var respuesta = await this.LlamarApiAsync(input);

            using var doc = JsonDocument.Parse(respuesta);
            var root = doc.RootElement;

            if (!root.TryGetProperty("output", out var output)) {
                // Si vino un error de la API lo devolvemos para que se vea en pantalla.
                if (root.TryGetProperty("error", out var err)) {
                    return $"Error de la API: {err.GetRawText()}";
                }
                return "Respuesta inesperada del modelo.";
            }

            var pedidosDeTool = new List<(string callId, string nombre, JsonElement args)>();
            var textoFinal = new StringBuilder();

            foreach (var item in output.EnumerateArray()) {
                var tipo = item.GetProperty("type").GetString();

                if (tipo == "reasoning") {
                    // Los modelos de razonamiento emiten un item "reasoning" antes de cada
                    // function_call. La API exige que se incluya en el historial junto al
                    // function_call que lo sigue; si falta, devuelve invalid_request_error.
                    input.Add(JsonSerializer.Deserialize<object>(item.GetRawText())!);
                }
                else if (tipo == "message") {
                    // El modelo respondio con texto. Concatenamos los bloques output_text.
                    if (item.TryGetProperty("content", out var contenido)) {
                        foreach (var bloque in contenido.EnumerateArray()) {
                            if (bloque.GetProperty("type").GetString() == "output_text") {
                                textoFinal.Append(bloque.GetProperty("text").GetString());
                            }
                        }
                    }
                    // Mantenemos el mensaje completo en el historial.
                    input.Add(JsonSerializer.Deserialize<object>(item.GetRawText())!);
                }
                else if (tipo == "function_call") {
                    var callId  = item.GetProperty("call_id").GetString() ?? "";
                    var nombre  = item.GetProperty("name").GetString() ?? "";
                    var argsRaw = item.GetProperty("arguments").GetString() ?? "{}";

                    using var argsDoc = JsonDocument.Parse(argsRaw);
                    pedidosDeTool.Add((callId, nombre, argsDoc.RootElement.Clone()));

                    // El function_call vuelve al input tal cual para la siguiente vuelta.
                    input.Add(JsonSerializer.Deserialize<object>(item.GetRawText())!);
                }
            }

            // Si el modelo no pidio herramientas, ya tenemos la respuesta final.
            if (pedidosDeTool.Count == 0) {
                // Guardamos el estado completo de este turno para el proximo mensaje.
                this.historial.Clear();
                this.historial.AddRange(input);
                return textoFinal.ToString();
            }

            // Ejecutamos cada herramienta pedida y mandamos los resultados.
            foreach (var (callId, nombre, args) in pedidosDeTool) {
                var resultado = await this.EjecutarHerramientaAsync(nombre, args);
                input.Add(new {
                    type = "function_call_output",
                    call_id = callId,
                    output = resultado
                });
            }
        }

        return "El agente no pudo completar la consulta (demasiadas vueltas).";
    }

    private string ConstruirInstrucciones() {
        var sb = new System.Text.StringBuilder();
        sb.Append($"""
            Sos un asistente de CRM. Podes consultar y modificar datos: buscar contactos,
            crear contactos, actualizar sus datos, registrar actividades (llamadas, mails,
            reuniones, notas), buscar oportunidades de venta, crearlas, eliminarlas y
            cambiar su etapa en el pipeline.

            REGLAS para identificar contactos:
            - Si el usuario esta viendo la ficha de un contacto (indicado abajo), 
              USA ESE CONTACTO DIRECTAMENTE sin llamar a buscar_contactos.
            - Si NO hay contacto activo y el usuario menciona un nombre o empresa, 
              llama a buscar_contactos con ese texto.
            - Si buscar_contactos devuelve UN solo resultado, procede sin preguntar.
            - Si devuelve VARIOS resultados, NO los enumeres en el texto: el panel ya
              le muestra al usuario una lista clicable con los candidatos. Solo pedile
              en una frase corta que elija de la lista cual es el que busca.
            - Si no devuelve resultados, informalo y ofrecé crear el contacto.
            - NUNCA le pidas el ID al usuario. Vos lo obtenés con buscar_contactos.

            REGLAS para oportunidades:
            - Para CREAR una oportunidad necesitas el contactoId (usa el contacto
              activo o buscar_contactos), una descripcion y un monto. La etapa es
              opcional (por defecto Prospecto).
            - Para CAMBIAR LA ETAPA o ELIMINAR una oportunidad, primero usa
              buscar_oportunidades para ubicarla; devuelve el id, el contacto y la
              descripcion de cada una.
            - Si un mismo contacto tiene VARIAS oportunidades y no queda claro cual,
              preguntale al usuario cual (por descripcion o monto), no adivines.
            - Eliminar es PERMANENTE: asegurate de tener la oportunidad correcta y,
              ante cualquier duda, confirma con el usuario antes de borrar.
            - Las etapas validas son: Prospecto, Contactado, Propuesta, Negociacion,
              Ganada y Perdida.
            - Cuando listes oportunidades con buscar_oportunidades, NO las enumeres una
              por una en el texto: el panel ya las muestra como tarjetas. Da solo un
              resumen corto (cuantas hay y, si suma, el total).

            Responde siempre en español rioplatense, claro y conciso.
            Montos con separador de miles.
            """);

        var ahora = DateTime.Now;
        var cultura = new CultureInfo("es-AR");
        sb.Append(
            $"\n\nFECHA Y HORA ACTUAL: {ahora:yyyy-MM-ddTHH:mm} ({ahora.ToString("dddd d 'de' MMMM 'de' yyyy", cultura)}). " +
            "Usala para resolver referencias relativas a fechas (\"hoy\", \"ayer\", \"anteayer\", \"mañana\", " +
            "\"pasado mañana\", \"la semana pasada\", etc.) cuando el usuario indique cuando ocurrio una actividad. " +
            "Pasa la fecha resuelta en formato ISO 8601 al parametro 'fecha' de registrar_actividad. " +
            "Si el usuario no menciona ninguna fecha, omiti el parametro: se usara la fecha y hora actual.");

        if (this.filtro.ContactoActualId.HasValue) {
            sb.Append(
                $"\n\nCONTACTO ACTIVO: El usuario esta viendo la ficha de " +
                $"{this.filtro.ContactoActualNombre} " +
                $"(ID: {this.filtro.ContactoActualId}, Empresa: {this.filtro.ContactoActualEmpresa}). " +
                "Para cualquier accion sobre este contacto, usa este ID directamente.");
        }

        return sb.ToString();
    }

    // ---- Llamada HTTP cruda a la Responses API de OpenAI ----
    private async Task<string> LlamarApiAsync(List<object> input) {
        var cuerpo = new {
            model = Modelo,
            instructions = ConstruirInstrucciones(),
            tools = DefinirHerramientas(),
            input
        };

        var json = JsonSerializer.Serialize(cuerpo);
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        req.Headers.Add("Authorization", $"Bearer {this.apiKey}");
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await this.http.SendAsync(req);
        return await resp.Content.ReadAsStringAsync();
    }
}
