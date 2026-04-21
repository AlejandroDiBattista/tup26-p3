abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class Numero : Nodo
{
    private int valor;
    public Numero(int valor) => this.valor = valor;
    public override int Evaluar(int x = 0) => valor;
} 

class Variable : Nodo
{
    public override int Evaluer(int x = 0) => x;
}

class Negativo : Nodo
{
    private Nodo nodo;
    public Negativo(Nodo nodo) => this.nodo = nodo;
    public override int Evaluar(int x = 0) => -nodo.Evaluar(x);
}
