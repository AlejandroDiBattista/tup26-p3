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

abstract class NodoBinario(Nodo izq, Nodo der) : Nodo {
    protected int Izq(int x) => izq.Evaluar(x);
    protected int Der(int x) => der.Evaluar(x);
}

class SumaNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => Izq(x) + Der(x);
}

class RestaNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => Izq(x) - Der(x);
}

class MultiplicacionNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => Izq(x) * Der(x);
}

class DivisionNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) {
        var divisor = Der(x);
        if (divisor == 0) throw new DivideByZeroException("División por cero.");
        return Izq(x) / divisor;
    }
}
