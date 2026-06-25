namespace Calculadora;

public abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

public class NumeroNodo(int valor) : Nodo {
    public override int Evaluar(int x = 0) => valor;
}

public class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

public class NegativoNodo(Nodo interior) : Nodo {
    public override int Evaluar(int x = 0) => -interior.Evaluar(x);
}

public class PositivoNodo(Nodo interior) : Nodo {
    public override int Evaluar(int x = 0) => interior.Evaluar(x);
}

public abstract class NodoBinario(Nodo izquierdo, Nodo derecho) : Nodo {
    protected readonly Nodo izq = izquierdo;
    protected readonly Nodo der = derecho;
}

public class SumaNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => izq.Evaluar(x) + der.Evaluar(x);
}

public class RestaNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => izq.Evaluar(x) - der.Evaluar(x);
}

public class MultiplicacionNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => izq.Evaluar(x) * der.Evaluar(x);
}

public class DivisionNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) {
        int divisor = der.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("Error: División por cero.");
        return izq.Evaluar(x) / divisor;
    }
}