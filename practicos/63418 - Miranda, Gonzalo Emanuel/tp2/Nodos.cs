namespace TP2_GONZALO_CALCULADORA;

// Clase base para todos los nodos del árbol
public abstract class Nodo
{
    public abstract int Evaluar(int x);
}

// Nodo para números enteros
public class NumeroNodo : Nodo
{
    private readonly int _valor;
    public NumeroNodo(int valor) => _valor = valor;
    public override int Evaluar(int x) => _valor;
}

// Nodo para la variable x
public class VariableNodo : Nodo
{
    public override int Evaluar(int x) => x;
}

// Clase base para operaciones con dos hijos
public abstract class NodoBinario : Nodo
{
    protected Nodo Izquierdo;
    protected Nodo Derecho;

    protected NodoBinario(Nodo izquierdo, Nodo derecho)
    {
        Izquierdo = izquierdo;
        Derecho = derecho;
    }
}

// Implementaciones de operaciones binarias
public class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
}

public class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
}

public class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
}

public class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) / Derecho.Evaluar(x);
}

// Nodo para operador unario (negativo)
public class NegativoNodo : Nodo
{
    private readonly Nodo _contenido;
    public NegativoNodo(Nodo contenido) => _contenido = contenido;
    public override int Evaluar(int x) => -_contenido.Evaluar(x);
}