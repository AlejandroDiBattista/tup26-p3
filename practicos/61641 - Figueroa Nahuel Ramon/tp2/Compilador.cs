using System;

namespace CalculadoraAST
{
    public class Compilador
    {
        private readonly string _input;
        private int _pos;

        public Compilador(string input)
        {
            _input = input ?? "";
            _pos = 0;
        }

        private char Current => _pos < _input.Length ? _input[_pos] : '\0';

        private void Avanzar() => _pos++;

        private void SaltearEspacios()
        {
            while (char.IsWhiteSpace(Current)) Avanzar();
        }

        public Nodo Parsear()
        {
            SaltearEspacios();
            if (string.IsNullOrEmpty(_input) || Current == '\0')
                throw new Exception("Error: Entrada vacía.");

            Nodo nodo = ParsearExpresion();
            SaltearEspacios();

            if (Current != '\0')
                throw new Exception($"Error: Token inesperado '{Current}' en la posición {_pos}.");

            return nodo;
        }

        // Expresion := Termino { ('+' | '-') Termino }
        private Nodo ParsearExpresion()
        {
            Nodo izq = ParsearTermino();
            SaltearEspacios();

            while (Current == '+' || Current == '-')
            {
                char op = Current;
                Avanzar();
                Nodo der = ParsearTermino();
                izq = (op == '+') ? new SumaNodo(izq, der) : new RestaNodo(izq, der);
                SaltearEspacios();
            }
            return izq;
        }

        // Termino := Factor { ('*' | '/') Factor }
        private Nodo ParsearTermino()
        {
            Nodo izq = ParsearFactor();
            SaltearEspacios();

            while (Current == '*' || Current == '/')
            {
                char op = Current;
                Avanzar();
                Nodo der = ParsearFactor();
                izq = (op == '*') ? new MultiplicacionNodo(izq, der) : new DivisionNodo(izq, der);
                SaltearEspacios();
            }
            return izq;
        }

        // Factor := '+' Factor | '-' Factor | '(' Expresion ')' | numero | x
        private Nodo ParsearFactor()
        {
            SaltearEspacios();

            if (Current == '+')
            {
                Avanzar();
                return new PositivoNodo(ParsearFactor());
            }
            if (Current == '-')
            {
                Avanzar();
                return new NegativoNodo(ParsearFactor());
            }
            if (Current == '(')
            {
                Avanzar();
                Nodo nodo = ParsearExpresion();
                SaltearEspacios();
                if (Current != ')') throw new Exception("Error: Paréntesis sin cerrar.");
                Avanzar();
                return nodo;
            }
            if (char.IsDigit(Current))
            {
                int val = 0;
                while (char.IsDigit(Current))
                {
                    val = val * 10 + (Current - '0');
                    Avanzar();
                }
                return new NumeroNodo(val);
            }
            if (Current == 'x' || Current == 'X')
            {
                Avanzar();
                return new VariableNodo();
            }

            throw new Exception($"Error: Token inesperado '{Current}'.");
        }
    }
}