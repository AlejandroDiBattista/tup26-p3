using System;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Xml;


public class Compilador {
    
    private readonly string _texto;
    private int _posicion;
    public static Nodo Parse (string expresion)
    {
        return new Compilador(expresion).Parsear();
    }
    public Compilador(string texto)
    {
        if(string.IsNullOrWhiteSpace(texto))
         throw new Exception("Error: dato de entrada vacio.");
         _texto = texto ?? "";
         _posicion = 0;
    }
 private char CaracterActual => _posicion < _texto.Length ? _texto[_posicion] : '\0';

 private void Avanzar() => _posicion++;
 private void SaltarEspacios()
    {
        while (char.IsWhiteSpace(CaracterActual)) Avanzar();
    }   

public Nodo Parsear()
    {
        SaltarEspacios();
        if (CaracterActual == '\0')
           throw new FormatException("Token inesperado :  Entrada vacia.");
        
        var ast = ParsearExpresion();

        SaltarEspacios();
        if(CaracterActual != '\0')
            throw new FormatException($"Error token Inesperado '{CaracterActual}' en la posicion {_posicion}.");

        return ast;
    }

private Nodo ParsearExpresion()
    {
        var nodo = ParsearTermino();
        SaltarEspacios();

        while (CaracterActual == "+" || CaracterActual == "-")
        {
            Char operador = CaracterActual;
            Avanzar();
            var derecho = ParsearTermino();

            if(operador == "+") nodo = new SumaNodo(nodo , derecho);
            else nodo = new RestaNodo(nodo, derecho);

            SaltarEspacios();
        }
        return nodo;
    }
    
    private Nodo ParsearTermino()
    {
        var nodo = ParsearFactor();
        SaltarEspacios();
        while (CaracterActual == '*' || CaracterActual == '/')
        {
            char operador = CaracterActual;
            Avanzar();
            var derecho = ParsearFactor();

            if (operador == "*") nodo = new MultiplicacionNodo(nodo, derecho);
            else nodo = new DivisionNodo(nodo, derecho);

            SaltarEspacios();
        }
        return nodo;
    }

    private Nodo ParsearFactor()
    {
        SaltarEspacios();

        if (CaracterActual == "+")
        {
            Avanzar();
            return new PositivoNodo(ParsearFactor());
        }
         if (CaracterActual == "-")
        {
            Avanzar();
            return new NegativoNodo(ParsearFactor());
        }
         if (CaracterActual == "(")
        {
            Avanzar();
            var nodo = ParsearExpresion();
            SaltarEspacios();
            if(CaracterActual != ")") throw new FormatException("Error: Parentesis sin cerrar.");
            Avanzar();
            return nodo;
        }
        if (CaracterActual == 'x' || CaracterActual == 'X')
        {
            Avanzar();
            return new varNodo();

        }
        if (char.IsDigit(CaracterActual))
        {
            int inicio = _posicion;
            while (char.IsDigit(CaracterActual)) Avanzar();
            string numeroStr = _texto.Substring(inicio, _posicion - inicio);
            return new NumNodo(int.Parse(numeroStr));
        }
        throw new Exception($"Error : token inesperado '{CaracterActual}' en la posicion {_posicion}");

    }
}
