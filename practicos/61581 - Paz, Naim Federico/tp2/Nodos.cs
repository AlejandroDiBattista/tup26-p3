abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    public int Valor { get; }

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

class PositivoNodo : Nodo {
    public Nodo Operando { get; }

    public PositivoNodo(Nodo operando) {
        Operando = operando;
    }

    public override int Evaluar(int x = 0) {
        return Operando.Evaluar(x);
    }
}

class NegativoNodo : Nodo {
    public Nodo Operando { get; }

    public NegativoNodo(Nodo operando) {
        Operando = operando;
    }

    public override int Evaluar(int x = 0) {
        return -Operando.Evaluar(x);
    }
}

abstract class NodoBinario : Nodo {
    public Nodo Izquierda { get; }
    public Nodo Derecha { get; }

    protected NodoBinario(Nodo izquierda, Nodo derecha) {
        Izquierda = izquierda;
        Derecha = derecha;
    }
}

class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0) {
        return Izquierda.Evaluar(x) + Derecha.Evaluar(x);
    }
}

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0) {
        return Izquierda.Evaluar(x) - Derecha.Evaluar(x);
    }
}

class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0) {
        return Izquierda.Evaluar(x) * Derecha.Evaluar(x);
    }
}

class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0) {
        var divisor = Derecha.Evaluar(x);

        if (divisor == 0) {
            throw new DivideByZeroException("División por cero.");
        }

        return Izquierda.Evaluar(x) / divisor;
    }
}