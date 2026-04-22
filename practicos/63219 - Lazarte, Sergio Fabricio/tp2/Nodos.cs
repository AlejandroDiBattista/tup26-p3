abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo
{
    private int valor;

    public NumeroNodo(int valor)
    {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) => valor;
}

class VariableNodo : Nodo
{
    public override int Evaluar(int x = 0) => x;
}

abstract class NodoBinario : Nodo
{
    protected Nodo izquierdo;
    protected Nodo derecho;

    public NodoBinario(Nodo izq, Nodo der)
    {
        izquierdo = izq;
        derecho = der;
    }
}