using System;
using System.Linq;

public class Compilador
{
    private string expresion;
    private int posicion;
    private char ultimoToken;

    public Compilador(string expresion)
    {
        this.expresion = expresion.Replace(" ", "");
        this.posicion = 0;
        this.ultimoToken = '\0'; 
    }

    public Nodo Parsear()
    {
        if (string.IsNullOrEmpty(expresion))
        {
            throw new Exception("Expresión vacía");
        }

        Nodo resultado = ParsearExpresion();

        if (posicion < expresion.Length)
        {
            throw new Exception($"Token inesperado en posición {posicion}: '{expresion[posicion]}'");
        }

        return resultado;
    }

    private Nodo ParsearExpresion()
    {
        Nodo izquierdo = ParsearTermino();

        while (posicion < expresion.Length && (expresion[posicion] == '+' || expresion[posicion] == '-'))
        {
            char operador = expresion[posicion];
            posicion++;
            ultimoToken = operador; 
            Nodo derecho = ParsearTermino();

            if (operador == '+')
                izquierdo = new SumaNodo(izquierdo, derecho);
            else
                izquierdo = new RestaNodo(izquierdo, derecho);
        }

        return izquierdo;
    }

    private Nodo ParsearTermino()
    {
        Nodo izquierdo = ParsearFactor();

        while (posicion < expresion.Length && (expresion[posicion] == '*' || expresion[posicion] == '/'))
        {
            char operador = expresion[posicion];
            posicion++;
            ultimoToken = operador; 
            Nodo derecho = ParsearFactor();

            if (operador == '*')
                izquierdo = new MultiplicacionNodo(izquierdo, derecho);
            else
                izquierdo = new DivisionNodo(izquierdo, derecho);
        }

        return izquierdo;
    }

    private Nodo ParsearFactor()
    {
        if (posicion < expresion.Length)
        {
            if (expresion[posicion] == '+' && EsUnario())
            {
                posicion++;
                ultimoToken = '+';
                return new PositivoNodo(ParsearFactor());
            }

            if (expresion[posicion] == '-' && EsUnario())
            {
                posicion++;
                ultimoToken = '-';
                return new NegativoNodo(ParsearFactor());
            }

            if (expresion[posicion] == '(')
            {
                posicion++;
                ultimoToken = '(';
                Nodo resultado = ParsearExpresion();

                if (posicion >= expresion.Length || expresion[posicion] != ')')
                {
                    throw new Exception("Paréntesis sin cerrar");
                }

                posicion++;
                ultimoToken = ')';
                return resultado;
            }

            if (char.ToLower(expresion[posicion]) == 'x')
            {
                posicion++;
                ultimoToken = 'x';
                return new VariableNodo();
            }

            if (char.IsDigit(expresion[posicion]))
            {
                return ParsearNumero();
            }

            throw new Exception($"Token inesperado: '{expresion[posicion]}'");
        }

        throw new Exception("Expresión incompleta");
    }

    private bool EsUnario()
    {
        return ultimoToken == '\0' || 
               ultimoToken == '+' || 
               ultimoToken == '-' || 
               ultimoToken == '*' || 
               ultimoToken == '/' || 
               ultimoToken == '(';
    }

    private Nodo ParsearNumero()
    {
        int inicio = posicion;

        while (posicion < expresion.Length && char.IsDigit(expresion[posicion]))
        {
            posicion++;
        }

        string numeroStr = expresion.Substring(inicio, posicion - inicio);
        int numero = int.Parse(numeroStr);
        ultimoToken = 'n'; 

        return new NumeroNodo(numero);
    }
}