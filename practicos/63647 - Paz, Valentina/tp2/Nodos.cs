public abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}

public class NumeroNodo : Nodo
{
    private int _numero;

    public NumeroNodo(int numero)
    {
        _numero = numero;
    }

    public override int Evaluar(int x = 0)
    {
        return _numero;
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
    private Nodo _operando;

    public NegativoNodo(Nodo operando)
    {
        _operando = operando;
    }

    public override int Evaluar(int x = 0)
    {
        return -_operando.Evaluar(x);
    }
}

public abstract class NodoBinario : Nodo
{
    protected Nodo _izquierdo;
    protected Nodo _derecho;

    public NodoBinario(Nodo izquierdo, Nodo derecho)
    {
        _izquierdo = izquierdo;
        _derecho = derecho;
    }
}

public class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho)
    {
    }

    public override int Evaluar(int x = 0)
    {
        return _izquierdo.Evaluar(x) + _derecho.Evaluar(x);
    }
}

public class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho)
    {
    }

    public override int Evaluar(int x = 0)
    {
        return _izquierdo.Evaluar(x) - _derecho.Evaluar(x);
    }
}

public class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho)
    {
    }

    public override int Evaluar(int x = 0)
    {
        return _izquierdo.Evaluar(x) * _derecho.Evaluar(x);
    }
}

public class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho)
    {
    }

    public override int Evaluar(int x = 0)
    {
        int divisor = _derecho.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException("División por cero.");

        return _izquierdo.Evaluar(x) / divisor;
    }
}