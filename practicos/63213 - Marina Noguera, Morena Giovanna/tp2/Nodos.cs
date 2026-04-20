namespace TP2.Calculadora;
// Clase base para todos los nodos del árbol
public abstract class Nodo
{
    public abstract int Evaluar(int x);
}

// Nodo para números enteros (usando constructor primario)
public class NumeroNodo(int valor) : Nodo
{
    public override int Evaluar(int x) => valor;
}

// Nodo para la variable x
public class VariableNodo : Nodo
{
    public override int Evaluar(int x) => x;
}

// Clase base para operaciones binarias
public abstract class NodoBinario(Nodo izquierdo, Nodo derecho) : Nodo
{
    protected readonly Nodo Izq = izquierdo;
    protected readonly Nodo Der = derecho;
}

// Implementaciones de operaciones binarias simplificadas
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
    public override int Evaluar(int x) => Izq.Evaluar(x) / Der.Evaluar(x);
}

// Nodo para operador unario (negativo)
public class NegativoNodo(Nodo contenido) : Nodo
{
    public override int Evaluar(int x) => -contenido.Evaluar(x);
}