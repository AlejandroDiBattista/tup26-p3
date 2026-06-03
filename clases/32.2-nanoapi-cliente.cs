using System.Diagnostics;

using var srv = Servidor.Run();

Console.WriteLine(srv.Send("GET /hola").Body);
Console.WriteLine(srv.Send("GET /chau").Body);
Console.WriteLine(srv.Send("POST /enviar").Body);
Console.WriteLine(srv.SendJson("POST", "/echo-json", $$"""{"mensaje":"hola"}""").Body);

Console.WriteLine(srv.Send("GET /contador").Body);
Console.WriteLine(srv.Send("POST /contador").Body);
Console.WriteLine(srv.Send("POST /contador").Body);
Console.WriteLine(srv.Send("POST /contador").Body);
Console.WriteLine(srv.Send("GET /contador").Body);
Console.WriteLine(srv.Send("PUT /contador").Body);
Console.WriteLine(srv.Send("GET /contador").Body);
Console.WriteLine(srv.Send("DELETE /contador").Body);
Console.WriteLine(srv.Send("GET /contador").Body);
// ---

class Servidor : IDisposable {
    private Process servidor { get; init;}   

    public static Servidor Run() => new Servidor();    

    public Servidor() {
        var startInfo = new ProcessStartInfo { FileName = "dotnet", RedirectStandardInput  = true, RedirectStandardOutput = true, };
        startInfo.ArgumentList.Add("31.2-nanoapi-servidor.cs");
        this.servidor = Process.Start(startInfo)!;
    }

    public HttpResponse Send(string peticion) {
        Console.WriteLine($"\nCLIENTE -> SERVIDOR: {peticion}");
        servidor.StandardInput.WriteLine(peticion);
        servidor.StandardInput.Flush();

        var statusLine = LeerStatusLine();
        var headers = LeerHeaders();
        var body = servidor.StandardOutput.ReadLine() ?? string.Empty;

        return HttpResponse.Parse(statusLine, headers, body);
    }

    public HttpResponse SendJson(string metodo, string ruta, string json) {
        return Send($"{metodo} {ruta} {json}");
    }

    private string LeerStatusLine() {
        string? linea;
        while ((linea = servidor.StandardOutput.ReadLine()) is not null) {
            if (linea.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase)) {
                return linea;
            }
        }

        throw new FormatException("Respuesta invalida: no se recibio una linea de estado HTTP.");
    }

    private Dictionary<string, string> LeerHeaders() {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? linea;
        while ((linea = servidor.StandardOutput.ReadLine()) is not null) {
            if (linea.Length == 0) {
                return headers;
            }

            var separador = linea.IndexOf(':');
            if (separador <= 0) {
                throw new FormatException($"Header invalido: '{linea}'.");
            }

            var clave = linea[..separador].Trim();
            var valor = linea[(separador + 1)..].Trim();
            headers[clave] = valor;
        }

        throw new FormatException("Respuesta invalida: faltaba el separador entre headers y body.");
    }

    public void Dispose() {
        servidor.StandardInput.Close();
        servidor.WaitForExit();
    }
}

record HttpResponse(int StatusCode, Dictionary<string, string>? headers, string Body){
    public static HttpResponse Parse(string statusLine, Dictionary<string, string> headers, string body) {
        var partes = statusLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length < 3) { throw new FormatException("Respuesta invalida."); }

        return new HttpResponse(int.Parse(partes[1]), headers, body);
    }   
};

