using System;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Xml;


public class Compilador {
    private readonly string _texto;
    private int _posicion;

    public static Nodo Parse(string expresion) {
        return new Compilador(expresion).Parsear();
    }

    private Compilador(string texto) {
        _texto = texto ?? "";
        _posicion = 0;
    }

    private char CaracterActual => _posicion < _texto.Length ? _texto[_posicion] : '\0';

    private void Avanzar() => _posicion++;

    private void SaltarEspacios() {
        while (char.IsWhiteSpace(CaracterActual)) Avanzar();
    }

    private Nodo Parsear() {
        SaltarEspacios();
        if (CaracterActual == '\0') 
            throw new FormatException("Token inesperado: entrada vacía");

        var ast = ParsearExpresion();
        SaltarEspacios();
        
        if (CaracterActual != '\0')
            throw new FormatException($"Token inesperado: '{CaracterActual}'");

        return ast;
    }

    private Nodo ParsearExpresion() {
        var nodo = ParsearTermino();
        SaltarEspacios();

        while (CaracterActual == '+' || CaracterActual == '-') {
            char operador = CaracterActual;
            Avanzar();
            var derecho = ParsearTermino();

            if (operador == '+') nodo = new SumaNodo(nodo, derecho);
            else nodo = new RestaNodo(nodo, derecho);

            SaltarEspacios();
        }
        return nodo;
    }

    private Nodo ParsearTermino() {
        var nodo = ParsearFactor();
        SaltarEspacios();

        while (CaracterActual == '*' || CaracterActual == '/') {
            char operador = CaracterActual;
            Avanzar();
            var derecho = ParsearFactor();

            if (operador == '*') nodo = new MultiplicacionNodo(nodo, derecho);
            else nodo = new DivisionNodo(nodo, derecho);

            SaltarEspacios();
        }
        return nodo;
    }

    private Nodo ParsearFactor() {
        SaltarEspacios();
        if (CaracterActual == '\0') throw new FormatException("Token inesperado");

        if (CaracterActual == '+') {
            Avanzar();
            return new PositivoNodo(ParsearFactor());
        }
        if (CaracterActual == '-') {
            Avanzar();
            return new NegativoNodo(ParsearFactor());
        }
        if (CaracterActual == '(') {
            Avanzar();
            var nodo = ParsearExpresion();
            SaltarEspacios();
            if (CaracterActual != ')') throw new FormatException("Se esperaba ')'");
            Avanzar(); 
            return nodo;
        }
        if (CaracterActual == 'x' || CaracterActual == 'X') {
            Avanzar();
            return new VariableNodo();
        }
        if (char.IsDigit(CaracterActual)) {
            int inicio = _posicion;
            while (char.IsDigit(CaracterActual)) Avanzar();
            string numeroStr = _texto.Substring(inicio, _posicion - inicio);
            return new NumeroNodo(int.Parse(numeroStr));
        }

        throw new FormatException($"Token inesperado: '{CaracterActual}'");
    }
}