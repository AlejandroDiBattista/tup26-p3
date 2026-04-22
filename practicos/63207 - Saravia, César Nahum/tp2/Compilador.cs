using System;
class Compilador {
    private string input = "";
    private int pos = 0;
    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new FormatException("Token inesperado");
        var comp = new Compilador();
        comp.input = expresion;
        comp.pos = 0;
        var nodo = comp.ParseExpresion();
        return nodo;
    }
    private Nodo ParseExpresion() {
        Nodo nodo = ParseTermino();
        while (true) {
            SkipEspacios();
            if (Match('+')) {
                nodo = new SumaNodo(nodo, ParseTermino());
            }
            else if (Match('-')) {
                nodo = new RestaNodo(nodo, ParseTermino());
            }
            else {
                break;
            }
        }
        return nodo;
    }
    private Nodo ParseTermino() {
        Nodo nodo = ParseFactor();
        while (true) {
            SkipEspacios();
            if (Match('*')) {
                nodo = new MultiplicacionNodo(nodo, ParseFactor());
            }
            else if (Match('/')) {
                nodo = new DivisionNodo(nodo, ParseFactor());
            }
            else {
                break;
            }
        }

        return nodo;
    }
    }
