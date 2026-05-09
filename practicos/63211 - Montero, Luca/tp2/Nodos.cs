using System;

public abstract class Nodo
{
    public abstract int Evaluar(int x);
}

public class NumeroNodo : Nodo
{
    private readonly int _valor;
    public NumeroNodo(int valor) { _valor = valor; }
    public override int Evaluar(int x) => _valor;
}

public class VariableNodo : Nodo
{
    public override int Evaluar(int x) => x;
}

public class NegativoNodo : Nodo
{
    private readonly Nodo _nodo;
    public NegativoNodo(Nodo nodo) { _nodo = nodo; }
    public override int Evaluar(int x) => -_nodo.Evaluar(x);
}

public class PositivoNodo : Nodo
{
    private readonly Nodo _nodo;
    public PositivoNodo(Nodo nodo) { _nodo = nodo; }
    public override int Evaluar(int x) => _nodo.Evaluar(x);
}

public abstract class NodoBinario : Nodo
{
    protected Nodo Izquierdo;
    protected Nodo Derecho;
    protected NodoBinario(Nodo izquierdo, Nodo derecho)
    {
        Izquierdo = izquierdo;
        Derecho = derecho;
    }
}

public class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
}

public class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
}

public class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
}

public class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x)
    {
        int divisor = Derecho.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("Error: División por cero.");
        return Izquierdo.Evaluar(x) / divisor;
    }
}