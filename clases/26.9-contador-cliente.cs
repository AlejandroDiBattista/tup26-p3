const string host = "http://localhost:5001";
using HttpClient client = new() { BaseAddress = new Uri(host) };

Console.Clear();
Console.WriteLine($"=== Conectando a {host} (C#) ===\n");
Console.WriteLine($"Estado inicial:  \n → {await LeerContador()}\n");
Console.WriteLine($"Incrementar 1:   \n → {await IncrementarContador()}\n");
Console.WriteLine($"Incrementar 2:   \n → {await IncrementarContador()}\n");
Console.WriteLine($"Estado Contador: \n → {await LeerContador()}\n");
Console.WriteLine($"Borrar contador: \n → {await BorrarContador()}\n");
Console.WriteLine($"Estado final:    \n → {await LeerContador()}\n\n");

async Task<string> LeerContador() =>
    await client.GetStringAsync("/contador");

async Task<bool> IncrementarContador() {
    HttpResponseMessage respuesta = await client.PostAsync("/contador", null);
    return respuesta.IsSuccessStatusCode;
}

async Task<bool> BorrarContador() {
    HttpResponseMessage respuesta = await client.DeleteAsync("/contador");
    return respuesta.IsSuccessStatusCode;
}
