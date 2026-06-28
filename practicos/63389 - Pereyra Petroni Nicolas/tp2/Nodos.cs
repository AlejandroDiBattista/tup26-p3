abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
class NumeroNodo : Nodo {
    public int Valor;
    public NumeroNodo(int valor) {
        Valor = valor;
    }
    public override int Evaluar(int x = 0) {
        return Valor;
    }
}
class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }

}
class SumaNodo : Nodo {
    public Nodo Izquierdo;
    public Nodo Derecho;
    public SumaNodo(Nodo izq, Nodo der) {
        Izquierdo = izq;
        Derecho = der;
    }
    public override int Evaluar(int x = 0) {
        return Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
    }
}
class RestaNodo : Nodo {
    public Nodo Izquierdo;
    public Nodo Derecho;
    public RestaNodo(Nodo izq, Nodo der) {
        Izquierdo = izq;
        Derecho = der;
    }
    public override int Evaluar(int x = 0) {
        return Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
    }
}
class MultiplicacionNodo : Nodo {
    public Nodo Izquierdo;
    public Nodo Derecho;

    public MultiplicacionNodo(Nodo izq, Nodo der) {
        Izquierdo = izq;
        Derecho = der;
    }

    public override int Evaluar(int x = 0) {
        return Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
    }
}
class DivisionNodo : Nodo {
    public Nodo Izquierdo;
    public Nodo Derecho;

    public DivisionNodo(Nodo izq, Nodo der) {
        Izquierdo = izq;
        Derecho = der;
    }

    public override int Evaluar(int x = 0) {
        int divisor = Derecho.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException("División por cero");

        return Izquierdo.Evaluar(x) / divisor;
    }
}
class NegativoNodo : Nodo {
    public Nodo Expr;

    public NegativoNodo(Nodo expr) {
        Expr = expr;
    }

    public override int Evaluar(int x = 0) {
        return -Expr.Evaluar(x);
    }
}
