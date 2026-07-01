using System;
using System.Collections.Generic;
using System.Linq;

namespace Calculadora
{
    public static class ConstructorDeExpresiones
    {
        public static ExpresionMatematica AnalizarCadena(string entrada)
        {
            // Tokenización manual
            var tokens = new List<string>();
            for (int i = 0; i < entrada.Length; i++)
            {
                char c = entrada[i];
                if (char.IsWhiteSpace(c)) continue;

                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < entrada.Length && char.IsDigit(entrada[i])) i++;
                    tokens.Add(entrada.Substring(start, i - start));
                    i--;
                }
                else if (c == 'x' || c == 'X')
                {
                    tokens.Add("x");
                }
                else if ("+-*/()".Contains(c))
                {
                    tokens.Add(c.ToString());
                }
                else
                {
                    throw new Exception($"Carácter no válido: '{c}'");
                }
            }

            if (tokens.Count == 0)
                throw new Exception("Entrada vacía");

            int pos = 0;

            // Funciones locales para la gramática
            ExpresionMatematica Expresion()
            {
                ExpresionMatematica izq = Termino();
                while (pos < tokens.Count && (tokens[pos] == "+" || tokens[pos] == "-"))
                {
                    string op = tokens[pos++];
                    ExpresionMatematica der = Termino();
                    izq = op == "+" ? new Suma(izq, der) : new Resta(izq, der);
                }
                return izq;
            }

            ExpresionMatematica Termino()
            {
                ExpresionMatematica izq = Factor();
                while (pos < tokens.Count && (tokens[pos] == "*" || tokens[pos] == "/"))
                {
                    string op = tokens[pos++];
                    ExpresionMatematica der = Factor();
                    izq = op == "*" ? new Producto(izq, der) : new Cociente(izq, der);
                }
                return izq;
            }

            ExpresionMatematica Factor()
            {
                if (pos >= tokens.Count)
                    throw new Exception("Expresión incompleta");

                string actual = tokens[pos];
                if (actual == "+")
                {
                    pos++;
                    return new Identidad(Factor());
                }
                if (actual == "-")
                {
                    pos++;
                    return new Opuesto(Factor());
                }
                if (actual == "(")
                {
                    pos++;
                    ExpresionMatematica interno = Expresion();
                    if (pos >= tokens.Count || tokens[pos] != ")")
                        throw new Exception("Falta paréntesis de cierre");
                    pos++;
                    return interno;
                }
                if (int.TryParse(actual, out int numero))
                {
                    pos++;
                    return new ConstanteNumerica(numero);
                }
                if (actual.ToLower() == "x")
                {
                    pos++;
                    return new VariableX();
                }

                throw new Exception($"Token desconocido: '{actual}'");
            }

            ExpresionMatematica resultado = Expresion();
            if (pos != tokens.Count)
                throw new Exception($"Sobran tokens: {string.Join("", tokens.Skip(pos))}");
            return resultado;
        }
    }
}