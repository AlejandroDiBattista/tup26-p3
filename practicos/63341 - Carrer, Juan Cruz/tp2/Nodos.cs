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