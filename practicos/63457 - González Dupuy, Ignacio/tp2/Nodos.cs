abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    private int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) {
        return valor;
    }
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }
}

class NegativoNodo : Nodo {
    private Nodo nodo;

    public NegativoNodo(Nodo nodo) {
        this.nodo = nodo;
    }

    public override int Evaluar(int x = 0) {
        return -nodo.Evaluar(x);
    }
}
class SumaNodo : Nodo {
    private Nodo izquierda;
    private Nodo derecha;

    public SumaNodo(Nodo izquierda, Nodo derecha) {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) + derecha.Evaluar(x);
    }
}

class RestaNodo : Nodo {
    private Nodo izquierda;
    private Nodo derecha;

    public RestaNodo(Nodo izquierda, Nodo derecha) {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) - derecha.Evaluar(x);
    }
}

class MultiplicacionNodo : Nodo {
    private Nodo izquierda;
    private Nodo derecha;

    public MultiplicacionNodo(Nodo izquierda, Nodo derecha) {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) * derecha.Evaluar(x);
    }
}

class DivisionNodo : Nodo {
    private Nodo izquierda;
    private Nodo derecha;

    public DivisionNodo(Nodo izquierda, Nodo derecha) {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }

    public override int Evaluar(int x = 0) {
        int divisor = derecha.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException();

        return izquierda.Evaluar(x) / divisor;
    }
}
