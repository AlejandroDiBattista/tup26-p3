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

    public override int Evaluar(int x = 0)
    {
        return valor;
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
    private Nodo interno;

    public NegativoNodo(Nodo nodo)
    {
        interno = nodo;
    }

    public override int Evaluar(int x = 0)
    {
        return -interno.Evaluar(x);
    }
}
abstract class NodoBinario : Nodo
{
    protected Nodo izq;
    protected Nodo der;

    public NodoBinario(Nodo izq, Nodo der)
    {
        this.izq = izq;
        this.der = der;
    }
}
class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo i, Nodo d) : base(i, d) { }

    public override int Evaluar(int x = 0)
    {
        return izq.Evaluar(x) + der.Evaluar(x);
    }
}

class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo i, Nodo d) : base(i, d) { }

    public override int Evaluar(int x = 0)
    {
        return izq.Evaluar(x) - der.Evaluar(x);
    }
}
class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }

    public override int Evaluar(int x = 0)
    {
        return izq.Evaluar(x) * der.Evaluar(x);
    }
}

class DivisionNodo : NodoBinario
{ public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }

    public override int Evaluar(int x = 0)
    {
        int divisor = der.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException("División por cero");

        return izq.Evaluar(x) / divisor;
    }
}