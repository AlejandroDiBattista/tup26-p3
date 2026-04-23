abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo(int valor) : Nodo {
    public override int Evaluar(int x = 0) => valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}
