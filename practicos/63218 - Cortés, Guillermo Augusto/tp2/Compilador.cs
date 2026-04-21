using System.ComponentModel.Design.Serialization;
using System.Text.RegularExpressions;

class Compilador {
    private string input = "";
    private int pos = 0;
    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion)) throw new FormatException ("Token inesperado");

        var p = new Compilador
        {
            input = expresion,
            pos = 0
        };

        var nodo = p.ParseSumaResta();

        p.SaltarEspacios();
        if (p.pos < p.input.Length) throw new FormatException("Token inesperado");

        return nodo;
    }
    private Nodo ParseExpresion()
    {
        var nodo = ParseTermino();
        while (true)
        {
            SaltarEspacios();
            
            if(Match('+'))
            {
                nodo = new Suma(nodo, ParseTermino());
            }
            else if (Match('-'))
            {
                nodo = new Resta(nodo, ParseTermino());
            }
            else
            {
                break;
            }           
        }
        return nodo;
    }
    private Nodo ParseTermino()
    {
        var nodo = ParseFActor();
        while (true)
        {
            SAltarEspacios();
            if (Match('*'))
            {
                nodo = new Multiplicacion(nodo, ParseFActor());
            }
            else if (Match('/'))
            {
                nodo = new Division(nodo, ParseFActor());
            }
            else
            {
                break;
            }
        }
        return nodo;
    }
}

