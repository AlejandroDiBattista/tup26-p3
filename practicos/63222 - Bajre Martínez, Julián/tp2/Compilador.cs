using System;
using System.Collections.Generic;
namespace TP2.Calculadora;

public class Compilador
{
    private string _expresion = "";
    private int _posicion = 0;

    public Nodo Parsear(string expresion)
    {
        _expresion = expresion.Replace(" ", "");
        _posicion = 0;
        return ParsearExpresion();
    }

    private char CharActual => _posicion < _expresion.Length ? _expresion[_posicion] : '\0';

    private char Consumir() => _expresion[_posicion++];

    private Nodo ParsearExpresion()
    {
        Nodo izq = ParsearTermino();

        while (CharActual == '+' || CharActual == '-')
        {
            char operador = Consumir();
            Nodo der = ParsearTermino();
            izq = operador == '+' ? new NodoSuma(izq, der) : new NodoResta(izq, der);
        }

        return izq;
    }

    private Nodo ParsearTermino()
    {
        Nodo izq = ParsearFactor();

        while (CharActual == '*' || CharActual == '/')
        {
            char operador = Consumir();
            Nodo der = ParsearFactor();
            izq = operador == '*' ? new NodoProducto(izq, der) : new NodoCociente(izq, der);
        }

        return izq;
    }

    private Nodo ParsearFactor()
    {
        if (CharActual == 'x' || CharActual == 'X')
        {
            Consumir();
            return new NodoVariable();
        }

        if (char.IsDigit(CharActual))
        {
            return LeerNumero();
        }

        throw new Exception($"Token inesperado '{CharActual}' en posición {_posicion}.");
    }

    private NodoNumero LeerNumero()
    {
        int inicio = _posicion;
        while (char.IsDigit(CharActual))
            Consumir();

        int valor = int.Parse(_expresion.Substring(inicio, _posicion - inicio));
        return new NodoNumero(valor);
    }
}