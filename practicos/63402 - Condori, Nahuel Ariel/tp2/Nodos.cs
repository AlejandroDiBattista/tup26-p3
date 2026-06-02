using System;

public abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

public class NumeroNodo : Nodo {
    private readonly int valor;
    public NumeroNodo(int valor) { this.valor = valor; }
    public override int Evaluar(int x = 0) => valor;
}

public class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

public class PositivoNodo : Nodo {
    private readonly Nodo operando;
    public PositivoNodo(Nodo operando) { this.operando = operando; }
    public override int Evaluar(int x = 0) => operando.Evaluar(x);
}

public class NegativoNodo : Nodo {
    private readonly Nodo operando;
    public NegativoNodo(Nodo operando) { this.operando = operando; }
    public override int Evaluar(int x = 0) => -operando.Evaluar(x);
}

public abstract class NodoBinario : Nodo {
    protected Nodo Izquierdo { get; }
    protected Nodo Derecho { get; }
    protected NodoBinario(Nodo izq, Nodo der) {
        Izquierdo = izq;
        Derecho = der;
    }
}

public class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
}

public class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
}

public class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
}

public class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) {
        int divisor = Derecho.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("Error: División por cero.");
        return Izquierdo.Evaluar(x) / divisor;
    }
}
