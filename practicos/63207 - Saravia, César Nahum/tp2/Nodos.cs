abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    private int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) {
        return valor;
    }
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }
}

class NegativoNodo : Nodo {
    private Nodo nodo;
    public NegativoNodo(Nodo nodo) {
        this.nodo = nodo;
    }
    public override int Evaluar(int x = 0) {
        return -nodo.Evaluar(x);
    }
}
abstract class NodoBinario : Nodo {
    protected Nodo izquierdo;
    protected Nodo derecho;
    public NodoBinario(Nodo izquierdo, Nodo derecho) {
        this.izquierdo = izquierdo;
        this.derecho = derecho;
    }
}