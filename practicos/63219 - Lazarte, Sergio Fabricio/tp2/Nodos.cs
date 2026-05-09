abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo
{
    private int valor;

    public NumeroNodo(int valor)
    {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) => valor;
}

class VariableNodo : Nodo
{
    public override int Evaluar(int x = 0) => x;
}

abstract class NodoBinario : Nodo
{
    protected Nodo izquierdo;
    protected Nodo derecho;

    public NodoBinario(Nodo izq, Nodo der)
    {
        izquierdo = izq;
        derecho = der;
    }
}

class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
        => izquierdo.Evaluar(x) + derecho.Evaluar(x);
}

class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
        => izquierdo.Evaluar(x) - derecho.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
        => izquierdo.Evaluar(x) * derecho.Evaluar(x);
}

class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0)
    {
        int divisor = derecho.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException();

        return izquierdo.Evaluar(x) / divisor;
    }
}

class NegativoNodo : Nodo
{
    private Nodo hijo;

    public NegativoNodo(Nodo hijo)
    {
        this.hijo = hijo;
    }

    public override int Evaluar(int x = 0)
        => -hijo.Evaluar(x);
}