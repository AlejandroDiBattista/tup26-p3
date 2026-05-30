abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
class NodoNumero : Nodo {
    private int valor;

    public NodoNumero(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) {
        return valor;
    }
}
class NodoUnario : Nodo {
    private char operador;
    private Nodo operando;

    public NodoUnario(char operador, Nodo operando) {
        this.operador = operador;
        this.operando = operando;
    }

    public override int Evaluar(int x) {
        int valor = operando.Evaluar(x);

        if (operador == '+')
            return valor;

        if (operador == '-')
            return -valor;

        throw new InvalidOperationException("Operador unario inválido");
    }
}
class NodoVariable : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }
}
class NodoNegativo : Nodo {
    private Nodo hijo;

    public NodoNegativo(Nodo hijo) {
        this.hijo = hijo;
    }

    public override int Evaluar(int x = 0) {
        return -hijo.Evaluar(x);
    }
}

class NodoPositivo : Nodo {
    private Nodo hijo;

    public NodoPositivo(Nodo hijo) {
        this.hijo = hijo;
    }

    public override int Evaluar(int x = 0) {
        return hijo.Evaluar(x);
    }
}

abstract class NodoOperacion : Nodo {
    protected Nodo izquierdo;
    protected Nodo derecho;

    public NodoOperacion(Nodo izquierdo, Nodo derecho) {
        this.izquierdo = izquierdo;
        this.derecho = derecho;
    }
}

class NodoSuma : NodoOperacion {
    public NodoSuma(Nodo izq, Nodo der) : base(izq, der) {}

    public override int Evaluar(int x = 0) {
        return izquierdo.Evaluar(x) + derecho.Evaluar(x);
    }
}

class NodoResta : NodoOperacion {
    public NodoResta(Nodo izq, Nodo der) : base(izq, der) {}

    public override int Evaluar(int x = 0) {
        return izquierdo.Evaluar(x) - derecho.Evaluar(x);
    }
}

class NodoMultiplicacion : NodoOperacion {
    public NodoMultiplicacion(Nodo izq, Nodo der) : base(izq, der) {}

    public override int Evaluar(int x = 0) {
        return izquierdo.Evaluar(x) * derecho.Evaluar(x);
    }
}

class NodoDivision : NodoOperacion {
    public NodoDivision(Nodo izq, Nodo der) : base(izq, der) {}

    public override int Evaluar(int x = 0) {
        var divisor = derecho.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException("División por cero");

        return izquierdo.Evaluar(x) / divisor;
    }
}