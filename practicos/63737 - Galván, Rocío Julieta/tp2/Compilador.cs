namespace Calculadora;

public static class Compilador
{
    private static string texto = "";
    private static int posicion;

    public static Nodo Parse(string expresion)
    {
        texto = expresion;
        posicion = 0;

        Nodo resultado = ParseSumaResta();
        SaltarEspacios();

        if (posicion < texto.Length)
            throw new FormatException($"Token inesperado: '{texto[posicion]}'");

        return resultado;
    }

    private static Nodo ParseSumaResta()
    {
        Nodo nodo = ParseMultiplicacionDivision();

        while (true)
        {
            SaltarEspacios();

            if (Coincidir('+'))
                nodo = new SumaNodo(nodo, ParseMultiplicacionDivision());
            else if (Coincidir('-'))
                nodo = new RestaNodo(nodo, ParseMultiplicacionDivision());
            else
                return nodo;
        }
    }

    private static Nodo ParseMultiplicacionDivision()
    {
        Nodo nodo = ParseFactor();

        while (true)
        {
            SaltarEspacios();

            if (Coincidir('*'))
                nodo = new MultiplicacionNodo(nodo, ParseFactor());
            else if (Coincidir('/'))
                nodo = new DivisionNodo(nodo, ParseFactor());
            else
                return nodo;
        }
    }

    private static Nodo ParseFactor()
    {
        SaltarEspacios();

        if (posicion >= texto.Length)
            throw new FormatException("Token inesperado");

        if (Coincidir('+'))
            return ParseFactor();

        if (Coincidir('-'))
            return new NegativoNodo(ParseFactor());

        if (Coincidir('('))
        {
            Nodo nodo = ParseSumaResta();

            if (!Coincidir(')'))
                throw new FormatException("Se esperaba ')'");

            return nodo;
        }

        if (char.IsDigit(texto[posicion]))
            return ParseNumero();

        if (texto[posicion] == 'x' || texto[posicion] == 'X')
        {
            posicion++;
            return new VariableNodo();
        }

        throw new FormatException($"Token inesperado: '{texto[posicion]}'");
    }

    private static Nodo ParseNumero()
    {
        int inicio = posicion;

        while (posicion < texto.Length && char.IsDigit(texto[posicion]))
        {
            posicion++;
        }

        int valor = int.Parse(texto[inicio..posicion]);
        return new NumeroNodo(valor);
    }

    private static bool Coincidir(char esperado)
    {
        SaltarEspacios();

        if (posicion < texto.Length && texto[posicion] == esperado)
        {
            posicion++;
            return true;
        }

        return false;
    }

    private static void SaltarEspacios()
    {
        while (posicion < texto.Length && char.IsWhiteSpace(texto[posicion]))
        {
            posicion++;
        }
    }
}