abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}

class NoNumero : Nodo
{
    private int valor;

    public NoNumero(int valor)
    {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0)
    {
        return valor;
    }
}

class NoVariable : Nodo
{
    public override int Evaluar(int x = 0)
    {
        return x;
    }
}

class NoNegativo : Nodo
{
    private Nodo nodo;

    public NoNegativo(Nodo nodo)
    {
        this.nodo = nodo;
    }

    public override int Evaluar(int x = 0)
    {
        return -nodo.Evaluar(x);
    }
}

abstract class NoBinario : Nodo
{
    protected Nodo izquierda;
    protected Nodo derecha;

    public NoBinario(Nodo izq, Nodo der)
    {
        izquierda = izq;
        derecha = der;
    }
}