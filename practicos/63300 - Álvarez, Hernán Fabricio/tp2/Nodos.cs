using System;



public abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

public class NumeroNodo : Nodo {
    public int Valor { get; }
    public NumeroNodo(int valor) => Valor = valor;
    public override int Evaluar(int x = 0) => Valor;
}

public class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

public class PositivoNodo : Nodo {
    public Nodo Hijo { get; }
    public PositivoNodo(Nodo hijo) => Hijo = hijo;
    public override int Evaluar(int x = 0) => Hijo.Evaluar(x);
}

public class NegativoNodo : Nodo {
    public Nodo Hijo { get; }
    public NegativoNodo(Nodo hijo) => Hijo = hijo;
    public override int Evaluar(int x = 0) => -Hijo.Evaluar(x);
}

public abstract class NodoBinario : Nodo {
    public Nodo Izquierdo { get; }
    public Nodo Derecho { get; }
    protected NodoBinario(Nodo izquierdo, Nodo derecho) {
        Izquierdo = izquierdo;
        Derecho = derecho;
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
