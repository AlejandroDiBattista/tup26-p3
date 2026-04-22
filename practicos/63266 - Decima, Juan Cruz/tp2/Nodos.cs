abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo
{

    private readonly int valor;

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
    private readonly Nodo operando;

    public NegativoNodo(Nodo operando)
    {
        this.operando = operando;
    }

    public override int Evaluar(int x = 0)
    {
        return -operando.Evaluar(x);
    }
}

abstract class NodoBinario : Nodo
{
    protected readonly Nodo izquierda;
    protected readonly Nodo derecha;

    protected NodoBinario(Nodo izquierda, Nodo derecha)
    {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }
}

class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0)
    {
        return izquierda.Evaluar(x) + derecha.Evaluar(x);
    }
}

class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0)
    {
        return izquierda.Evaluar(x) - derecha.Evaluar(x);
    }
}

