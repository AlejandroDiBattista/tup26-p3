abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo(int valor) : Nodo {
    public override int Evaluar(int x = 0) => valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

class NegativoNodo(Nodo operando) : Nodo {
    public override int Evaluar(int x = 0) => -operando.Evaluar(x);
}

class PositivoNodo(Nodo operando) : Nodo {
    public override int Evaluar(int x = 0) => operando.Evaluar(x);
}

abstract class NodoBinario(Nodo izquierdo, Nodo derecho) : Nodo {
    protected Nodo Izquierdo { get; } = izquierdo;
    protected Nodo Derecho { get; } = derecho;
}

class SumaNodo(Nodo izquierdo, Nodo derecho) : NodoBinario(izquierdo, derecho) {
    public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
}

class RestaNodo(Nodo izquierdo, Nodo derecho) : NodoBinario(izquierdo, derecho) {
    public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
}

class MultiplicacionNodo(Nodo izquierdo, Nodo derecho) : NodoBinario(izquierdo, derecho) {
    public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
}

class DivisionNodo(Nodo izquierdo, Nodo derecho) : NodoBinario(izquierdo, derecho) {
    public override int Evaluar(int x = 0) {
        int divisor = Derecho.Evaluar(x);
        if (divisor == 0)
            throw new DivideByZeroException("División por cero.");
        return Izquierdo.Evaluar(x) / divisor;
    }
}
