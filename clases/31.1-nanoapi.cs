
Dictionary<Peticion, Controlador> nanoapi = [];

void RegistrarGet(string ruta, Controlador handler) {
    nanoapi[new Peticion("GET", ruta)] = handler;
}

void RegistrarPost(string ruta, Controlador handler) {
    nanoapi[new Peticion("POST", ruta)] = handler;
}

string EnviarGet(string ruta) {
    if (nanoapi.TryGetValue(new Peticion("GET", ruta), out var handler)) {
        return handler();
    } else {
        Console.Error.WriteLine($"No se encontró un handler para la ruta '{ruta}'.");
        return "Error: Ruta no encontrada.";
    }
}

string EnviarPost(string ruta) {
    if (nanoapi.TryGetValue(new Peticion("POST", ruta), out var handler)) {
        return handler();
    } else {
        Console.Error.WriteLine($"No se encontró un handler para la ruta '{ruta}'.");
        return "Error: Ruta no encontrada.";
    }
}

RegistrarGet("/hola",  () => "¡Hola, mundo!");
RegistrarGet("/adios", () => "¡Adiós, mundo!");
RegistrarPost("/enviar", () => "¡Mensaje enviado!");

Console.WriteLine(EnviarGet("/hola")); // Imprime: ¡Hola, mundo!
Console.WriteLine(EnviarGet("/adios")); // Imprime: ¡Adiós, mundo!
Console.WriteLine(EnviarPost("/enviar")); // Imprime: ¡Mensaje enviado!

record Peticion(string Metodo, string Ruta);
delegate string Controlador();