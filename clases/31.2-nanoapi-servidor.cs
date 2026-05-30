var app = new WebApi();

app.Use(Logging);

app.Get("/hola",    _ => "¡Hola, mundo!");
app.Get("/chau",    _ => "¡Chau, mundo!").Use(ByNanoApi);
app.Get("/json",    _ => "¡Hola, texto plano!");
app.Post("/enviar", _ => "¡Mensaje enviado!");
app.Post("/echo-json", ctx => {
    if (ctx.Request.Body is null) {
        return Response.BadRequest("Error: falta body JSON.");
    }

    if (!ctx.Request.IsJson()) {
        return Response.BadRequest("Error: se esperaba Content-Type application/json.");
    }

    return Response.Json(ctx.Request.Body);
});

var contador = 0;
app.Get("/contador", _ => $"Contador: {contador}").Use(ContadorHeader);
app.Post("/contador", _ => $"Contador: {++contador}");
app.Put("/contador", _ => {
    contador = 100;
    return $"Contador: {contador}";
});

app.Use(ContadorHeader);
app.Delete("/contador", _ => {
    contador = 0;
    return $"Contador reiniciado.";
});

app.Run();


Response Logging(Context ctx, Func<Context, Response> next) {
    Console.WriteLine($"[Middleware] {ctx.Request.Metodo} {ctx.Request.Ruta}");
    return next(ctx);
}

Response ByNanoApi(Context ctx, Func<Context, Response> next) {
    var response = next(ctx);
    return response.AddHeader("X-Powered-By", "nanoAPI");
}

Response ContadorHeader(Context ctx, Func<Context, Response> next) {
    var response = next(ctx);
    return response.AddHeader("X-Contador", "true");
}

record Request(string Metodo, string Ruta, Dictionary<string, string> headers, string? Body) {
    public static Request Parse(string linea) {
        var partes = linea.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length < 2) { throw new FormatException("Peticion invalida."); }

        var headers = new Dictionary<string, string>();
        var body = partes.Length == 3 ? partes[2] : null;

        if (body is not null) {
            headers["Content-Type"] = "application/json";
        }

        return new Request(partes[0], partes[1], headers, body);
    }

    public bool IsJson() {
        return headers.TryGetValue("Content-Type", out var contentType)
            && string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase);
    }
};

record class Response(int StatusCode, Dictionary<string, string>? headers, string Body) {
    public string Serialize() {
        var statusText = StatusCode switch {
            200 => "OK",
            400 => "Bad Request",
            404 => "Not Found",
            _   => "Unknown"
        };

        var headersSerialized = string.Join("\r\n", headers?.Select(h => $"{h.Key}: {h.Value}") ?? Enumerable.Empty<string>());
        var headersBlock = headersSerialized.Length == 0
            ? string.Empty
            : $"{headersSerialized}\r\n";

        return $"HTTP/1.1 {StatusCode} {statusText}\r\n{headersBlock}\r\n{Body}";
    }
    public static Response Ok(string body) => new Response(200, [], body);
    public static Response NotFound(string body) => new Response(404, [], body);
    public static Response BadRequest(string body) => new Response(400, new Dictionary<string, string>(), body);
    public static Response Json(string body) => new Response(200, new Dictionary<string, string> {
        ["Content-Type"] = "application/json"
    }, body);

    public Response AddHeader(string key, string value) {
        var nuevosHeaders = headers is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(headers);

        nuevosHeaders[key] = value;
        return this with { headers = nuevosHeaders };
    }

    public Response WithBody(string body) {
        return this with { Body = body };
    }
}

record Context(Request Request);

record HttpRequest(string Metodo, string Ruta);
delegate object ActionController(Context ctx);

delegate Response Middleware(Context ctx, Func<Context, Response> next);

class WebApi {
    private readonly Dictionary<HttpRequest, Endpoint> routes = [];
    private readonly List<Middleware> middlewares = [];

    public Endpoint Get(string ruta, ActionController handler) {
        return Map("GET", ruta, handler);
    }

    public Endpoint Post(string ruta, ActionController handler) {
        return Map("POST", ruta, handler);
    }

    public Endpoint Put(string ruta, ActionController handler) {
        return Map("PUT", ruta, handler);
    }

    public Endpoint Delete(string ruta, ActionController handler) {
        return Map("DELETE", ruta, handler);
    }

    public WebApi Use(Middleware middleware) {
        middlewares.Add(middleware);
        return this;
    }

    public void Run() {
        var app = BuildPipeline();
        var linea = "";

        Console.WriteLine("Servidor nanoAPI iniciado. Escribe una petición (ej: 'GET /hola'):");
        while ((linea = Console.ReadLine()) != null) {
            var req = Request.Parse(linea);
            Console.WriteLine(app(req).Serialize());
        }
        Console.WriteLine("Servidor nanoAPI finalizado.");
    }

    private Endpoint Map(string metodo, string ruta, ActionController handler) {
        var endpoint = new Endpoint(handler, middlewares);
        routes[new HttpRequest(metodo, ruta)] = endpoint;
        return endpoint;
    }

    private Func<Request, Response> BuildPipeline() {
        return req => RouteRequest(new Context(req));
    }

    private Response RouteRequest(Context ctx) {
        var url = new HttpRequest(ctx.Request.Metodo, ctx.Request.Ruta);

        if (routes.TryGetValue(url, out var endpoint)) {
            return endpoint.App(ctx);
        }

        return Response.NotFound("Error: Ruta no encontrada.");
    }
}


class Endpoint {
    private readonly ActionController handler;
    private readonly List<Middleware> globalMiddlewares;
    private readonly List<Middleware> endpointMiddlewares = [];

    public Func<Context, Response> App { get; private set; }

    public Endpoint(ActionController handler, IEnumerable<Middleware> globalMiddlewares) {
        this.handler = handler;
        this.globalMiddlewares = globalMiddlewares.ToList();
        App = BuildApp();
    }

    public Endpoint Use(Middleware middleware) {
        endpointMiddlewares.Add(middleware);
        App = BuildApp();
        return this;
    }

    private Func<Context, Response> BuildApp() {
        Func<Context, Response> app = InvokeHandler;
        var allMiddlewares = globalMiddlewares.Concat(endpointMiddlewares).ToList();

        for (var i = allMiddlewares.Count - 1; i >= 0; i--) {
            var next = app;
            var middleware = allMiddlewares[i];
            app = ctx => middleware(ctx, next);
        }

        return app;
    }

    private Response InvokeHandler(Context ctx) {
        var result = handler(ctx);
        if (result is string str) {
            return Response.Ok(str);
        }

        if (result is Response res) {
            return res;
        }

        return Response.Ok(result?.ToString() ?? string.Empty);
    }
}