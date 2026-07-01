using System;

abstract class Nodo{
    public abstract int Evaluar(int x);
}

class NumeroNodo : Nodo{
    private int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x) => valor;
   
}

class VariableNodo : Nodo{
    public override int Evaluar(int x) => x;
};

class NegativoNodo : Nodo{
    private Nodo nodo;

    public NegativoNodo(Nodo nodo) {
        this.nodo = nodo;
    }

    public override int Evaluar(int x) => -nodo.Evaluar(x); 
}

abstract class NodoBinario : Nodo{
    protected Nodo izquierda;
    protected Nodo derecha;

    public NodoBinario (Nodo izquiera, Nodo derecha){
        this.izquierda = izquiera;
        this.derecha = derecha;   
    }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x) =>
        izquierda.Evaluar(x) + derecha.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x) =>
        izquierda.Evaluar(x) - derecha.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x) =>
        izquierda.Evaluar(x) * derecha.Evaluar(x);
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x) {
        int divisor = derecha.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException();

        return izquierda.Evaluar(x) / divisor;
    }
}
