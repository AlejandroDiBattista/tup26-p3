abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class Numero : Nodo {
    public int Valor;

    public Numero(int valor) {
        Valor = valor;
    }

    public override int Evaluar(int x = 0) {
        return Valor;
    }
}

class Variable : Nodo {
    public override int Evaluar(int x = 0) {
        return x;
    }
}

class Binaria : Nodo {
    public Nodo Izq;
    public Nodo Der;
    public char Op;

    public Binaria(Nodo izq, char op, Nodo der) {
        Izq = izq;
        Op = op;
        Der = der;
    }

    public override int Evaluar(int x = 0) {
        int a = Izq.Evaluar(x);
        int b = Der.Evaluar(x);

        switch (Op) {
            case '+': return a + b;
            case '-': return a - b;
            case '*': return a * b;
            case '/': return a / b;
            default: throw new Exception("Operador inválido");
        }
    }
}

class Unaria : Nodo {
    public char Op;
    public Nodo Expr;

    public Unaria(char op, Nodo expr) {
        Op = op;
        Expr = expr;
    }

    public override int Evaluar(int x = 0) {
        int val = Expr.Evaluar(x);
        return Op == '-' ? -val : val;
    }
}
