using System;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

//Clase base de todos los nodos de la calculadora
abstract class Node {
    public abstract int Evaluar(int x);
}
//Nodo que Representa un numero entero
class Numero : Node {
    public int valor;
    public Numero(int valor) {
        this.valor = valor;
    }
    public override int Evaluar(int x) {
        return valor; //simpemente devuelve el valor del número
    }

}
//Nodo que representa la variable x
class variable : Node {
    public override int Evaluar(int x) {
        return x; //Devuelve el valor de la variable x
    }
}

//Nodo que representa un unario negativo (-x)
class Negativo : Node {
    private Node NodeInterno;
    public Negativo(Node Node) {
        NodeInterno = Node; //Almacena el nodo interno que se va a evaluar
    }
    public override int Evaluar(int x) {
        return -NodeInterno.Evaluar(x); //Devuelve el negativo del valor del nodo interno
    }
}
// Clase base para operaciones binarias (dos operandos)
abstract class NodoBinario : Node {
    protected Node Izq; // lado izquierdo
    protected Node Der; // lado derecho

    public NodoBinario(Node i, Node d) {
        Izq = i;
        Der = d;
    }
}

//suma
class Suma : NodoBinario {
    public Suma(Node i, Node d) : base(i, d) { }
    public override int Evaluar(int x) {
        return Izq.Evaluar(x) + Der.Evaluar(x); //Devuelve la suma de los valores de los nodos izquierdo y derecho
    }
}

//Resta
class Resta : NodoBinario {
    public Resta(Node i, Node d) : base(i, d) { }
    public override int Evaluar(int x) {
        return Izq.Evaluar(x) - Der.Evaluar(x); //Devuelve la resta de los valores de los nodos izquierdo y derecho
    }
}

//Multiplicacion
class Multiplicacion : NodoBinario {
    public Multiplicacion(Node i, Node d) : base(i, d) { }

    public override int Evaluar(int x) {
        return Izq.Evaluar(x) * Der.Evaluar(x); //Devuelve la multiplicacion de los valores de los nodos izquierdo y derecho
    }


}

//Division
class Division : NodoBinario {
    public Division(Node i, Node d) : base(i, d) { }
    public override int Evaluar(int x) {
        int division = Der.Evaluar(x); //Evalua el nodo derecho para obtener el divisor
        if (division == 0) {
            throw new DivideByZeroException("No se puede dividir por cero"); //Lanza una excepción si el divisor es cero
        }
        return Izq.Evaluar(x) / division; //Devuelve la división de los valores de los nodos izquierdo y derecho

    }
}

//Clase principal para probar la calculadora
class Compilador {
    private string input; //Expresion sin espacios
    private int pos; //Posicion actual en la cadena de entrada

    //Metodo Principal de parseo, recibe una expresion y devuelve el nodo raiz del arbol de expresion
    public Node Parsear(string texto) {
        input = texto.Replace(" ", ""); //Elimina los espacios de la cadena de entrada
        pos = 0; //Inicializa la posición en cero
        if (string.IsNullOrEmpty(input))
            throw new Exception("Entrada vacía");

        Node nodo = Expresion(); // empieza desde la regla más alta

        // Si no consumió todo, hay error
        if (pos < input.Length)
            throw new Exception("Token inesperado");

        return nodo;
    }

    // Expresion := Termino { (+|-) Termino
    private Node Expresion() {
        Node nodo = Termino();

        while (pos < input.Length &&
              (input[pos] == '+' || input[pos] == '-')) {
            char op = input[pos++];
            Node der = Termino();

            // Construye nodo binario según operador
            nodo = op == '+'
                ? new Suma(nodo, der)
                : new Resta(nodo, der);
        }

        return nodo;
    }

    // Termino := Factor { (*|/) Factor }
    private Node Termino() {
        Node nodo = Factor();

        while (pos < input.Length &&
              (input[pos] == '*' || input[pos] == '/')) {
            char op = input[pos++];
            Node der = Factor();

            nodo = op == '*'
                ? new Multiplicacion(nodo, der)
                : new Division(nodo, der);
        }

        return nodo;
    }
    // Factor := +Factor | -Factor | (Expresion) | numero | x
    private Node Factor() {
        if (pos >= input.Length)
            throw new Exception("Token inesperado");

        char c = input[pos];

        // Operador unario +
        if (c == '+') {
            pos++;
            return Factor();
        }

        // Operador unario -
        if (c == '-') {
            pos++;
            return new Negativo(Factor());
        }

        // Paréntesis
        if (c == '(') {
            pos++;
            Node nodo = Expresion();

            if (pos >= input.Length || input[pos] != ')')
                throw new Exception("Paréntesis sin cerrar");

            pos++;
            return nodo;
        }

        // Número
        if (char.IsDigit(c)) {
            int inicio = pos;

            while (pos < input.Length && char.IsDigit(input[pos]))
                pos++;

            int valor = int.Parse(input.Substring(inicio, pos - inicio));
            return new Numero(valor);
        }
        // Variable x
        if (c == 'x' || c == 'X') {
            pos++;
            return new variable();
        }

        throw new Exception("Token inesperado");
    }
}

#endregion

#region COMANDOS

class Comandos {
    // Detecta ayuda
    public static bool EsHelp(string[] args) {
        return args.Contains("--help") || args.Contains("-h");
    }

    // Detecta tests
    public static bool EsTest(string[] args) {
        return args.Contains("--test") || args.Contains("-t") || args.Contains("-p");
    }
}

#endregion

#region PRUEBAS

class Pruebas {
    public static void Ejecutar() {
        var comp = new Compilador();

        // Casos de prueba
        Test(comp, "1 + 2 * 3", 0, 7);
        Test(comp, "1 + 2 * x", 10, 21);
        Test(comp, "(x - 1) * (x - 8 / 4) + 3", 10, 75);
        Test(comp, "-(3 + 2)", 0, -5);
        Test(comp, "10 / 2", 0, 5);

        Console.WriteLine("✔ Todas las pruebas pasaron");
    }

    static void Test(Compilador comp, string expr, int x, int esperado) {
        var nodo = comp.Parsear(expr);
        int resultado = nodo.Evaluar(x);

        if (resultado != esperado)
            throw new Exception($"Error en {expr}");
    }
}

#endregion

#region PROGRAMA PRINCIPAL

class Program {
    static void Main(string[] args) {
        // Si pide ayuda
        if (Comandos.EsHelp(args)) {
            MostrarAyuda();
            return;
        }

        // Si pide tests
        if (Comandos.EsTest(args)) {
            Pruebas.Ejecutar();
            return;
        }

        var compilador = new Compilador();

        try {
            // 🔹 MODO DIRECTO
            if (args.Length == 2) {
                string expr = args[0];
                int x = int.Parse(args[1]);

                var nodo = compilador.Parsear(expr);
                Console.WriteLine(nodo.Evaluar(x));
            }
            // 🔹 MODO INTERACTIVO
            else {
                Console.Write("Expresión: ");
                string expr = Console.ReadLine();

                var nodo = compilador.Parsear(expr);

                while (true) {
                    Console.Write("x = ");
                    string input = Console.ReadLine();

                    // salir si escribe fin o vacío
                    if (string.IsNullOrEmpty(input) || input.ToLower() == "fin")
                        break;

                    int x = int.Parse(input);
                    Console.WriteLine(nodo.Evaluar(x));
                }
            }
        } catch (Exception ex) {
            // Manejo de errores
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    static void MostrarAyuda() {
        Console.WriteLine("Uso:");
        Console.WriteLine("calculadora \"expresion\" valor");
        Console.WriteLine("calculadora --help");
        Console.WriteLine("calculadora --test");
    }
}
