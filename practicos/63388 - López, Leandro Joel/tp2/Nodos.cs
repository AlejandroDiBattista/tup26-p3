abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class NumeroNodo : Nodo {
    private readonly int valor;

    public NumeroNodo(int valor) {
        this.valor = valor;
    }

    public override int Evaluar(int x = 0) => valor;
    
}

class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}

class negativoNodo : Nodo {
    private readonly Nodo nodo;

    public negativoNodo(Nodo nodo) {
        this.nodo = nodo;
    }

    public override int Evaluar(int x = 0) => -nodo.Evaluar(x);
}

abstract class BinarioNodo : Nodo {
    protected readonly Nodo izquierda;
    protected readonly Nodo derecha;

    public BinarioNodo(Nodo izquierda, Nodo derecha) {
        this.izquierda = izquierda;
        this.derecha = derecha;
    }
}

class SumaNodo : BinarioNodo {
    public SumaNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0) => izquierda.Evaluar(x) + derecha.Evaluar(x);
}

class RestaNodo : BinarioNodo {
    public RestaNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0) => izquierda.Evaluar(x) - derecha.Evaluar(x);
}

class MultiplicacionNodo : BinarioNodo {
    public MultiplicacionNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0) => izquierda.Evaluar(x) * derecha.Evaluar(x);
}

class DivisionNodo : BinarioNodo {
    public DivisionNodo(Nodo izquierda, Nodo derecha) : base(izquierda, derecha) { }

    public override int Evaluar(int x = 0) => izquierda.Evaluar(x) / derecha.Evaluar(x);

    private Nodo Factor() {

    if (Match('+')) return Factor();
    if (Match('-')) return new negativoNodo(Factor());
       
    if (Match('(')) {
        Nodo nodo = Expresion();
        Expect(')');

        return nodo;
    }

    if (Peek() == 'x' || Peek() == 'X') {
        Posicion++;
        return new VariableNodo();
    }


    if (char.IsDigit(Peek())) {
        
        return new NumeroNodo(valor);

        throw new Exception($"Carácter inesperado: {Peek()}");
    }
    
}

private Nodo Numero() {

    int inicio = Posicion;

    while (Posicion < texto.Length && char.IsDigit(Peek()))
        Posicion++;
    
    return new NumeroNodo(int.Parse(texto.Substring(inicio, Posicion - inicio)));
}

private char Peek() => Posicion < texto.Length ? texto[Posicion] : '\0';
private bool Match(char expected) {
    
    if (Peek() == expected) {
        Posicion++;
        return true;
    }
    return false;

}

public Nodo Parse(string entrada) {
    
    if (string.IsNullOrWhiteSpace(entrada))
        throw new ArgumentException("La expresión no puede estar vacía.");

    texto = entrada;
    Posicion = 0;

    var resultado = Expresion();

    if (Posicion < texto.Length)
        throw new Exception($"Carácter inesperado al final de la expresión: {Peek()}");

        return resultado;
}

}