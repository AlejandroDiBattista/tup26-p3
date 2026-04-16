using System;
using System.Collections.Generic;

class Calculadora
{
    public static Integer Resolver(string expresionUsuario)
    {
        var listaTokens = GenerarTokens(expresionUsuario);
        var expresionRPN = ConvertirAPostfijo(listaTokens);
        return CalcularRPN(expresionRPN);
    }
