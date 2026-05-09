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

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) + Derecha.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) - Derecha.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) * Derecha.Evaluar(x);
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) {
        int divisor = Derecha.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("¡Error! No se puede dividir por cero.");
        return Izquierda.Evaluar(x) / divisor;
    }
}