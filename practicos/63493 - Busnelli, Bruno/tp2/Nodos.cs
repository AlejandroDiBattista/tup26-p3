abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NodoNumero : Nodo {
    private readonly int valor;

    public NodoNumero(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) {
        return valor;
    }
}

class NodoVariable : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }
}