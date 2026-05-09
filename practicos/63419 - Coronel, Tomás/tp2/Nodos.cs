
abstract class Nodo 

{
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo
{
    int Valor;
    public NumeroNodo(int v) { Valor = v; }
    public override int Evaluar(int x = 0) => Valor;
}

class VariableNodo : Nodo
{
    public override int Evaluar(int x = 0) => x;
}

class Negativo : Nodo
{
    Nodo nodo;
    public Negativo(Nodo n) { nodo = n; }
    public override int Evaluar(int x = 0) => -nodo.Evaluar(x);
}

abstract class NodoBinario : Nodo
{
    protected Nodo Izq;
    protected Nodo Der;

public NodoBinario(Nodo i, Nodo d)
    {
        Izq = i;
        Der = d;
    }


}

class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) + Der.Evaluar(x);
}

class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) - Der.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) * Der.Evaluar(x);
}

class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0)
    {
        int divisor = Der.Evaluar(x);
        if (divisor == 0) throw new Exception("División por cero");
        return Izq.Evaluar(x) / divisor;
    }
}