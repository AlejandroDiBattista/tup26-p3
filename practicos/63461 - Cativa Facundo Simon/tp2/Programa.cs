using System;

class Programa {
    static void Main(string[] args) {
        try {
            // si hay comandos (help, test, directo)
            if (Comandos.Procesar(args))
                return;

            // modo interactivo
            Console.WriteLine("Ingrese una expresion con x:");
            string expresion = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(expresion))
                return;

            var funcion = Compilador.Parse(expresion);

            while (true) {
                Console.Write("Ingrese valor de x (o fin): ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "fin")
                    break;

                int x = int.Parse(input);
                int resultado = funcion.Evaluar(x);

                Console.WriteLine($"Resultado: {resultado}");
            }
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
