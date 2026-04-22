using System;

abstract class Nodo {
    public abstract int Evaluar(int x);
}

class NumeroNodo : Nodo {
    private int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x) => valor;
}

class VariableNodo : Nodo {
    public override int Evaluar(int x) => x;
}

