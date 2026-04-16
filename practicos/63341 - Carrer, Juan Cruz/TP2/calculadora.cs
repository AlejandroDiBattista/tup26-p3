using System;
using System.Collections.Generic;

class Calculadora
{
    public static Integer Resolver(string expresionUsuario)
    {
        var listaTokens = GenerarTokens(expresionUsuario);
        var expresionRPN = ConvertirAPostfijo(listaTokens);
        return CalcularRPN(expresionRPN);
    }
    private static List<ElementoToken> GenerarTokens(string textoEntrada)
    {
        List<ElementoToken> listaTokens = new List<ElementoToken>();

        for (int indice = 0; indice < textoEntrada.Length; indice++)
        {
            char caracterActual = textoEntrada[indice];

            if (char.IsWhiteSpace(caracterActual)) continue;

            if (char.IsDigit(caracterActual))
            {
                string numeroConstruido = "";

                while (indice < textoEntrada.Length && char.IsDigit(textoEntrada[indice]))
                {
                    numeroConstruido += textoEntrada[indice];
                    indice++;
                }

                indice--;
                listaTokens.Add(new ElementoToken(TipoToken.Numero, numeroConstruido));
            }
            else
            {
                switch (caracterActual)
                {
                    case '+': listaTokens.Add(new ElementoToken(TipoToken.OperadorSuma)); break;
                    case '-': listaTokens.Add(new ElementoToken(TipoToken.OperadorResta)); break;
                    case '*': listaTokens.Add(new ElementoToken(TipoToken.OperadorMultiplicacion)); break;
                    case '/': listaTokens.Add(new ElementoToken(TipoToken.OperadorDivision)); break;
                    case '%': listaTokens.Add(new ElementoToken(TipoToken.OperadorModulo)); break;
                    case '(': listaTokens.Add(new ElementoToken(TipoToken.ParentesisApertura)); break;
                    case ')': listaTokens.Add(new ElementoToken(TipoToken.ParentesisCierre)); break;
                }
            }
        }

        return listaTokens;
    }
