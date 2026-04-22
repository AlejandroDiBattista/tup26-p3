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
    
