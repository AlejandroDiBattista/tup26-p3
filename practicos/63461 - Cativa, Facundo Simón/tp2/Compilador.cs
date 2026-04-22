using System;

static class Compilador
{
    static string texto = "";
    static int pos = 0;

    public static Nodo Parse(string input)
    {
        texto = input;
        pos = 0;

        var nodo = ParseExpresion();

        return nodo;
    }

    static Nodo ParseExpresion()
    {
        return ParseFactor();
    }

    static Nodo ParseFactor()
    {
        SaltarEspacios();

        if (pos >= texto.Length)
            throw new Exception("Expresion vacia");

        char actual = texto[pos];

        // numero
        if (char.IsDigit(actual))
        {
            int inicio = pos;

            while (pos < texto.Length && char.IsDigit(texto[pos]))
                pos++;

            string numero = texto.Substring(inicio, pos - inicio);
            return new NumeroNodo(int.Parse(numero));
        }

        // variable x
        if (actual == 'x' || actual == 'X')
        {
            pos++;
            return new VariableNodo();
        }

        throw new Exception("Token inesperado");
    }

    static void SaltarEspacios()
    {
        while (pos < texto.Length && char.IsWhiteSpace(texto[pos]))
            pos++;
    }
}