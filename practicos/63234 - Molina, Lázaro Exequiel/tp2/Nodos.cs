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
