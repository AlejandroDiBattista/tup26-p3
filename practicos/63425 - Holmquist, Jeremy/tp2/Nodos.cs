using System;

public abstract class Nodo {
    public abstract int Evaluar(int x);
}

public class NumeroNodo : Nodo {
    private int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x) {
        return valor;
    }

    public override string ToString() => $"NumeroNodo({valor})";
}

public class VariableNodo : Nodo {
    public override int Evaluar(int x) {
        return x;
    }

    public override string ToString() => "VariableNodo(x)";
}

public class NegativoNodo : Nodo {
    private Nodo operando;

    public NegativoNodo(Nodo operando) {
        this.operando = operando;
    }

    public override int Evaluar(int x) {
        return -operando.Evaluar(x);
    }

    public override string ToString() => $"NegativoNodo({operando})";
}

public class PositivoNodo : Nodo {
    private Nodo operando;

    public PositivoNodo(Nodo operando) {
        this.operando = operando;
    }

    public override int Evaluar(int x) {
        return operando.Evaluar(x);
    }

    public override string ToString() => $"PositivoNodo({operando})";
}

public abstract class NodoBinario : Nodo {
    protected Nodo izquierdo;
    protected Nodo derecho;

    public NodoBinario(Nodo izquierdo, Nodo derecho) {
        this.izquierdo = izquierdo;
        this.derecho = derecho;
    }
}

public class SumaNodo : NodoBinario {
    public SumaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x) {
        return izquierdo.Evaluar(x) + derecho.Evaluar(x);
    }

    public override string ToString() => $"SumaNodo({izquierdo}, {derecho})";
}

public class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x) {
        return izquierdo.Evaluar(x) - derecho.Evaluar(x);
    }

    public override string ToString() => $"RestaNodo({izquierdo}, {derecho})";
}

public class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x) {
        return izquierdo.Evaluar(x) * derecho.Evaluar(x);
    }

    public override string ToString() => $"MultiplicacionNodo({izquierdo}, {derecho})";
}

public class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izquierdo, Nodo derecho) : base(izquierdo, derecho) { }

    public override int Evaluar(int x) {
        int denominador = derecho.Evaluar(x);
        if (denominador == 0) {
            throw new DivideByZeroException("División por cero detectada");
        }
        return izquierdo.Evaluar(x) / denominador;
    }

    public override string ToString() => $"DivisionNodo({izquierdo}, {derecho})";
}
