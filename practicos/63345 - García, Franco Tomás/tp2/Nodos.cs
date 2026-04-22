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
