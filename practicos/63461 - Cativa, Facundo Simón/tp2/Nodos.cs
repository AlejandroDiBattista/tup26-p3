using System;

abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}



class NumeroNodo : Nodo
{
    public int Valor;

    public NumeroNodo(int valor)
    {
        Valor = valor;
    }

    public override int Evaluar(int x = 0)
    {
        return Valor;
    }
}

class VariableNodo : Nodo
{
    public override int Evaluar(int x = 0)
    {
        return x;
    }
}



class NegativoNodo : Nodo
{
    public Nodo Expr;

    public NegativoNodo(Nodo expr)
    {
        Expr = expr;
    }

    public override int Evaluar(int x = 0)
    {
        return -Expr.Evaluar(x);
    }
}



abstract class NodoBinario : Nodo
{
    public Nodo Izq;
    public Nodo Der;

    public NodoBinario(Nodo izq, Nodo der)
    {
        Izq = izq;
        Der = der;
    }
}


class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        return Izq.Evaluar(x) + Der.Evaluar(x);
    }
}

class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        return Izq.Evaluar(x) - Der.Evaluar(x);
    }
}

class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        return Izq.Evaluar(x) * Der.Evaluar(x);
    }
}

class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        int divisor = Der.Evaluar(x);

        if (divisor == 0)
            throw new Exception("Division por cero");

        return Izq.Evaluar(x) / divisor;
    }
}
