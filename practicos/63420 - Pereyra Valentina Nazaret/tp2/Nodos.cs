abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    public int Valor;
    public NumeroNodo(int valor) => Valor = valor;

    public override int Evaluar(int x) => Valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x) => x;
}

class NegativoNodo : Nodo {
    Nodo Expr;
    public NegativoNodo(Nodo expr) => Expr = expr;

    public override int Evaluar(int x) => -Expr.Evaluar(x);
}

abstract class NodoBinario : Nodo {
    protected Nodo Izq, Der;

    public NodoBinario(Nodo izq, Nodo der) {
        Izq = izq;
        Der = der;
    }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo a, Nodo b) : base(a, b) { }
    public override int Evaluar(int x) => Izq.Evaluar(x) + Der.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo a, Nodo b) : base(a, b) { }
    public override int Evaluar(int x) => Izq.Evaluar(x) - Der.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo a, Nodo b) : base(a, b) { }
    public override int Evaluar(int x) => Izq.Evaluar(x) * Der.Evaluar(x);
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo a, Nodo b) : base(a, b) { }

    public override int Evaluar(int x) {
        int d = Der.Evaluar(x);
        if (d == 0) throw new Exception("División por cero");
        return Izq.Evaluar(x) / d;
    }
}
