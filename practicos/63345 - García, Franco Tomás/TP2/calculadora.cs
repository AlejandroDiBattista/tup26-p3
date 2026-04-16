class Calculadora
{
    public static Integer Resolver(string expresion)
    {
        var listaTokens = GenerarTokens(expresion);
        var salidaRPN = ConvertirAPostfijo(listaTokens);
        return CalcularRPN(salidaRPN);
    }

    private static List<Token> GenerarTokens(string texto)
    {
        List<Token> lista = new List<Token>();

        for (int indice = 0; indice < texto.Length; indice++)
        {
            char caracter = texto[indice];

            if (char.IsWhiteSpace(caracter)) continue;

            if (char.IsDigit(caracter))
            {
                string numeroConstruido = "";

                while (indice < texto.Length && char.IsDigit(texto[indice]))
                {
                    numeroConstruido += texto[indice];
                    indice++;
                }

                indice--;
                lista.Add(new Token(TipoToken.Numero, numeroConstruido));
            }
            else
            {
                switch (caracter)
                {
                    case '+': lista.Add(new Token(TipoToken.Suma)); break;
                    case '-': lista.Add(new Token(TipoToken.Resta)); break;
                    case '*': lista.Add(new Token(TipoToken.Multiplicacion)); break;
                    case '/': lista.Add(new Token(TipoToken.Division)); break;
                    case '%': lista.Add(new Token(TipoToken.Modulo)); break;
                    case '(': lista.Add(new Token(TipoToken.ParentesisIzquierdo)); break;
                    case ')': lista.Add(new Token(TipoToken.ParentesisDerecho)); break;
                }
            }
        }

        return lista;
    }

    private static int ObtenerPrecedencia(Token tokenActual)
    {
        if (tokenActual.Tipo == TipoToken.Suma || tokenActual.Tipo == TipoToken.Resta) return 1;
        if (tokenActual.Tipo == TipoToken.Multiplicacion || tokenActual.Tipo == TipoToken.Division || tokenActual.Tipo == TipoToken.Modulo) return 2;
        return 0;
    }

    private static List<Token> ConvertirAPostfijo(List<Token> listaTokens)
    {
        List<Token> salida = new List<Token>();
        Stack<Token> operadores = new Stack<Token>();

        foreach (var tokenActual in listaTokens)
        {
            if (tokenActual.Tipo == TipoToken.Numero)
                salida.Add(tokenActual);

            else if (tokenActual.Tipo == TipoToken.ParentesisIzquierdo)
                operadores.Push(tokenActual);

            else if (tokenActual.Tipo == TipoToken.ParentesisDerecho)
            {
                while (operadores.Peek().Tipo != TipoToken.ParentesisIzquierdo)
                    salida.Add(operadores.Pop());

                operadores.Pop();
            }
            else
            {
                while (operadores.Count > 0 && ObtenerPrecedencia(operadores.Peek()) >= ObtenerPrecedencia(tokenActual))
                    salida.Add(operadores.Pop());

                operadores.Push(tokenActual);
            }
        }

        while (operadores.Count > 0)
            salida.Add(operadores.Pop());

        return salida;
    }

    private static Integer CalcularRPN(List<Token> listaTokens)
    {
        Stack<Integer> pila = new Stack<Integer>();

        foreach (var tokenActual in listaTokens)
        {
            if (tokenActual.Tipo == TipoToken.Numero)
            {
                pila.Push(new Integer(tokenActual.Contenido));
            }
            else
            {
                var segundo = pila.Pop();
                var primero = pila.Pop();

                switch (tokenActual.Tipo)
                {
                    case TipoToken.Suma: pila.Push(primero + segundo); break;
                    case TipoToken.Resta: pila.Push(primero - segundo); break;
                    case TipoToken.Multiplicacion: pila.Push(primero * segundo); break;
                    case TipoToken.Division: pila.Push(primero / segundo); break;
                    case TipoToken.Modulo: pila.Push(primero % segundo); break;
                }
            }
        }

        return pila.Pop();
    }
}