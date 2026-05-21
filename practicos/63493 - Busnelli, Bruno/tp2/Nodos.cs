abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NodoNumero : Nodo {
    private readonly int valor;

    public NodoNumero(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) {
        return valor;
    }
}

class NodoVariable : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }
}

class NodoBinario : Nodo {
    private readonly Nodo izquierda;
    private readonly Nodo derecha;
    private readonly char operador;

    public NodoBinario(Nodo izquierda, char operador, Nodo derecha) {
        this.izquierda = izquierda;
        this.operador = operador;
        this.derecha = derecha;
    }

    public override int Evaluar(int x = 0) {
        int a = izquierda.Evaluar(x);
        int b = derecha.Evaluar(x);

        return operador switch {
            '+' => a + b,
            '-' => a - b,
            '*' => a * b,
            '/' => a / b,
            _ => throw new InvalidOperationException("Operador inválido")
        };
    }
}

class NodoUnario : Nodo {
    private readonly char operador;
    private readonly Nodo nodo;

    public NodoUnario(char operador, Nodo nodo) {
        this.operador = operador;
        this.nodo = nodo;
    }

    public override int Evaluar(int x = 0) {
        int valor = nodo.Evaluar(x);

        return operador switch {
            '+' => valor,
            '-' => -valor,
            _ => throw new InvalidOperationException("Operador inválido")
        };
    }
}