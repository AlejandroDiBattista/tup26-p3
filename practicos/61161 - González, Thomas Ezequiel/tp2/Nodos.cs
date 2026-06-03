abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
// Nodo para números constantes
class NumeroNodo : Nodo {
    private int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) {
        return valor;
    }
}
// Nodo para la variable 'x'
class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }
}
// Nodo para la negación unaria 
class NegativoNodo : Nodo {
    private Nodo nodo;

    public NegativoNodo(Nodo nodo) {
        this.nodo = nodo;
    }

    public override int Evaluar(int x = 0) {
        return -nodo.Evaluar(x);
    }
}

// Nodo para operaciones binarias
abstract class NodoBinario : Nodo {
    protected Nodo izquierda;
    protected Nodo derecha;

    public NodoBinario(Nodo izquierda, Nodo derecha) {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }
}
// Nodo para la suma
class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) + derecha.Evaluar(x);
    }
}
// Nodo para la resta
class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) - derecha.Evaluar(x);
    }
}
// Nodo para la multiplicación
class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) * derecha.Evaluar(x);
    }
}
// Nodo para la división
class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        int divisor = derecha.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException();

        return izquierda.Evaluar(x) / divisor;
    }
}
