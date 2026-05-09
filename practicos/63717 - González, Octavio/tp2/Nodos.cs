using System;

// Esta es la clase padre de todos los nodos
public abstract class Nodo
{
    // Todos los nodos tienen que tener este metodo para sacar la cuenta
    // Si no le pasamos x, vale 0 por defecto
    public abstract int Evaluar(int x = 0);
}

// Este nodo es para cuando hay un numero solo, como el 5 o el 10
public class NumeroNodo : Nodo
{
    public int MiNumero;

    public NumeroNodo(int numero)
    {
        MiNumero = numero;
    }

    public override int Evaluar(int x)
    {
        // Aca solo devolvemos el numero que guardamos
        return MiNumero;
    }
}

// Este es para cuando aparece la letra x en la cuenta
public class VariableNodo : Nodo
{
    public override int Evaluar(int x)
    {
        // Devolvemos el valor de x que nos pasaron
        return x;
    }
}

// Este es para cuando hay un signo menos adelante, como -5 o -(x+2)
public class NegativoNodo : Nodo
{
    public Nodo Hijo;

    public NegativoNodo(Nodo h)
    {
        Hijo = h;
    }

    public override int Evaluar(int x)
    {
        // Evaluamos lo de adentro y le cambiamos el signo
        return -Hijo.Evaluar(x);
    }
}

// Esta clase es para operaciones que necesitan dos cosas (izquierda y derecha)
public abstract class NodoBinario : Nodo
{
    public Nodo Izquierda;
    public Nodo Derecha;

    public NodoBinario(Nodo izq, Nodo der)
    {
        Izquierda = izq;
        Derecha = der;
    }
}

public class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x)
    {
        return Izquierda.Evaluar(x) + Derecha.Evaluar(x);
    }
}

public class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x)
    {
        return Izquierda.Evaluar(x) - Derecha.Evaluar(x);
    }
}

public class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x)
    {
        return Izquierda.Evaluar(x) * Derecha.Evaluar(x);
    }
}

public class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x)
    {
        int divisor = Derecha.Evaluar(x);
        if (divisor == 0)
        {
            throw new DivideByZeroException("Error: No podes dividir por cero!");
        }
        return Izquierda.Evaluar(x) / divisor;
    }
}
