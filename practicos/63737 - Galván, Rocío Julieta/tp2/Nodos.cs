namespace Calculadora;

public abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}

public class NumeroNodo : Nodo
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

public class VariableNodo : Nodo
{
    public override int Evaluar(int x = 0)
    {
        return x;
    }
}

public class NegativoNodo : Nodo
{
    public Nodo Operando { get; }

    public NegativoNodo(Nodo operando)
    {
        Operando = operando;
    }

    public override int Evaluar(int x = 0)
    {
        return -Operando.Evaluar(x);
    }
}

public abstract class NodoBinario : Nodo
{
    protected Nodo Izquierdo { get; }
    protected Nodo Derecho { get; }

    protected NodoBinario(Nodo izquierdo, Nodo derecho)
    {
        Izquierdo = izquierdo;
        Derecho = derecho;
    }
}

public class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izquierdo, Nodo derecho)
        : base(izquierdo, derecho)
    {
    }

    public override int Evaluar(int x = 0)
    {
        return Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
    }
}

public class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izquierdo, Nodo derecho)
        : base(izquierdo, derecho)
    {
    }

    public override int Evaluar(int x = 0)
    {
        return Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
    }
}

public class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izquierdo, Nodo derecho)
        : base(izquierdo, derecho)
    {
    }

    public override int Evaluar(int x = 0)
    {
        return Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
    }
}

public class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izquierdo, Nodo derecho)
        : base(izquierdo, derecho)
    {
    }

    public override int Evaluar(int x = 0)
    {
        int divisor = Derecho.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException("División por cero.");

        return Izquierdo.Evaluar(x) / divisor;
    }
}