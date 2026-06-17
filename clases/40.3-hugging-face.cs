using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var token = Environment.GetEnvironmentVariable("HF_TOKEN");

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Falta la variable de entorno HF_TOKEN.");
    return;
}

// Modelo de generación de imágenes.
// FLUX.1-schnell está pensado para generar rápido.
var model = "black-forest-labs/FLUX.1-schnell";

// Proveedor dentro de Hugging Face Inference Providers.
// fal-ai suele servir modelos FLUX en Hugging Face.
var provider = "fal-ai";

var url = $"https://router.huggingface.co/{provider}/models/{model}";

var prompt = """
A small friendly robot drinking mate in Tucumán, Argentina,
warm morning light, cozy classroom, digital art, high detail
""";

var payload = new
{
    inputs = prompt,
    parameters = new
    {
        width = 512,
        height = 512,
        num_inference_steps = 4,
        guidance_scale = 3.5,
        seed = 12345
    }
};

using var http = new HttpClient();

http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

http.DefaultRequestHeaders.Accept.Clear();
http.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("image/png"));

var json = JsonSerializer.Serialize(payload);

using var content = new StringContent(
    json,
    Encoding.UTF8,
    "application/json");

Console.WriteLine("Generando imagen...");

using var response = await http.PostAsync(url, content);

var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

if (!response.IsSuccessStatusCode)
{
    var error = await response.Content.ReadAsStringAsync();

    Console.WriteLine($"Error HTTP {(int)response.StatusCode}");
    Console.WriteLine(error);
    return;
}

if (!contentType.StartsWith("image/"))
{
    var text = await response.Content.ReadAsStringAsync();

    Console.WriteLine("La respuesta no fue una imagen.");
    Console.WriteLine($"Content-Type: {contentType}");
    Console.WriteLine(text);
    return;
}

var imageBytes = await response.Content.ReadAsByteArrayAsync();

var outputPath = Path.Combine(
    Environment.CurrentDirectory,
    "imagen-generada.png");

await File.WriteAllBytesAsync(outputPath, imageBytes);

Console.WriteLine($"Imagen guardada en:");
Console.WriteLine(outputPath);