using System;

public abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}

public class NumeroNodo : Nodo
{
    private int valor;

    public NumeroNodo(int valor)
    {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0)
    {
        return valor;
    }
}

public class VariableNodo : Nodo
{
    public override int Evaluar(int x = 0)
    {
        return x;
    }
}

public class NegativoNodo : Nodo
{
    private Nodo hijo;

    public NegativoNodo(Nodo hijo)
    {
        this.hijo = hijo;
    }

    public override int Evaluar(int x = 0)
    {
        return -hijo.Evaluar(x);
    }
}

public abstract class NodoBinario : Nodo
{
    protected Nodo izquierdo;
    protected Nodo derecho;

    public NodoBinario(Nodo izquierdo, Nodo derecho)
    {
        this.izquierdo = izquierdo;
        this.derecho = derecho;
    }
}

public class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        return izquierdo.Evaluar(x) + derecho.Evaluar(x);
    }
}

public class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        return izquierdo.Evaluar(x) - derecho.Evaluar(x);
    }
}

public class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        return izquierdo.Evaluar(x) * derecho.Evaluar(x);
    }
}

public class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        int divisor = derecho.Evaluar(x);
        if (divisor == 0)
        {
            throw new DivideByZeroException("Error: División por cero.");
        }
        return izquierdo.Evaluar(x) / divisor;
    }
}