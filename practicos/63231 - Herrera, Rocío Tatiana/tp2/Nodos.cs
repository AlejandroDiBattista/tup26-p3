abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

public abstract class Expresion
{ public abstract int Calcular(int x=0);
}
 
public class constante : Expresion
{
    private readonly int valor;
    public constante(int valor) => valor=valor;
    public override int Calcular(int x=0) => valor;
}

public class variable : Expresion
{
    public override int Calcular (int x=0) => x;
}