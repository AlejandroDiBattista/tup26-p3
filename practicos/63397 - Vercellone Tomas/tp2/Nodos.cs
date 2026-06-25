abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
 

class NumeroNodo(int valor) : Nodo {
    public override int Evaluar(int x = 0) => valor;
}
 class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

class NegativoNodo(Nodo hijo) : Nodo {
    public override int Evaluar(int x = 0) => -hijo.Evaluar(x);
}
 
 
abstract class NodoBinario(Nodo izq, Nodo der) : Nodo {
    protected Nodo izquierdo = izq;
    protected Nodo derecho = der;
}


class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) + derecho.Evaluar(x);
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) - derecho.Evaluar(x);
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) * derecho.Evaluar(x);
}

class DivisionNodo : NodoBinario {

    public DivisionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) {}

 
class SumaNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) + derecho.Evaluar(x);
}
 
class RestaNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) - derecho.Evaluar(x);
}
 
class MultiplicacionNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {
    public override int Evaluar(int x = 0) => izquierdo.Evaluar(x) * derecho.Evaluar(x);
}
 
class DivisionNodo(Nodo izq, Nodo der) : NodoBinario(izq, der) {


    public DivisionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x = 0) {
        int divisor = derecho.Evaluar(x);
        if (divisor == 0)
            throw new DivideByZeroException("División por cero.");
        return izquierdo.Evaluar(x) / divisor;
    }
}
