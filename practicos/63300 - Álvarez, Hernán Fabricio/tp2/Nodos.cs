using System;

abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

abstract class NumNodo : Nodo
{
    public int Valor {get;}
    public NumNodo(int valor) => Valor = valor;

    public override int Evaluar(int x = 0) => Valor;
}
abstract class varNodo : Nodo
{
    public override int Evaluar(int x = 0) => x;
}
abstract class PositivoNodo : Nodo
{
    public Nodo hijo {get;}
    public PositivoNodo(Nodo hijo) => hijo = hijo;
    public override int Evaluar(int x = 0) => Hijo.Evaluar(x);
}
abstract class NegativoNodo : Nodo
{
    public Nodo Hijo {get;}
    public NegativoNodo(Nodo hijo) => Hijo = hijo;

    public override int Evaluar (int x = 0) => -Hijo.Evaluar(x);
}