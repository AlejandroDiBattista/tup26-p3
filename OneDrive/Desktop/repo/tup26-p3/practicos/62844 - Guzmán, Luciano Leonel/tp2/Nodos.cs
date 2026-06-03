abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
//--clase NumeroNodo--------------------------------
class NumeroNodo : Nodo {
    public int Valor { get; }

    public NumeroNodo(int valor) {
        Valor = valor;
    }

    public override int Evaluar(int x = 0) => Valor;
}
//-------clase VariableNodo--------------------------------
class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}
abstract class NodoUnario : Nodo {
    protected Nodo Operando;

    public NodoUnario(Nodo operando) {
        Operando = operando;
    }
}
class NegativoNodo : NodoUnario {
    public NegativoNodo(Nodo operando) : base(operando) { }
    public override int Evaluar(int x = 0) => -Operando.Evaluar(x);
}
class PositivoNodo : NodoUnario {
    public PositivoNodo(Nodo operando) : base(operando) { }
    public override int Evaluar(int x = 0) => Operando.Evaluar(x);
}
//--clase NodoBinario--------------------------------
abstract class NodoBinario : Nodo {
    protected Nodo Izq;
    protected Nodo Der;
    public NodoBinario(Nodo izq, Nodo der) {
        Izq = izq;
        Der = der;
    }
}

//--clase SumaNodo--------------------------------

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) + Der.Evaluar(x);
}

//--clase RestaNodo--------------------------------
class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) - Der.Evaluar(x);
}

//--clase MultiplicacionNodo--------------------------------

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) * Der.Evaluar(x);
}

//--clase DivisionNodo--------------------------------

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) {
        var divisor = Der.Evaluar(x);
        if (divisor == 0) {
            throw new DivideByZeroException("División por cero.");
        }
        return Izq.Evaluar(x) / divisor;
    }
}
