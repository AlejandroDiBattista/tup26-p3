namespace Calculadora;

class Program {
    static void Main(string[] args) {
        try {
            if (!Comandos.Procesar(args)) {
                ModoInteractivo();
            }
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
        }
    }

    static void ModoInteractivo() {
        Console.WriteLine("--- Modo Interactivo (escriba 'fin' para salir) ---");
        Console.Write("Ingrese expresión: ");
        string exp = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(exp) || exp == "fin") return;

        try {
            var ast = Compilador.Parse(exp);
            while (true) {
                Console.Write("Valor de x: ");
                string input = Console.ReadLine() ?? "";
                if (input == "fin" || input == "") break;
                if (int.TryParse(input, out int x)) Console.WriteLine($"Resultado: {ast.Evaluar(x)}");
                else Console.WriteLine("Valor inválido.");
            }
        } catch (Exception e) {
            Console.WriteLine(e.Message);
        }
    }
}
