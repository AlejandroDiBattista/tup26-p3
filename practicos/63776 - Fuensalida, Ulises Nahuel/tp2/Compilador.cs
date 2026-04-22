using System;

namespace CalculadoraUTN
{
    class Compilador
    {
        private string _entrada;
        private int _pos;

        public Nodo Parsear(string expresion)
        {
            _entrada = expresion.Replace(" ", "");
            _pos = 0;
            if (string.IsNullOrEmpty(_entrada)) throw new Exception("Entrada vacía.");
            
            Nodo nodo = ParsearExpresion();
            if (_pos < _entrada.Length) throw new Exception($"Token inesperado: '{_entrada[_pos]}'");
            return nodo;
        }

        private Nodo ParsearExpresion()
        {
            Nodo nodo = ParsearTermino();
            while (_pos < _entrada.Length && (_entrada[_pos] == '+' || _entrada[_pos] == '-'))
            {
                char op = _entrada[_pos++];
                Nodo derecha = ParsearTermino();
                nodo = (op == '+') ? new SumaNodo(nodo, derecha) : new RestaNodo(nodo, derecha);
            }
            return nodo;
        }

        private Nodo ParsearTermino()
        {
            Nodo nodo = ParsearFactor();
            while (_pos < _entrada.Length && (_entrada[_pos] == '*' || _entrada[_pos] == '/'))
            {
                char op = _entrada[_pos++];
                Nodo derecha = ParsearFactor();
                nodo = (op == '*') ? new MultiplicacionNodo(nodo, derecha) : new DivisionNodo(nodo, derecha);
            }
            return nodo;
        }

        private Nodo ParsearFactor()
        {
            if (_pos >= _entrada.Length) throw new Exception("Expresión incompleta.");

            if (_entrada[_pos] == '+') { _pos++; return new PositivoNodo(ParsearFactor()); }
            if (_entrada[_pos] == '-') { _pos++; return new NegativoNodo(ParsearFactor()); }
            
            if (_entrada[_pos] == '(')
            {
                _pos++;
                Nodo nodo = ParsearExpresion();
                if (_pos >= _entrada.Length || _entrada[_pos] != ')') throw new Exception("Paréntesis sin cerrar.");
                _pos++;
                return nodo;
            }

            if (char.ToLower(_entrada[_pos]) == 'x') { _pos++; return new VariableNodo(); }

            if (char.IsDigit(_entrada[_pos]))
            {
                int inicio = _pos;
                while (_pos < _entrada.Length && char.IsDigit(_entrada[_pos])) _pos++;
                return new NumeroNodo(int.Parse(_entrada.Substring(inicio, _pos - inicio)));
            }

            throw new Exception($"Carácter no reconocido: '{_entrada[_pos]}'");
        }
    }
}