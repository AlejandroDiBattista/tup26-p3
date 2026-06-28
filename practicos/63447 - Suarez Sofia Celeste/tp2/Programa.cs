using CalculadoraArimetica;
if (!Comandos.Procesar(args)) {

    Console.WriteLine("Modo Interactivo");
    Console.WriteLine("Ingresa una expresión con x o presiona Enter para salir:");

    string? entrada = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(entrada) || entrada.ToLower() == "fin")
        return;

    try {

        Nodo arbol = Compilador.Parse(entrada);

        while (true) {
            Console.Write("Ingresa el valor de x: ");
            string? valorTexto = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(valorTexto) || valorTexto.ToLower() == "fin")
                break;

            if (int.TryParse(valorTexto, out int x)) {
                int resultado = arbol.Evaluar(x);
                Console.WriteLine($"Resultado: {resultado}");
            } else {
                Console.WriteLine("Error: Por favor ingresa un número entero válido.");
            }
        }
    } catch (Exception ex) {
        Console.WriteLine($"Error de compilación: {ex.Message}");
    }
}
