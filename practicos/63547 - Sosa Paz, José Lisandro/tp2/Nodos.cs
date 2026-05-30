// Nodos.cs
using System;

public abstract class Nodo {
    public abstract int Evaluar(int x);
}

public class NumeroNodo : Nodo {
    private readonly int _valor;
    public NumeroNodo(int valor) => _valor = valor;
    public override int Evaluar(int x) => _valor;
}

public class VariableNodo : Nodo {
    public override int Evaluar(int x) => x;
}

public class NegativoNodo : Nodo {
    private readonly Nodo _nodo;
    public NegativoNodo(Nodo nodo) => _nodo = nodo;
    public override int Evaluar(int x) => -_nodo.Evaluar(x);
}

public abstract class NodoBinario : Nodo {
    protected readonly Nodo Izquierda;
    protected readonly Nodo Derecha;
    public NodoBinario(Nodo izq, Nodo der) {
        Izquierda = izq;
        Derecha = der;
    }
}

public class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierda.Evaluar(x) + Derecha.Evaluar(x);
}

public class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierda.Evaluar(x) - Derecha.Evaluar(x);
}

public class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierda.Evaluar(x) * Derecha.Evaluar(x);
}

public class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) {
        int divisor = Derecha.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("Error: División por cero.");
        return Izquierda.Evaluar(x) / divisor;
    }
}
