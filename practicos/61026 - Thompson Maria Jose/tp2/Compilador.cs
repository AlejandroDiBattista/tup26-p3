using System;

public class Compilador
{
    private string input;
    private int posicion;

    public Compilador(string input)
    {
        this.input = input.Replace(" ", "");
        this.posicion = 0;
    }

    private char TokenActual()
    {
        if (posicion >= input.Length) return '\0';
        return input[posicion];
    }

    private void Avanzar()
    {
        posicion++;
    }

    public Nodo Parsear()
    {
        if (string.IsNullOrEmpty(input))
        {
            throw new Exception("Error: La entrada está vacía.");
        }

        Nodo nodo = ParsearExpresion();

        if (posicion < input.Length)
        {
            throw new Exception("Error: Token inesperado al final de la expresión.");
        }

        return nodo;
    }

    private Nodo ParsearExpresion()
    {
        Nodo izq = ParsearTermino();

        while (TokenActual() == '+' || TokenActual() == '-')
        {
            char op = TokenActual();
            Avanzar();
            Nodo der = ParsearTermino();

            if (op == '+') izq = new SumaNodo(izq, der);
            else izq = new RestaNodo(izq, der);
        }

        return izq;
    }

    private Nodo ParsearTermino()
    {
        Nodo izq = ParsearFactor();

        while (TokenActual() == '*' || TokenActual() == '/')
        {
            char op = TokenActual();
            Avanzar();
            Nodo der = ParsearFactor();

            if (op == '*') izq = new MultiplicacionNodo(izq, der);
            else izq = new DivisionNodo(izq, der);
        }

        return izq;
    }

    private Nodo ParsearFactor()
    {
        if (TokenActual() == '+')
        {
            Avanzar();
            return ParsearFactor();
        }

        if (TokenActual() == '-')
        {
            Avanzar();
            Nodo hijo = ParsearFactor();
            return new NegativoNodo(hijo);
        }

        if (TokenActual() == '(')
        {
            Avanzar();
            Nodo expr = ParsearExpresion();

            if (TokenActual() != ')')
            {
                throw new Exception("Error: Paréntesis sin cerrar.");
            }
            Avanzar();
            return expr;
        }

        if (char.IsDigit(TokenActual()))
        {
            string numeroStr = "";
            while (char.IsDigit(TokenActual()))
            {
                numeroStr += TokenActual();
                Avanzar();
            }
            return new NumeroNodo(int.Parse(numeroStr));
        }

        if (TokenActual() == 'x' || TokenActual() == 'X')
        {
            Avanzar();
            return new VariableNodo();
        }

        throw new Exception("Error: Token inesperado '" + TokenActual() + "'.");
    }
}