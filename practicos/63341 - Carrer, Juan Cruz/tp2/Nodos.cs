abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class ValorNumero : Nodo {

    private int numeroGuardado;

    public ValorNumero(int valor) {
        numeroGuardado = valor;
    }

    public override int Evaluar(int x = 0) {
        return numeroGuardado;
    }
}

class VariableX : Nodo {

    public override int Evaluar(int x = 0) {
        return x;
    }
}

class CambioSigno : Nodo {

    private Nodo contenido;

    public CambioSigno(Nodo nodo) {
        contenido = nodo;
    }

    public override int Evaluar(int x = 0) {
        return -contenido.Evaluar(x);
    }
}

abstract class OperacionBinaria : Nodo {

    protected Nodo izquierda;
    protected Nodo derecha;

    public OperacionBinaria(
        Nodo izq,
        Nodo der
    ) {

        izquierda = izq;
        derecha = der;
    }
}

class OperacionSuma : OperacionBinaria {

    public OperacionSuma(
        Nodo izq,
        Nodo der
    ) : base(izq, der) { }

    public override int Evaluar(int x = 0) {

        return izquierda.Evaluar(x)
            + derecha.Evaluar(x);
    }
}

class OperacionResta : OperacionBinaria {

    public OperacionResta(
        Nodo izq,
        Nodo der
    ) : base(izq, der) { }

    public override int Evaluar(int x = 0) {

        return izquierda.Evaluar(x)
            - derecha.Evaluar(x);
    }
}

class OperacionMultiplicacion : OperacionBinaria {

    public OperacionMultiplicacion(
        Nodo izq,
        Nodo der
    ) : base(izq, der) { }

    public override int Evaluar(int x = 0) {

        return izquierda.Evaluar(x)
            * derecha.Evaluar(x);
    }
}

class OperacionDivision : OperacionBinaria {

    public OperacionDivision(
        Nodo izq,
        Nodo der
    ) : base(izq, der) { }

    public override int Evaluar(int x = 0) {

        var divisor =
            derecha.Evaluar(x);

        if (divisor == 0) {
            throw new DivideByZeroException();
        }

        return izquierda.Evaluar(x)
            / divisor;
    }
}