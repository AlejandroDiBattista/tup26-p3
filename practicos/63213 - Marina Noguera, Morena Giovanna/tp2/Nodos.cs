namespace TP2.Calculadora;
public abstract class Nodo 
{ 
    public abstract int Evaluar(int x); 
}

public class NumeroNodo(int valor) : Nodo 
{ 
    public override int Evaluar(int x) => valor; 
}

public class VariableNodo : Nodo 
{ 
    public override int Evaluar(int x) => x; 
}

public class NegativoNodo(Nodo interior) : Nodo
{
    public override int Evaluar(int x) => -interior.Evaluar(x);
}

public abstract class NodoBinario(Nodo izq, Nodo der) : Nodo 
{
    protected Nodo Izq = izq;
    protected Nodo Der = der;
}

public class SumaNodo(Nodo i, Nodo d) : NodoBinario(i, d) 
{
    public override int Evaluar(int x) => Izq.Evaluar(x) + Der.Evaluar(x);
}

public class RestaNodo(Nodo i, Nodo d) : NodoBinario(i, d) 
{
    public override int Evaluar(int x) => Izq.Evaluar(x) - Der.Evaluar(x);
}

public class MultiplicacionNodo(Nodo i, Nodo d) : NodoBinario(i, d) 
{
    public override int Evaluar(int x) => Izq.Evaluar(x) * Der.Evaluar(x);
}

public class DivisionNodo(Nodo i, Nodo d) : NodoBinario(i, d) 
{
    public override int Evaluar(int x) 
    {
        int divisor = Der.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("División por cero");
        return Izq.Evaluar(x) / divisor;
    }
}