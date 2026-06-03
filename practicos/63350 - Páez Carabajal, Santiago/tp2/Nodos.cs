using System;

abstract class Nodo {
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
    private readonly Nodo _nodo;
    public NegativoNodo(Nodo nodo) => _nodo = nodo;
    public override int Evaluar(int x = 0) => -_nodo.Evaluar(x);
}

class PositivoNodo : Nodo {
    private readonly Nodo _nodo;
    public PositivoNodo(Nodo nodo) => _nodo = nodo;
    public override int Evaluar(int x = 0) => _nodo.Evaluar(x);
}

abstract class NodoBinario : Nodo {
    protected readonly Nodo Izq, Der;
    protected NodoBinario(Nodo izq, Nodo der) { Izq = izq; Der = der; }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) + Der.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) - Der.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) * Der.Evaluar(x);
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) {
        int divisor = Der.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("división por cero");
        return Izq.Evaluar(x) / divisor;
    }
}
