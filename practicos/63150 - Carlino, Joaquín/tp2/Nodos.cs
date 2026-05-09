namespace calculadora;

// ─── Nodo base ────────────────────────────────────────────────────────────────

abstract class Nodo
{
    public abstract int Evaluar(int x = 0);
}

// ─── Hoja: número literal ────────────────────────────────────────────────────

class NumeroNodo(int valor) : Nodo
{
    public override int Evaluar(int x = 0) => valor;
    public override string ToString() => valor.ToString();
}

// ─── Hoja: variable x ───────────────────────────────────────────────────────

class VariableNodo : Nodo
{
    public override int Evaluar(int x = 0) => x;
    public override string ToString() => "x";
}

// ─── Unario: negación ────────────────────────────────────────────────────────

class NegativoNodo(Nodo operando) : Nodo
{
    public override int Evaluar(int x = 0) => -operando.Evaluar(x);
    public override string ToString() => $"(-{operando})";
}

// ─── Nodos binarios ──────────────────────────────────────────────────────────

abstract class NodoBinario(Nodo izq, Nodo der) : Nodo
{
    protected readonly Nodo Izq = izq;
    protected readonly Nodo Der = der;
}

class SumaNodo(Nodo izq, Nodo der) : NodoBinario(izq, der)
{
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) + Der.Evaluar(x);
    public override string ToString() => $"({Izq} + {Der})";
}

class RestaNodo(Nodo izq, Nodo der) : NodoBinario(izq, der)
{
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) - Der.Evaluar(x);
    public override string ToString() => $"({Izq} - {Der})";
}

class MultiplicacionNodo(Nodo izq, Nodo der) : NodoBinario(izq, der)
{
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) * Der.Evaluar(x);
    public override string ToString() => $"({Izq} * {Der})";
}

class DivisionNodo(Nodo izq, Nodo der) : NodoBinario(izq, der)
{
    public override int Evaluar(int x = 0)
    {
        int divisor = Der.Evaluar(x);
        if (divisor == 0)
            throw new DivisionPorCeroException();
        return Izq.Evaluar(x) / divisor;
    }
    public override string ToString() => $"({Izq} / {Der})";
}

// ─── Excepciones propias ─────────────────────────────────────────────────────

class ErrorDeParsing(string mensaje) : Exception(mensaje);
class DivisionPorCeroException() : Exception("Error: división por cero.");