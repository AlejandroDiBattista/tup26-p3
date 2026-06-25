abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    public int Valor;
    public NumeroNodo(int valor) => Valor = valor;
    public override int Evaluar(int x = 0) => Valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

class NegativoNodo : Nodo {
    public Nodo Expr;
    public NegativoNodo(Nodo expr) => Expr = expr;
    public override int Evaluar(int x = 0) => -Expr.Evaluar(x);
}

abstract class NodoBinario : Nodo {
    public Nodo Izq, Der;
    public NodoBinario(Nodo izq, Nodo der) {
        Izq = izq;
        Der = der;
    }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) + Der.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) - Der.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) * Der.Evaluar(x);
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }

    public override int Evaluar(int x = 0) {
        int der = Der.Evaluar(x);
        if (der == 0) throw new Exception("División por cero");
        return Izq.Evaluar(x) / der;
    }
}
