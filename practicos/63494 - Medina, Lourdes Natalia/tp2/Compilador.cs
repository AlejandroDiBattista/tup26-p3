using System;

class Compilador {
   private static string texto = "";
    private static int pos;

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new FormatException("Expresion vacia");

        texto = expresion;
        pos = 0;

        var nodo = ParseExpresion();

        SaltarEspacios();

        if (pos < texto.Length)
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private static char Actual => 
        pos < texto.Length ? texto[pos] : '\0';

    private static void Avanzar() => pos++;

    private static void SaltarEspacios() {
        while (char.IsWhiteSpace(Actual))
            Avanzar();

    }

    private static Nodo ParseExpresion() {
        var nodo = ParseTermino();

        while (true) {
            SaltarEspacios();

            if (Actual == '+') {
                Avanzar();
                nodo = new SumaNodo(nodo, ParseTermino());
            } 
            else if (Actual == '-') {
                Avanzar();
                nodo = new RestaNodo(nodo, ParseTermino());
            } 
            else {
                break;
            }
        }

        return nodo;
    }
   
    private static Nodo ParseTermino() {
        var nodo = ParseFactor();

        while (true) {
            SaltarEspacios();

            if (Actual == '*') {
                Avanzar();
                nodo = new MultiplicacionNodo(nodo, ParseFactor());
            } 
            else if (Actual == '/') {
                Avanzar();
                nodo = new DivisionNodo(nodo, ParseFactor());
            } 
            else {
                break;
            }
        }

        return nodo;
    }