using System;
using System.Text;

namespace CalculadoraAST
{
    public static class Compilador
    {
        public static Nodo Parse(string entrada)
        {
            if (string.IsNullOrWhiteSpace(entrada))
                throw new Exception("Error de sintaxis: Entrada vacía");

            var parser = new Parser(entrada);
            Nodo ast = parser.Parsear();

            if (parser.TokenActual != '\0')
                throw new Exception($"Error de sintaxis: Carácter inesperado al final '{parser.TokenActual}'");

            return ast;
        }
    }

    public class Parser
    {
        private int _posicion;
        private readonly string _entrada;
        
        
        public char TokenActual => _posicion < _entrada.Length ? _entrada[_posicion] : '\0';

        public Parser(string entrada)
        {      
            _entrada = entrada.Replace(" ", "");
            _posicion = 0;
        }

        private void Avanzar() => _posicion++;

        public Nodo Parsear()
        {
            Nodo nodo = ParsearTermino();

            while (TokenActual == '+' || TokenActual == '-')
            {
                char op = TokenActual;
                Avanzar();
                Nodo derecho = ParsearTermino();

                if (op == '+') nodo = new SumaNodo(nodo, derecho);
                else nodo = new RestaNodo(nodo, derecho);
            }

            return nodo;
        }

        private Nodo ParsearTermino()
        {
            Nodo nodo = ParsearFactor();

            while (TokenActual == '*' || TokenActual == '/')
            {
                char op = TokenActual;
                Avanzar();
                Nodo derecho = ParsearFactor();

                if (op == '*') nodo = new MultiplicacionNodo(nodo, derecho);
                else nodo = new DivisionNodo(nodo, derecho);
            }

            return nodo;
        }

        private Nodo ParsearFactor()
        {
            if (TokenActual == '+')
            {
                Avanzar();
                return new PositivoNodo(ParsearFactor());
            }

            if (TokenActual == '-')
            {
                Avanzar();
                return new NegativoNodo(ParsearFactor());
            }

            if (TokenActual == '(')
            {
                Avanzar();
                Nodo nodo = Parsear();
                if (TokenActual != ')')
                    throw new Exception("Error de sintaxis: Paréntesis sin cerrar. Se esperaba ')'");
                Avanzar(); 
                return nodo;
            }

            if (char.IsDigit(TokenActual))
            {
                StringBuilder sb = new StringBuilder();
                while (char.IsDigit(TokenActual))
                {
                    sb.Append(TokenActual);
                    Avanzar();
                }
                return new NumeroNodo(int.Parse(sb.ToString()));
            }

            if (TokenActual == 'x' || TokenActual == 'X')
            {
                Avanzar();
                return new VariableNodo();
            }

            throw new Exception($"Error de sintaxis: Token inesperado '{TokenActual}'");
        }
    }
}