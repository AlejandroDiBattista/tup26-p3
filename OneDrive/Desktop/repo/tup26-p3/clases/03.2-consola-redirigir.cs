
// Copio la entrada a la salida en mayusculas


List<string> lineas = new List<string>();
while(true) {
    
    string? entrada = Console.ReadLine();
    if (string.IsNullOrEmpty(entrada)) {
        break;
    }
    lineas.Add(entrada!);
}

Console.Clear();
Console.WriteLine("= SALIDA =");
foreach (var entrada in lineas) {   
    Console.WriteLine($"- {entrada.ToUpper()}");
}

