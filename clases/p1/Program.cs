using System.Globalization;

const string ExitCommand = "salir";

var historyPath = HistoryStore.GetDefaultPath();
var historyStore = new HistoryStore(historyPath);
var evaluator = new ExpressionEvaluator();

Console.WriteLine("Calculadora de expresiones aritmeticas");
Console.WriteLine($"Historial en archivo: {historyPath}");
Console.WriteLine("Ingresa una expresion con +, -, *, / y parentesis.");
Console.WriteLine("Tambien podes usar decimales con ',' o '.'.");
Console.WriteLine($"Escribi '{ExitCommand}' o deja la linea vacia para terminar.");

var recentEntries = historyStore.ReadRecent(5);
if (recentEntries.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Ultimas operaciones:");

    foreach (var entry in recentEntries)
    {
        Console.WriteLine($"- {entry}");
    }
}

while (true)
{
    Console.WriteLine();
    Console.Write("Expresion> ");

    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) ||
        input.Trim().Equals(ExitCommand, StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        var result = evaluator.Evaluate(input);
        Console.WriteLine($"Resultado: {result.ToString(CultureInfo.CurrentCulture)}");
        historyStore.Append(input, result);
    }
    catch (ExpressionException ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

Console.WriteLine("Hasta luego.");
