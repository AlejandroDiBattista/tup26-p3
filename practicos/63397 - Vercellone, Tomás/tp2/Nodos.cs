abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    int valor;
    public NumeroNodo(int valor) { this.valor = valor; }
    public override int Evaluar(int x = 0) => valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

class NegativoNodo : Nodo {
    Nodo interior;
    public NegativoNodo(Nodo interior) { this.interior = interior; }
    public override int Evaluar(int x = 0) => -interior.Evaluar(x);
}

abstract class NodoBinario : Nodo {
    protected Nodo izquierdo;
    protected Nodo derecho;
    public NodoBinario(Nodo izquierdo, Nodo derecho) {
        this.izquierdo = izquierdo;
        this.derecho = derecho;
    }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) {}
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) + derecho.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) {}
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) - derecho.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) {}
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) * derecho.Evaluar(x);
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) {}
    public override int Evaluar(int x = 0) {
        var divisor = derecho.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("División por cero.");
        return izquierdo.Evaluar(x) / divisor;
    }
}