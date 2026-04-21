
abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
class NumeroNodo : Nodo
{
    public int Valor { get; }

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

class PositivoNodo : Nodo
{
    public Nodo Hijo { get; }

    public PositivoNodo(Nodo hijo)
    {
        Hijo = hijo;
    }

    public override int Evaluar(int x = 0)
    {
        return Hijo.Evaluar(x);
    }
}

class NegativoNodo : Nodo
{
    public Nodo Hijo { get; }

    public NegativoNodo(Nodo hijo)
    {
        Hijo = hijo;
    }

    public override int Evaluar(int x = 0)
    {
        return -Hijo.Evaluar(x);
    }
}

abstract class NodoBinario : Nodo
{
    public Nodo Izquierdo { get; }
    public Nodo Derecho { get; }

    protected NodoBinario(Nodo izquierdo, Nodo derecho)
    {
        Izquierdo = izquierdo;
        Derecho = derecho;
    }
}

class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x = 0)
    {
        return Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
    }
}

class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x = 0)
    {
        return Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
    }
}

class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x = 0)
    {
        return Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
    }
}

class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x = 0)
    {
        int divisor = Derecho.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException("Intento de división por cero.");

        return Izquierdo.Evaluar(x) / divisor;
    }
}