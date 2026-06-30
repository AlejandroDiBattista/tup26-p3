using System;
using System.Collections.Generic;

namespace CalculadoraAST {
    public class Compilador {
        private readonly string _input;
        private int _pos;

        public Compilador(string input) {
            _input = input.Replace(" ", ""); // Limpio espacios
            _pos = 0;
        }

        private char Actual => _pos < _input.Length ? _input[_pos] : '\0';

        public Nodo Parsear() {
            if (string.IsNullOrEmpty(_input)) throw new Exception("Entrada vacía");
            Nodo nodo = ParsearExpresion();
            if (_pos < _input.Length) throw new Exception("Token inesperado: " + Actual);
            return nodo;
        }

        private Nodo ParsearExpresion() {
            Nodo nodo = ParsearTermino();
            while (Actual == '+' || Actual == '-') {
                char op = Actual;
                _pos++;
                Nodo derecho = ParsearTermino();
                nodo = op == '+' ? new SumaNodo(nodo, derecho) : new RestaNodo(nodo, derecho);
            }
            return nodo;
        }

        private Nodo ParsearTermino() {
            Nodo nodo = ParsearFactor();
            while (Actual == '*' || Actual == '/') {
                char op = Actual;
                _pos++;
                Nodo derecho = ParsearFactor();
                nodo = op == '*' ? new MultiplicacionNodo(nodo, derecho) : new DivisionNodo(nodo, derecho);
            }
            return nodo;
        }

        private Nodo ParsearFactor() {
            if (Actual == '+') { _pos++; return ParsearFactor(); }
            if (Actual == '-') { _pos++; return new NegativoNodo(ParsearFactor()); }

            if (Actual == '(') {
                _pos++;
                Nodo nodo = ParsearExpresion();
                if (Actual != ')') throw new Exception("Paréntesis sin cerrar");
                _pos++;
                return nodo;
            }

            if (char.IsDigit(Actual)) {
                string numero = "";
                while (char.IsDigit(Actual)) { numero += _input[_pos++]; }
                return new NumeroNodo(int.Parse(numero));
            }

            if (Actual == 'x' || Actual == 'X') {
                _pos++;
                return new VariableNodo();
            }

            throw new Exception("Token inesperado: " + Actual);
        }
    }
}
