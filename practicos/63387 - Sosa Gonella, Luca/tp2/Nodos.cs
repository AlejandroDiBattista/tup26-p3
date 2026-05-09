abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    private int _valor;
    public NumeroNodo(int valor) { _valor = valor; }
    public override int Evaluar(int x = 0) => _valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

class NegativoNodo : Nodo {
    private Nodo _expr;
    public NegativoNodo(Nodo expr) { _expr = expr; }
    public override int Evaluar(int x = 0) => -_expr.Evaluar(x);
}

abstract class NodoBinario : Nodo {
    protected Nodo Izq;
    protected Nodo Der;

    public NodoBinario(Nodo izq, Nodo der) {
        Izq = izq;
        Der = der;
    }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) {}
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) + Der.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) {}
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) - Der.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) {}
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) * Der.Evaluar(x);
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) {}

    public override int Evaluar(int x = 0) {
        int divisor = Der.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException();

        return Izq.Evaluar(x) / divisor;
    }
}