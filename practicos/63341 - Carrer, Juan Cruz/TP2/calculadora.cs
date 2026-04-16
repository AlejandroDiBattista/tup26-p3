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
    private static int ObtenerPrioridad(ElementoToken tokenActual)
    {
        if (tokenActual.Tipo == TipoToken.OperadorSuma || tokenActual.Tipo == TipoToken.OperadorResta) return 1;
        if (tokenActual.Tipo == TipoToken.OperadorMultiplicacion || tokenActual.Tipo == TipoToken.OperadorDivision || tokenActual.Tipo == TipoToken.OperadorModulo) return 2;
        return 0;
    }
    private static List<ElementoToken> ConvertirAPostfijo(List<ElementoToken> listaTokens)
    {
        List<ElementoToken> salida = new List<ElementoToken>();
        Stack<ElementoToken> pilaOperadores = new Stack<ElementoToken>();

        foreach (var tokenActual in listaTokens)
        {
            if (tokenActual.Tipo == TipoToken.Numero)
                salida.Add(tokenActual);

            else if (tokenActual.Tipo == TipoToken.ParentesisApertura)
                pilaOperadores.Push(tokenActual);

            else if (tokenActual.Tipo == TipoToken.ParentesisCierre)
            {
                while (pilaOperadores.Peek().Tipo != TipoToken.ParentesisApertura)
                    salida.Add(pilaOperadores.Pop());

                pilaOperadores.Pop();
            }
            else
            {
                while (pilaOperadores.Count > 0 && ObtenerPrioridad(pilaOperadores.Peek()) >= ObtenerPrioridad(tokenActual))
                    salida.Add(pilaOperadores.Pop());

                pilaOperadores.Push(tokenActual);
            }
        }

        while (pilaOperadores.Count > 0)
            salida.Add(pilaOperadores.Pop());

        return salida;
    }





