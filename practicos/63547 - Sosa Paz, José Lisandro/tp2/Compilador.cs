// Compilador.cs
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Compilador
{
    private string _entrada;
    private int _pos;
    private string _tokenActual;

    public Nodo Parsear(string expresion)
    {
        if (string.IsNullOrWhiteSpace(expresion)) throw new Exception("Error: Entrada vacía.");
        _entrada = expresion.Replace(" ", "");
        _pos = 0;
        SiguienteToken();
        var nodo = ParsearExpresion();
        if (_tokenActual != null) throw new Exception($"Error: Token inesperado '{_tokenActual}'");
        return nodo;
    }

    private void SiguienteToken()
    {
        if (_pos >= _entrada.Length) { _tokenActual = null; return; }
        char c = _entrada[_pos];
        if (char.IsDigit(c))
        {
            string num = "";
            while (_pos < _entrada.Length && char.IsDigit(_entrada[_pos])) num += _entrada[_pos++];
            _tokenActual = num;
        }
        else if (char.ToLower(c) == 'x') { _tokenActual = "x"; _pos++; }
        else { _tokenActual = c.ToString(); _pos++; }
    }

    private Nodo ParsearExpresion()
    {
        var nodo = ParsearTermino();
        while (_tokenActual == "+" || _tokenActual == "-")
        {
            string op = _tokenActual;
            SiguienteToken();
            var derecha = ParsearTermino();
            nodo = op == "+" ? new SumaNodo(nodo, derecha) : new RestaNodo(nodo, derecha);
        }
        return nodo;
    }

    private Nodo ParsearTermino()
    {
        var nodo = ParsearFactor();
        while (_tokenActual == "*" || _tokenActual == "/")
        {
            string op = _tokenActual;
            SiguienteToken();
            var derecha = ParsearFactor();
            nodo = op == "*" ? new MultiplicacionNodo(nodo, derecha) : new DivisionNodo(nodo, derecha);
        }
        return nodo;
    }

    private Nodo ParsearFactor()
    {
        if (_tokenActual == "+") { SiguienteToken(); return ParsearFactor(); }
        if (_tokenActual == "-") { SiguienteToken(); return new NegativoNodo(ParsearFactor()); }
        if (_tokenActual == "(")
        {
            SiguienteToken();
            var nodo = ParsearExpresion();
            if (_tokenActual != ")") throw new Exception("Error: Paréntesis sin cerrar.");
            SiguienteToken();
            return nodo;
        }
        if (int.TryParse(_tokenActual, out int valor)) { SiguienteToken(); return new NumeroNodo(valor); }
        if (_tokenActual == "x") { SiguienteToken(); return new VariableNodo(); }
        throw new Exception($"Error: Token inesperado '{_tokenActual}'");
    }
}