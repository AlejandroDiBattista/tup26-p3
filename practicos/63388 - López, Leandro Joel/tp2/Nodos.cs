abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    private readonly int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) => valor;
    
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}