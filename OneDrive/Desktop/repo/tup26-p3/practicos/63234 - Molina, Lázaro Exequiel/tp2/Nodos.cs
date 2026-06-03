abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NoNumero : Nodo {
    private int valor;

    public NoNumero(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) {
        return valor;
    }
}

class NoVariable : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }
}

class NoNegativo : Nodo {
    private Nodo nodo;

    public NoNegativo(Nodo nodo) {
        this.nodo = nodo;
    }

    public override int Evaluar(int x = 0) {
        return -nodo.Evaluar(x);
    }
}

abstract class NoBinario : Nodo {
    protected Nodo izquierda;
    protected Nodo derecha;

    public NoBinario(Nodo izq, Nodo der) {
        izquierda = izq;
        derecha = der;
    }
}

class NoSuma : NoBinario {
    public NoSuma(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) + derecha.Evaluar(x);
    }
}

class NoResta : NoBinario {
    public NoResta(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) - derecha.Evaluar(x);
    }
}

class NoMultiplicacion : NoBinario {
    public NoMultiplicacion(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        return izquierda.Evaluar(x) * derecha.Evaluar(x);
    }
}

class NoDivision : NoBinario {
    public NoDivision(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        int divisor = derecha.Evaluar(x);

        if (divisor == 0)
            throw new DivideByZeroException();

        return izquierda.Evaluar(x) / divisor;
    }
}
