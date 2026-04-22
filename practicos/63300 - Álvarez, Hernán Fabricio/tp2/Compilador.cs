using System;
using System.Runtime.CompilerServices;
using System.Xml;


public class Compilador {
    
    private readonly string _texto;
    private int _posicion;
    public Compilador(string texto)
    {
        if(string.IsNullOrWhiteSpace(texto))
         throw new Exception("Error: dato de entrada vacio.");
         _texto = texto;
         _posicion = 0;
    }
 private char CaracterActual => _posicion < _texto.Length ? _texto.Length ? _texto[_posicion] : '\0' ;

 private void Avanzar() => _posicion++;
 private void SaltarEspacios()
    {
        while (char.IsWhiteSpace(CaracterActual)) Avanzar();
    }   

public Nodo Parsear()
    {
        SaltarEspacios();
        if (CaracterActual == '\0')
           throw new Exception("Error: Entrada vacia.");
        
        var ast = ParsearExpresion();

        SaltarEspacios();
        if(CaracterActual != '\0')
            throw new Exception($"Error token Inesperado '{CaracterActual}' en la posicion {_posicion}.");

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
    
    

}
