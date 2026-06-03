using System;

abstract class Nodo {
    public abstract int Evaluar(int x);
}

class NumeroNodo : Nodo {
    private int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x) {
        return valor;
    }
}

class VariableNodo : Nodo {
    public override int Evaluar(int x) {
        return x;
    }
}

class NegativoNodo : Nodo {
    private Nodo nodo;

    public NegativoNodo(Nodo nodo) {
        this.nodo = nodo;
    }

    public override int Evaluar(int x) {
        return -nodo.Evaluar(x);
    }
}

abstract class NodoBinario : Nodo {
    protected Nodo izquierda;
    protected Nodo derecha;

    public NodoBinario(Nodo izquierda, Nodo derecha) {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) {
    }

    public override int Evaluar(int x) {
        return izquierda.Evaluar(x) + derecha.Evaluar(x);
    }
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) {
    }

    public override int Evaluar(int x) {
        return izquierda.Evaluar(x) - derecha.Evaluar(x);
    }
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) {
    }

    public override int Evaluar(int x) {
        return izquierda.Evaluar(x) * derecha.Evaluar(x);
    }
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) {
    }

    public override int Evaluar(int x) {
        int divisor = derecha.Evaluar(x);

        if (divisor == 0)
            throw new Exception("División por cero");

        return izquierda.Evaluar(x) / divisor;
    }
}
