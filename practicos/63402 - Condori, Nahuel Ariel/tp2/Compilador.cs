// Compilador.cs
using System;

public class Compilador {
    private readonly string entrada;
    private int pos;

    public static Nodo Compilar(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion)) throw new Exception("Error: Entrada vacía.");
        var parser = new Compilador(expresion);
        Nodo ast = parser.ParsearExpresion();
        if (parser.pos < parser.entrada.Length) throw new Exception("Error: Token inesperado al final de la expresión.");
        return ast;
    }

    private Compilador(string expresion) {
        entrada = expresion;
        pos = 0;
    }

    private void OmitirEspacios() {
        while (pos < entrada.Length && char.IsWhiteSpace(entrada[pos])) pos++;
    }

    private Nodo ParsearExpresion() {
        Nodo nodo = ParsearTermino();
        while (true) {
            OmitirEspacios();
            if (pos < entrada.Length && entrada[pos] == '+') {
                pos++;
                nodo = new SumaNodo(nodo, ParsearTermino());
            } else if (pos < entrada.Length && entrada[pos] == '-') {
                pos++;
                nodo = new RestaNodo(nodo, ParsearTermino());
            } else {
                break;
            }
        }
        return nodo;
    }

    private Nodo ParsearTermino() {
        Nodo nodo = ParsearFactor();
        while (true) {
            OmitirEspacios();
            if (pos < entrada.Length && entrada[pos] == '*') {
                pos++;
                nodo = new MultiplicacionNodo(nodo, ParsearFactor());
            } else if (pos < entrada.Length && entrada[pos] == '/') {
                pos++;
                nodo = new DivisionNodo(nodo, ParsearFactor());
            } else {
                break;
            }
        }
        return nodo;
    }

    private Nodo ParsearFactor() {
        OmitirEspacios();
        if (pos >= entrada.Length) throw new Exception("Error: Expresión incompleta o mal formada.");

        char actual = entrada[pos];

        if (actual == '+') {
            pos++;
            return new PositivoNodo(ParsearFactor());
        }
        if (actual == '-') {
            pos++;
            return new NegativoNodo(ParsearFactor());
        }
        if (actual == '(') {
            pos++;
            Nodo nodo = ParsearExpresion();
            OmitirEspacios();
            if (pos >= entrada.Length || entrada[pos] != ')') throw new Exception("Error: Paréntesis sin cerrar.");
            pos++; // Consumir ')'
            return nodo;
        }
        if (actual == 'x' || actual == 'X') {
            pos++;
            return new VariableNodo();
        }
        if (char.IsDigit(actual)) {
            int inicio = pos;
            while (pos < entrada.Length && char.IsDigit(entrada[pos])) pos++;
            int valor = int.Parse(entrada.Substring(inicio, pos - inicio));
            return new NumeroNodo(valor);
        }

        throw new Exception($"Error: Token inesperado '{actual}' en la posición {pos}.");
    }
}
