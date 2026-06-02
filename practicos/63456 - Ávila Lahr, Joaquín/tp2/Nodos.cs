abstract class Nodo {
    public abstract int Evaluar(int x);
}
class NumeroNodo : Nodo {
    public int Valor;
    public NumeroNodo(int valor) => Valor = valor;
    public override int Evaluar(int x) => Valor;
}
class VariableNodo : Nodo {
    public override int Evaluar(int x) => x;
}
class NegativoNodo : Nodo {
    public Nodo Expr;
    public NegativoNodo(Nodo expr) => Expr = expr;
    public override int Evaluar(int x) => -Expr.Evaluar(x);
}
abstract class NodoBinario : Nodo {
    public Nodo Izq;
    public Nodo Der;

    public NodoBinario(Nodo izq, Nodo der) {
        Izq = izq;
        Der = der;
    }
}
class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izq.Evaluar(x) + Der.Evaluar(x);
}
class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izq.Evaluar(x) - Der.Evaluar(x);
}
class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izq.Evaluar(x) * Der.Evaluar(x);
}
class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) {
        int divisor = Der.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException("División por cero");

        return Izq.Evaluar(x) / divisor;
    }
}
