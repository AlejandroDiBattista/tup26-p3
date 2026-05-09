using System.Text.RegularExpressions;

namespace Calculadora;

public class Compilador {
    private readonly string[] tokens;
    private int pos = 0;

    public Compilador(string entrada) {
        tokens = Regex.Matches(entrada, @"([0-9]+|[xX]|[+\-*/()]|[^ \t])")
                      .Select(m => m.Value).ToArray();
    }

    public static Nodo Parse(string entrada) {
        return new Compilador(entrada).Procesar();
    }

    private string Peek() => pos < tokens.Length ? tokens[pos] : "";
    private string Eat() => tokens[pos++];

    public Nodo Procesar() {
        if (tokens.Length == 0) throw new Exception("Error: Entrada vacía.");
        var nodo = ParseExpresion();
        if (pos < tokens.Length) throw new Exception($"Error: Token inesperado '{Peek()}'");
        return nodo;
    }

    private Nodo ParseExpresion() {
        var nodo = ParseTermino();
        while (Peek() == "+" || Peek() == "-") {
            var op = Eat();
            var der = ParseTermino();
            nodo = op == "+" ? new SumaNodo(nodo, der) : new RestaNodo(nodo, der);
        }
        return nodo;
    }

    private Nodo ParseTermino() {
        var nodo = ParseFactor();
        while (Peek() == "*" || Peek() == "/") {
            var op = Eat();
            var der = ParseFactor();
            nodo = op == "*" ? new MultiplicacionNodo(nodo, der) : new DivisionNodo(nodo, der);
        }
        return nodo;
    }

    private Nodo ParseFactor() {
        var t = Peek();
        if (t == "+") { Eat(); return new PositivoNodo(ParseFactor()); }
        if (t == "-") { Eat(); return new NegativoNodo(ParseFactor()); }
        if (t == "(") {
            Eat();
            var nodo = ParseExpresion();
            if (Eat() != ")") throw new Exception("Error: Paréntesis sin cerrar.");
            return nodo;
        }
        if (int.TryParse(t, out int val)) { Eat(); return new NumeroNodo(val); }
        if (t.ToLower() == "x") { Eat(); return new VariableNodo(); }

        throw new Exception($"Error: Token inesperado '{t}'");
    }
}