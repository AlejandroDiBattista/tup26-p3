namespace CalculadoraArimetica;

public abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    private readonly int _valor;
    public NumeroNodo(int valor) => _valor = valor;
    public override int Evaluar(int x = 0) => _valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

class NegativoNodo : Nodo {
    private readonly Nodo _hijo;
    public NegativoNodo(Nodo hijo) => _hijo = hijo;
    public override int Evaluar(int x = 0) => -_hijo.Evaluar(x);
}

public abstract class NodoBinario : Nodo {
    protected readonly Nodo Izquierda;
    protected readonly Nodo Derecha;
    protected NodoBinario(Nodo izq, Nodo der) {
        Izquierda = izq;
        Derecha = der;
    }
}