abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
class NumeroNodo(int valor) : Nodo {
    public override int Evaluar(int x = 0) => valor;
}
class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}
class NegativoNodo(Nodo operando) : Nodo {
    public override int Evaluar(int x = 0) => -operando.Evaluar(x);
}
abstract class NodoBinario(Nodo izquierda, Nodo derecha) : Nodo {
    protected readonly Nodo Izquierda = izquierda;
    protected readonly Nodo Derecha = derecha;
}