using System;

public abstract class Nodo
{
    public abstract int Evaluar(int x);
}

public class NumeroNodo : Nodo
{
    public int Valor { get; }
    public NumeroNodo(int valor) => Valor = valor;
    public override int Evaluar(int x) => Valor;
}

public class VariableNodo : Nodo
{
    public override int Evaluar(int x) => x;
}

public class PositivoNodo : Nodo
{
    public Nodo Operando { get; }
    public PositivoNodo(Nodo operando) => Operando = operando;
    public override int Evaluar(int x) => Operando.Evaluar(x);
}

public class NegativoNodo : Nodo
{
    public Nodo Operando { get; }
    public NegativoNodo(Nodo operando) => Operando = operando;
    public override int Evaluar(int x) => -Operando.Evaluar(x);
}

public abstract class NodoBinario : Nodo
{
    public Nodo Izquierdo { get; }
    public Nodo Derecho { get; }

    protected NodoBinario(Nodo izquierdo, Nodo derecho)
    {
        Izquierdo = izquierdo;
        Derecho = derecho;
    }
}

public class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
}

public class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
}

public class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
}

public class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x)
    {
        int divisor = Derecho.Evaluar(x);
        if (divisor == 0)
            throw new DivideByZeroException("Error: División por cero.");
        return Izquierdo.Evaluar(x) / divisor;
    }
}
public class Compilador
{
    private readonly string _input;
    private int _pos;

    public Compilador(string input)
    {
        // Eliminamos espacios para simplificar el parseo
        _input = input.Replace(" ", "");
        _pos = 0;
    }

    public Nodo Parsear()
    {
        if (string.IsNullOrWhiteSpace(_input))
            throw new Exception("Error: Entrada vacía.");

        Nodo resultado = ParsearExpresion();

        if (_pos < _input.Length)
            throw new Exception($"Error: Token inesperado en la posición {_pos} ('{_input[_pos]}').");

        return resultado;
    }

    // Expresion := Termino { ('+' | '-') Termino }
    private Nodo ParsearExpresion()
    {
        Nodo nodo = ParsearTermino();

        while (_pos < _input.Length && (_input[_pos] == '+' || _input[_pos] == '-'))
        {
            char operador = _input[_pos];
            _pos++;
            Nodo derecho = ParsearTermino();

            if (operador == '+')
                nodo = new SumaNodo(nodo, derecho);
            else
                nodo = new RestaNodo(nodo, derecho);
        }

        return nodo;
    }

    // Termino := Factor { ('*' | '/') Factor }
    private Nodo ParsearTermino()
    {
        Nodo nodo = ParsearFactor();

        while (_pos < _input.Length && (_input[_pos] == '*' || _input[_pos] == '/'))
        {
            char operador = _input[_pos];
            _pos++;
            Nodo derecho = ParsearFactor();

            if (operador == '*')
                nodo = new MultiplicacionNodo(nodo, derecho);
            else
                nodo = new DivisionNodo(nodo, derecho);
        }

        return nodo;
    }

    // Factor := '+' Factor | '-' Factor | '(' Expresion ')' | numero | x
    private Nodo ParsearFactor()
    {
        if (_pos >= _input.Length)
            throw new Exception("Error: Se esperaba un factor, pero se encontró el final de la expresión.");

        char actual = _input[_pos];

        // Operadores Unarios
        if (actual == '+')
        {
            _pos++;
            return new PositivoNodo(ParsearFactor());
        }
        if (actual == '-')
        {
            _pos++;
            return new NegativoNodo(ParsearFactor());
        }

        // Paréntesis
        if (actual == '(')
        {
            _pos++;
            Nodo nodo = ParsearExpresion();
            if (_pos >= _input.Length || _input[_pos] != ')')
                throw new Exception("Error: Paréntesis sin cerrar.");
            _pos++; // Consumir ')'
            return nodo;
        }

        // Variable
        if (char.ToLower(actual) == 'x')
        {
            _pos++;
            return new VariableNodo();
        }

        // Números
        if (char.IsDigit(actual))
        {
            int inicio = _pos;
            while (_pos < _input.Length && char.IsDigit(_input[_pos]))
                _pos++;
            
            int valor = int.Parse(_input.Substring(inicio, _pos - inicio));
            return new NumeroNodo(valor);
        }

        throw new Exception($"Error: Token inesperado '{actual}' en la posición {_pos}.");
    }
}
public static class Pruebas
{
    public static void Ejecutar()
    {
        Console.WriteLine("Ejecutando pruebas automáticas...\n");

        int exitos = 0;
        int fallos = 0;

        EvaluarPrueba("1 + 2 * 3", 0, 7, ref exitos, ref fallos);
        EvaluarPrueba("1 + 2 * x", 10, 21, ref exitos, ref fallos);
        EvaluarPrueba("(x - 1) * (x - 8 / 4) + 3", 10, 75, ref exitos, ref fallos);
        EvaluarPrueba("-(3 + 2)", 0, -5, ref exitos, ref fallos);
        EvaluarPrueba("10 / 2", 0, 5, ref exitos, ref fallos);

        // Prueba de error (Paréntesis sin cerrar)
        try
        {
            new Compilador("(1 + 2").Parsear();
            Console.WriteLine("[FALLO] (1 + 2 con x=0 -> Se esperaba Error de análisis, pero no falló.");
            fallos++;
        }
        catch (Exception)
        {
            Console.WriteLine("[EXITO] (1 + 2 con x=0 -> Error de análisis capturado correctamente.");
            exitos++;
        }

        Console.WriteLine($"\nResultados: {exitos} exitosas, {fallos} fallidas.");
        Environment.Exit(fallos == 0 ? 0 : 1);
    }

    private static void EvaluarPrueba(string expresion, int x, int esperado, ref int exitos, ref int fallos)
    {
        try
        {
            Compilador compilador = new Compilador(expresion);
            Nodo ast = compilador.Parsear();
            int resultado = ast.Evaluar(x);

            if (resultado == esperado)
            {
                Console.WriteLine($"[EXITO] {expresion} con x={x} -> {resultado}");
                exitos++;
            }
            else
            {
                Console.WriteLine($"[FALLO] {expresion} con x={x} -> Se esperaba {esperado}, se obtuvo {resultado}");
                fallos++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FALLO] {expresion} con x={x} -> Excepción inesperada: {ex.Message}");
            fallos++;
        }
    }
}
public static class Comandos
{
    public static void Procesar(string[] args)
    {
        if (args.Length == 0)
        {
            IniciarModoInteractivo();
            return;
        }

        string arg1 = args[0].ToLower();

        if (arg1 == "--help" || arg1 == "-h")
        {
            MostrarAyuda();
            Environment.Exit(0);
        }

        if (arg1 == "--test" || arg1 == "-t" || arg1 == "-p")
        {
            Pruebas.Ejecutar();
            return;
        }

        if (args.Length == 2)
        {
            EjecutarModoDirecto(args[0], args[1]);
            return;
        }

        Console.WriteLine("Error: Argumentos inválidos. Usa --help para más información.");
        Environment.Exit(1);
    }

    private static void MostrarAyuda()
    {
        Console.WriteLine("Calculadora AST");
        Console.WriteLine("Uso:");
        Console.WriteLine("  calculadora [expresion valor]");
        Console.WriteLine("  calculadora --help | -h");
        Console.WriteLine("  calculadora --test | -t");
        Console.WriteLine("\nModo directo:");
        Console.WriteLine("  Evalúa una expresión reemplazando 'x' por el valor indicado.");
        Console.WriteLine("  Ejemplo: dotnet run -- \"(x - 1) * 2\" 10");
        Console.WriteLine("\nModo interactivo:");
        Console.WriteLine("  Ejecuta sin argumentos. Permite ingresar una expresión y evaluarla múltiples veces.");
    }

    private static void EjecutarModoDirecto(string expresion, string valorStr)
    {
        try
        {
            if (!int.TryParse(valorStr, out int x))
                throw new Exception("Valor de x inválido. Debe ser un número entero.");

            Compilador compilador = new Compilador(expresion);
            Nodo ast = compilador.Parsear();
            int resultado = ast.Evaluar(x);
            Console.WriteLine(resultado);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Environment.Exit(1);
        }
    }

    private static void IniciarModoInteractivo()
    {
        Console.WriteLine("--- MODO INTERACTIVO ---");
        Console.Write("Ingrese la expresión matemática: ");
        string expresion = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(expresion))
        {
            Console.WriteLine("Entrada vacía. Finalizando.");
            return;
        }

        try
        {
            Compilador compilador = new Compilador(expresion);
            Nodo ast = compilador.Parsear(); // Compila una sola vez
            Console.WriteLine("Expresión compilada con éxito. Ingrese valores para 'x' (o 'fin' para salir).");

            while (true)
            {
                Console.Write("x = ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "fin")
                    break;

                if (int.TryParse(input, out int x))
                {
                    try
                    {
                        Console.WriteLine($"Resultado: {ast.Evaluar(x)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error de evaluación: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Valor inválido. Ingrese un entero o 'fin'.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
class Programa
{
    static void Main(string[] args)
    {
        Comandos.Procesar(args);
    }
}