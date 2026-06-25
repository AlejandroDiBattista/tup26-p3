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
class SumaNodo(Nodo izquierda, Nodo derecha) : NodoBinario(izquierda, derecha) {
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) + Derecha.Evaluar(x);
}
class RestaNodo(Nodo izquierda, Nodo derecha) : NodoBinario(izquierda, derecha) {
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) - Derecha.Evaluar(x);
}
class MultiplicacionNodo(Nodo izquierda, Nodo derecha) : NodoBinario(izquierda, derecha) {
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) * Derecha.Evaluar(x);
}
class DivisionNodo(Nodo izquierda, Nodo derecha) : NodoBinario(izquierda, derecha) {
    public override int Evaluar(int x = 0) {
        var divisor = Derecha.Evaluar(x);
        if (divisor == 0)
            throw new DivideByZeroException("División por cero no permitida.");
        return Izquierda.Evaluar(x) / divisor;
    }
}
