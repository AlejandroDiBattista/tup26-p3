using System;

abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    int valor;
    public NumeroNodo(int n) { valor = n; }
    public override int Evaluar(int x) => valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x) => x;
}

class NegativoNodo : Nodo {
    Nodo interno;
    public NegativoNodo(Nodo n) { interno = n; }
    public override int Evaluar(int x) => -interno.Evaluar(x);
}

abstract class NodoBinario : Nodo {
    protected Nodo izq, der;
    public NodoBinario(Nodo a, Nodo b) {
        izq = a;
        der = b;
    }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo a, Nodo b) : base(a, b) { }
    public override int Evaluar(int x) => izq.Evaluar(x) + der.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo a, Nodo b) : base(a, b) { }
    public override int Evaluar(int x) => izq.Evaluar(x) - der.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo a, Nodo b) : base(a, b) { }
    public override int Evaluar(int x) => izq.Evaluar(x) * der.Evaluar(x);
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo a, Nodo b) : base(a, b) { }
    public override int Evaluar(int x) {
        int d = der.Evaluar(x);
    }
