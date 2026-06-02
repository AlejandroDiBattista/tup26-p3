abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class numeroNodo(int valor) : Nodo {    // override del metodo Evaluar para que devuelva el valor del numeroNodo cuando sea evaluado
                                        // aca x no importa, solo se evaluan los enteros 
    public override int Evaluar(int x = 0) {
        return valor;
    }
}

class variableNodo : Nodo {     // clase que cuando sea evaluada va devolver el valor de x (que lo da el usuario)
    public override int Evaluar(int x = 0) {
        return x;
    }
}

class negativo_positivoNodo(Nodo operando, bool esNegativo) : Nodo {
    public override int Evaluar(int x = 0) {
        var valor = operando.Evaluar(x);
        return esNegativo ? -valor : valor;
    }
}

abstract class NodoBinario(Nodo izquierda, Nodo derecha) : Nodo { //lee las 2 ramas del nodo 
    protected Nodo Izquierda => izquierda;                        // protected porque solo se va a usar en las clases que hereden de NodoBinario, no se va a usar fuera de esas clases
    protected Nodo Derecha => derecha;
}

class sumaNodo(Nodo izquierda, Nodo derecha) : NodoBinario(izquierda, derecha) {
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) + Derecha.Evaluar(x);
}

class restaNodo(Nodo izquierda, Nodo derecha) : NodoBinario(izquierda, derecha) {
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) - Derecha.Evaluar(x);
}

class multiplicacionNodo(Nodo izquierda, Nodo derecha) : NodoBinario(izquierda, derecha) {
    public override int Evaluar(int x = 0) => Izquierda.Evaluar(x) * Derecha.Evaluar(x);
}

class divisionNodo(Nodo izquierda, Nodo derecha) : NodoBinario(izquierda, derecha) {
    public override int Evaluar(int x = 0) {
        var divisor = Derecha.Evaluar(x);
        if (divisor == 0) throw new DivideByZeroException("División por cero.");
        return Izquierda.Evaluar(x) / divisor;
    }
}
