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

class Suma : Binario
{
    public Suma(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => izq.Evaluar(x) + der.Evaluar(x);
}

class Resta : Binario
{
    public Resta(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => izq.Evaluar(x) - der.Evaluar(x);
}

class Multiplicacion : Binario
{
    public Multiplicacion(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x = 0) => izq.Evaluar(x) * der.Evaluar(x);
}

class Division : Binario
{
    public Division(Nodo i, Nodo d) : base(i, d) { }

    public override int Evaluar(int x = 0)
    {
        var division = der.Evaluar(x);
        if (division == 0) throw new DivideByZeroException();
        return izq.Evaluar(x) / division;
    }
}