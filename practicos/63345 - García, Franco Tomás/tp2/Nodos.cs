class Numero : Nodo
{
    private int valor;
    public Numero(int valor) => this.valor = valor;
    public override int Evaluar(int x = 0) => valor;
} 

class Variable : Nodo
{
    public override int Evaluar(int x = 0) => x;
}
class Negativo : Nodo
{
    private Nodo nodo;
    public Negativo(Nodo nodo) => this.nodo = nodo;
    public override int Evaluar(int x = 0) => -nodo.Evaluar(x);
}
abstract class Binario : Nodo
{
    protected Nodo izq, der;
    public Binario(Nodo izq, Nodo der)
    {
        this.izq = izq;
        this.der = der;
    }
}