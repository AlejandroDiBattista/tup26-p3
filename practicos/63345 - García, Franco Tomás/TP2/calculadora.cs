class Calculadora
{
    public static Integer Resolver(string expresion)
    {
        var listaTokens = GenerarTokens(expresion);

        return new Integer();
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
}