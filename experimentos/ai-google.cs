// To run this code you need to install the following dependencies:
// dotnet add package Google.GenAI
#:package Google.GenAI@*
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.GenAI;
using Google.GenAI.Types;

public class Prueba {
    public static async Task Main(string[] args) {
        Console.Clear();
        Console.WriteLine("=== Prueba de Google GenAI (C#) ===\n");

        string? apiKey = System.Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
            ?? System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        var client = new Client(
            apiKey: apiKey
        );

        var model = "gemini-3.5-flash";
        var contents = new List<Content> {
            new Content {
                Role = "user",
                Parts = new List<Part> {
                    new Part { Text = "Deme function factorial en js, ruby, python, c#, c y rush (solo codigo)" },
                }
            },
        };

        var config = new GenerateContentConfig {
            ThinkingConfig = new ThinkingConfig {
                ThinkingLevel = "LOW"
            },
        };

        await foreach (var chunk in client.Models.GenerateContentStreamAsync(model, contents, config)) {
            Console.Write(chunk.Text);
        }

        Console.WriteLine("\n\n=== Fin de la respuesta ===");
    }
}


