abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    private readonly int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) => valor;
    
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

class negativoNodo : Nodo {
    private readonly Nodo nodo;

    public negativoNodo(Nodo nodo) {
        this.nodo = nodo;
    }

    public override int Evaluar(int x = 0) => -nodo.Evaluar(x);
}

abstract class BinarioNodo : Nodo {
    protected readonly Nodo izquierda;
    protected readonly Nodo derecha;

    public BinarioNodo(Nodo izquierda, Nodo derecha) {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }
}