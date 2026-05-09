List<Token> Tokenizar(string expresion) {
    var tokens = new List<Token>();
    int i = 0;
    var numero = "";
    while (i < expresion.Length) {
        char c = expresion[i];

        if (char.IsWhiteSpace(c)) {
            i++;
            continue;
        } else if (char.IsDigit(c)) {
            numero = c.ToString();
            i++;
        } else {
            if(numero != "") {
                tokens.Add(new Token(Tipo.Numero, numero));
            }
            numero = "";
            var token = c switch {
                '+' => new Token(Tipo.Suma),
                '-' => new Token(Tipo.Resta),
                '*' => new Token(Tipo.Producto),
                '/' => new Token(Tipo.Division),
                '(' => new Token(Tipo.Apertura),
                ')' => new Token(Tipo.Cierre),
                _ => throw new Exception($"Caracter no reconocido: {c}")
            };
            tokens.Add(token);
        }
    }
    if(numero != "") {
        tokens.Add(new Token(Tipo.Numero, numero));
    }
    tokens.Add(new Token(Tipo.Fin));
    return tokens;
}

Nodo Compilar(List<Token> tokens) {
    int index = 0;

    Token actual   = () => tokens[0];
    Token anterior = () => tokens[index - 1];

    bool consumir(Tipo tipo) {
        if (tokens[index].Tipo == tipo) {
            index++;
            return true;
        }
        return false;
    }

    Nodo expresion() {
        var nodo = termino();
        if(consumir(Tipo.Suma)) {
            nodo = new Suma(nodo, expresion());
        }
        if(consumir(Tipo.Resta)) {
            nodo = new Resta(nodo, expresion());
        }
        return nodo;
    }

    Nodo termino() {
        var nodo = factor();
        if(consumir(Tipo.Producto)) {
            nodo = new Producto(nodo, termino());
        }
        if(consumir(Tipo.Division)) {
            nodo = new Division(nodo, termino());
        }
        return nodo;
    }

    Nodo factor() {
        if(consumir(Tipo.Suma)) {
            return new Unario(factor());
        }
        if(consumir(Tipo.Resta)) {
            return new Negativo(factor());
        }
        if(consumir(Tipo.Apertura)) {
            var nodo = expresion();
            if(consumir(Tipo.Cierre)) {
                return nodo;
            }
            throw new Exception("Se esperaba un paréntesis de cierre");
        }
        if(consumir(Tipo.Numero)) {
            return new Numero(anterior().Valor);
        }
        throw new Exception($"Token inesperado: {actual().Tipo}");
    }

    var nodo = expresion();
    if(actual().Tipo != Tipo.Fin) {
        throw new Exception("Se esperaba el fin de la expresión");
    }   
    return nodo;
}


interface IVisitante<T> {
    T Visitar(Nodo nodo);
    T Visitar(Unario nodo);
    T Visitar(Binario nodo);
}

abstract class Nodo {
    public abstract void Aceptar(IVisitante visitante);
}

class Unario(Nodo Operando) : Nodo {
    public override void Aceptar(IVisitante visitante) => visitante.Visitar(this);    
}

class Binario(Nodo Izquierdo, Nodo Derecho) : Nodo {
    public override void Aceptar(IVisitante visitante) => visitante.Visitar(this);
}

class Imprimir : IVisitante {
    public void Visitar(Nodo nodo) {
        Console.WriteLine(nodo.GetType().Name);
    }

    public void Visitar(Unario nodo) {
        Console.WriteLine("Unario");
        nodo.Operando.Aceptar(this);
    }

    public void Visitar(Binario nodo) {
        Console.WriteLine("Binario");
        nodo.Izquierdo.Aceptar(this);
        nodo.Derecho.Aceptar(this);
    }
}

enum Tipo { Numero, Suma, Resta, Producto, Division, Apertura, Cierre, Fin }
record Token(Tipo Tipo, string Valor="");