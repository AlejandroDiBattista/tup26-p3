// DEMO: evaluar una expresión matemática usando un AST creado por un parser descendente recursivo.

// Tomamos una expresión matemática como cadena.
var e0 = "1 + 2 * (8 / 4)";

// La parseamos para construir un AST (árbol de sintaxis abstracta).
var e1 = Parse(e0);

// Usamos el AST para evaluar la expresión.
Console.WriteLine($"{e1} = {e1.Evaluar()}"); //> (1 + (2 * (8 / 4))) = 5

// Construimos la expresión manualmente, sin usar el parser, para mostrar que el AST es independiente de la sintaxis concreta.
var e2 = new Suma(
    new Numero(1),
    new Multiplicacion(
        new Numero(2),
        new Division(
            new Numero(8),
            new Numero(4)
        )
    )
);
Console.WriteLine($"{e2} = {e2.Evaluar()}");

// Con un poco de azúcar sintáctico, podemos construir el AST de forma más natural.
Nodo e3 = 1 + 2 * (8 / 4);
Console.WriteLine($"{e3} = {e3.Evaluar()}");

// Desarrollemos el parser.
//
// Primero definamos la gramática de nuestra calculadora.
//
// La gramática define las reglas con las que se combinan los símbolos
// para formar expresiones válidas. En este caso, queremos una calculadora
// que soporte números enteros, los operadores +, -, *, / y paréntesis
// para controlar el orden de las operaciones.
//
// La gramática podría escribirse así:
//     expresion := termino ('+' | '-' termino)*
//     termino   := factor ('*' | '/' factor)*
//     factor    := ('+' | '-') factor | '(' expresion ')' | numero

// Esta función toma una cadena de texto y devuelve el AST.
Nodo Parse(string s) {
    var pos = 0;

    // Devuelve el carácter actual o '\0' si llegamos al final de la cadena.
    char Current() => pos < s.Length ? s[pos] : '\0';

    // Devuelve el carácter actual y avanza la posición.
    char Consume() => s[pos++];

    // Match comprueba si el carácter actual coincide con el esperado.
    bool Match(char c) {
        while (char.IsWhiteSpace(Current())) {
            Consume();
        }
        return Current() == c;
    }

    // Parser descendente recursivo.
    Nodo Expresion() {
        // Primero parseamos un término.
        var izquierda = Termino();

        // Si encuentra un operador '+' o '-', lo consume y luego parsea el siguiente término.
        while (Match('+') || Match('-')) {
            var operador = Consume();
            var derecha = Termino();

            if (operador == '+') {
                izquierda = new Suma(izquierda, derecha);
            } else {
                izquierda = new Resta(izquierda, derecha);
            }
        }

        return izquierda;
    }

    Nodo Termino() {
        // Primero parseamos un factor.
        var izquierda = Factor();

        // Si encuentra un operador '*' o '/', lo consume y luego parsea el siguiente factor.
        while (Match('*') || Match('/')) {
            var operador = Consume();
            var derecha = Factor();

            if (operador == '*') {
                izquierda = new Multiplicacion(izquierda, derecha);
            } else {
                izquierda = new Division(izquierda, derecha);
            }
        }

        return izquierda;
    }

    Nodo Factor() {
        // Si encuentra un operador unario '+' o '-', lo consume y parsea el siguiente factor.
        if (Match('+') || Match('-')) {
            var signo = Consume();
            var nodo = Factor();
            return signo == '+' ? new Positivo(nodo) : new Negativo(nodo);
        }

        // Si encuentra un '(', consume el paréntesis, parsea la expresión interna y luego consume el ')'.
        if (Match('(')) {
            Consume();
            var nodo = Expresion();
            if (!Match(')')) throw new Exception("Se esperaba ')'");
            Consume();
            return nodo;
        }

        // Si encuentra un número, consume sus dígitos y construye un nodo numérico.
        if (char.IsDigit(Current())) {
            var valor = 0f;
            while (char.IsDigit(Current())) {
                valor = valor * 10 + (Consume() - '0');
            }
            return new Numero(valor);
        }

        throw new Exception($"Carácter inesperado: '{Current()}'");
    }

    return Expresion();
}

// AST: Abstract Syntax Tree (árbol de sintaxis abstracta).
// Nodo base de toda la jerarquía.
abstract class Nodo {
    public abstract float Evaluar();

    // Azúcar sintáctico para construir el AST de forma más natural.
    public static implicit operator Nodo(float valor) => new Numero(valor);
    public static Nodo operator +(Nodo a, Nodo b) => new Suma(a, b);
    public static Nodo operator -(Nodo a, Nodo b) => new Resta(a, b);
    public static Nodo operator *(Nodo a, Nodo b) => new Multiplicacion(a, b);
    public static Nodo operator /(Nodo a, Nodo b) => new Division(a, b);
    public static Nodo operator -(Nodo a) => new Negativo(a);
    public static Nodo operator +(Nodo a) => new Positivo(a);
}

// Comportamiento común para operadores unarios.
abstract class Unario(string operador, Nodo operando) : Nodo {
    protected string Operador { get; } = operador;
    protected Nodo Operando { get; } = operando;

    public override string ToString() => $"({Operador} {Operando})";
}

// Comportamiento común para operadores binarios.
abstract class Binario(string operador, Nodo izquierda, Nodo derecha) : Nodo {
    protected string Operador { get; } = operador;
    protected Nodo Izquierda { get; } = izquierda;
    protected Nodo Derecha { get; } = derecha;

    public override string ToString() => $"({Izquierda} {Operador} {Derecha})";
}

class Negativo(Nodo operando) : Unario("-", operando) {
    public override float Evaluar() => -Operando.Evaluar();
}

class Positivo(Nodo operando) : Unario("+", operando) {
    public override float Evaluar() => Operando.Evaluar();
}

class Numero(float valor) : Nodo {
    public override float Evaluar() => valor;
    public override string ToString() => valor.ToString();
}

class Suma(Nodo izquierda, Nodo derecha) : Binario("+", izquierda, derecha) {
    public override float Evaluar() => Izquierda.Evaluar() + Derecha.Evaluar();
}

class Resta(Nodo izquierda, Nodo derecha) : Binario("-", izquierda, derecha) {
    public override float Evaluar() => Izquierda.Evaluar() - Derecha.Evaluar();
}

class Multiplicacion(Nodo izquierda, Nodo derecha) : Binario("*", izquierda, derecha) {
    public override float Evaluar() => Izquierda.Evaluar() * Derecha.Evaluar();
}

class Division(Nodo izquierda, Nodo derecha) : Binario("/", izquierda, derecha) {
    public override float Evaluar() => Izquierda.Evaluar() / Derecha.Evaluar();
}
