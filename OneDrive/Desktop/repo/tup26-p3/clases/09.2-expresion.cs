using System.Diagnostics;

#region Parantesis balanceados

// Dado una cadena de caracteres, determinar si los paréntesis están balanceados.
bool Balanceados(string e) {
    var parentesis = 0;
    foreach(var c in e) {
        if (c == '(') {
            parentesis++;
        } else if (c == ')') {
            if(--parentesis < 0) { return false; }
        }
    }
    return parentesis == 0;
}   

Debug.Assert(Balanceados("(()(()()))") == true);
Debug.Assert(Balanceados("(()(()())") == false);
Debug.Assert(Balanceados("(()(()()))))") == false);

#endregion

#region Expresion simple 

bool VerificarSimple(string e) {
    var i = 0;
    const char fin = '\0';

    char proximo() => i < e.Length ? e[i] : fin;
    bool es(char c) {
        espacios();
        if(proximo() == c) {
            avanzar();
            return true;
        }        
        return false;
    }
    
    void avanzar() => i++;

    bool EsNumero() {
        espacios();
        var digitos = 0;
        while(char.IsDigit(proximo())) {
            digitos++;
            avanzar();
        }
        return digitos > 0;
    }

    bool EsFactor() {
        if(es('(')) {
            var ok = EsSuma();
            if(es(')')) {
                return ok;
            } else {
                return false;
            }
        }
        return EsNumero();
    }

    bool EsProducto() {
        var izq = EsFactor();
        if(izq && (es('*') || es('/'))) {
            return EsProducto();
        }
        return izq;
    }

    bool EsSuma() {
        var izq = EsProducto();
        if(izq && (es('+') || es('-'))) {
            return EsSuma();
        }
        return izq;
    }

    void espacios() {
        while(char.IsWhiteSpace(proximo())) {
            avanzar();
        }
    }

    return EsSuma() && es(fin);
}


Debug.Assert(VerificarSimple("1")     == true);
Debug.Assert(VerificarSimple("1+2")   == true);
Debug.Assert(VerificarSimple("1+2*3") == true);
Debug.Assert(VerificarSimple("1*2+3") == true);
Debug.Assert(VerificarSimple("1*2*3") == true);
Debug.Assert(VerificarSimple("1*2*")  == false);
Debug.Assert(VerificarSimple("1+*2")  == false);  
Debug.Assert(VerificarSimple("1")     == true);
Debug.Assert(VerificarSimple("1+2")   == true);
Debug.Assert(VerificarSimple("1+2*3") == true);
Debug.Assert(VerificarSimple("1*2+3") == true);
Debug.Assert(VerificarSimple("1*2*3") == true);
Debug.Assert(VerificarSimple("1*2*")  == false);
Debug.Assert(VerificarSimple("1+*2")  == false);
#endregion

#region Tokenizar
const char fin = '\0';

List<string> TokenizarSimple(string e) {
    var tokens = new List<string>();
    var i = 0;
    
    char proximo()  => i < e.Length ? e[i]   : fin;
    char consumir() => i < e.Length ? e[i++] : fin;

    while(proximo() != fin) {
        while(char.IsWhiteSpace(proximo())) {
            consumir();
        } 
        
        if(char.IsDigit(proximo())) {
            var numero = "";
            while(char.IsDigit(proximo())) {
                numero += consumir();
            }
            tokens.Add(numero);
        } else {
            switch(proximo()) {
                case '+' or '-' or '*' or '/' or '(' or ')':
                    tokens.Add(consumir().ToString());
                    break;
                default:
                    throw new Exception($"Caracter no reconocido: {proximo()}");
            }
        }
    }

    tokens.Add(fin.ToString());

    return tokens;
}

bool Verificar(string expresion) {
    var tokens = TokenizarSimple(expresion);
    var i = 0;

    string proximo() => i < tokens.Count ? tokens[i] : fin.ToString();
    bool es(string c) {
        if(proximo() == c) {
            avanzar();
            return true;
        }        
        return false;
    }
    
    void avanzar() => i++;

    bool EsFactor() {
        if(es("(")) {
            var ok = EsSuma();
            if(es(")")) {
                return ok;
            } else {
                return false;
            }
        }
        return EsNumero();
    }

    bool EsProducto() {
        var izq = EsFactor();
        if(izq && (es("*") || es("/"))) {
            return EsProducto();
        }
        return izq;
    }

    bool EsSuma() {
        var izq = EsProducto();
        if(izq && (es("+") || es("-"))) {
            return EsSuma();
        }
        return izq;
    }

    bool EsNumero() {
        if (int.TryParse(proximo(), out _)) {
            avanzar();
            return true;
        }
        return false;
    }

    return EsSuma() && es(fin.ToString());
}

Debug.Assert(Verificar("1")     );
Debug.Assert(Verificar("1+2")   );
Debug.Assert(Verificar("1+2*3") );
Debug.Assert(Verificar("1*2+3") );
Debug.Assert(Verificar("1*2*3") );
Debug.Assert(!Verificar("1*2*") );
Debug.Assert(!Verificar("1+*2") );
#endregion

#region Tokenizardor con clases

List<Token> Tokenizar(string e) {

    string numero() {
        var numero = "";
        while(char.IsDigit(proximo())) {
            numero += consumir();
        }
        return numero;
    }

    void espacios() {
        while(char.IsWhiteSpace(proximo())) {
            consumir();
        }
    }

    var tokens = new List<Token>();
    var i = 0;
    
    char proximo()  => i < e.Length ? e[i]   : fin;
    char consumir() => i < e.Length ? e[i++] : fin;

    while(proximo() != fin) {
        espacios();
        
        if(char.IsDigit(proximo())) {
            tokens.Add(new Token(Tipo.Numero, numero()));
        } else {
            var token = consumir() switch {
                '+' => new Token(Tipo.Suma),
                '-' => new Token(Tipo.Resta),
                '*' => new Token(Tipo.Multiplicacion),
                '/' => new Token(Tipo.Division),
                '(' => new Token(Tipo.Abre),
                ')' => new Token(Tipo.Cierra),
                _   => throw new Exception($"Caracter no reconocido: {proximo()}")
            };
            tokens.Add(token);
        }
    }

    tokens.Add(new Token(Tipo.Fin));

    return tokens;
}

bool ValidarExpresion(string expresion) {
    var tokens = Tokenizar(expresion);
    var i = 0;

    Tipo proximo() => i < tokens.Count ? tokens[i].tipo : Tipo.Fin;
    bool es(Tipo tipo) {
        if(proximo() == tipo) {
            avanzar();
            return true;
        }        
        return false;
    }
    
    void avanzar() => i++;

    bool esFactor() {
        if(es(Tipo.Abre)) {
            var ok = esSuma();
            if(es(Tipo.Cierra)) {
                return ok;
            } else {
                return false;
            }
        }
        return es(Tipo.Numero);
    }

    bool esProducto() {
        var izq = esFactor();
        if(izq && (es(Tipo.Multiplicacion) || es(Tipo.Division))) {
            return esProducto();
        }
        return izq;
    }

    bool esSuma() {
        var izq = esProducto();
        if(izq && (es(Tipo.Suma) || es(Tipo.Resta))) {
            return esSuma();
        }
        return izq;
    }

    bool esNumero() {
        return es(Tipo.Numero);
    }

    return esSuma() && es(Tipo.Fin);
}

Debug.Assert(ValidarExpresion("1")     );
Debug.Assert(ValidarExpresion("1+2")   );
Debug.Assert(ValidarExpresion("1+2*3") );
Debug.Assert(ValidarExpresion("1*2+3") );
Debug.Assert(ValidarExpresion("1*2*3") );
Debug.Assert(!ValidarExpresion("1*2*") );
Debug.Assert(!ValidarExpresion("1+*2") );
#endregion

#region Token con herencia
List<TokenBase> TokenizarHerencia(string e) {

    string numero() {
        var numero = "";
        while(char.IsDigit(proximo())) {
            numero += consumir();
        }
        return numero;
    }

    void espacios() {
        while(char.IsWhiteSpace(proximo())) {
            consumir();
        }
    }

    var tokens = new List<TokenBase>();
    var i = 0;
    
    char proximo()  => i < e.Length ? e[i]   : fin;
    char consumir() => i < e.Length ? e[i++] : fin;

    while(proximo() != fin) {
        espacios();
        
        if(char.IsDigit(proximo())) {
            tokens.Add(new Numero(numero()));
        } else {
            var token = consumir() switch {
                '+' => new Suma(),
                '-' => new Resta(),
                '*' => new Multiplicacion(),
                '/' => new Division(),
                '(' => new Abre(),
                ')' => new Cierra(),
                _   => throw new Exception($"Caracter no reconocido: {proximo()}")
            };
            tokens.Add(token);
        }
    }

    tokens.Add(new Fin());

    return tokens;
}

bool ValidarHerencia(string expresion) {
    var tokens = TokenizarHerencia(expresion);
    var i = 0;

    TokenBase proximo() => i < tokens.Count ? tokens[i] : new Fin();
    bool es<T>() where T : TokenBase {
        if(proximo() is T) {
            avanzar();
            return true;
        }        
        return false;
    }
    
    void avanzar() => i++;

    bool esFactor() {
        if(proximo() is Abre) {
            var ok = esSuma();
            if(es<Cierra>()) {
                return ok;
            } else {
                return false;
            }
        }
        return es<Numero>();
    }

    bool esProducto() {
        var izq = esFactor();
        if(izq && (es<Multiplicacion>() || es<Division>())) {
            return esProducto();
        }
        return izq;
    }

    bool esSuma() {
        var izq = esProducto();
        if(izq && (es<Suma>() || es<Resta>())) {
            return esSuma();
        }
        return izq;
    }

    return esSuma() && es<Fin>();
}

int Interpretar(string expresion) {
    var tokens = Tokenizar(expresion);
    var i = 0;

    Token proximo() => i < tokens.Count ? tokens[i] : new Fin();
    bool es(Tipo tipo){
        if(proximo().tipo == tipo) {
            avanzar();
            return true;
        }        
        return false;
    }
    
    void avanzar() => i++;

    int esFactor() {
        if(proximo() is Abre) {
            var valor = esSuma();
            if(es(Tipo.Cierra)) {
                return valor;
            } else {
                throw new Exception("Se esperaba un cierre de paréntesis");
            }
        }
        if(proximo().tipo == Tipo.Numero) {
            var valor = proximo().Valor;
            avanzar();
            return valor;
        }
        throw new Exception("Se esperaba un número o un paréntesis");
    }

    int esProducto() {
        var valor = esFactor();
        while(true) {
            if(es(Tipo.Multiplicacion)) {
                valor *= esFactor();
            } else if(es(Tipo.Division)) {
                valor /= esFactor();
            } else {
                break;
            }
        }
        return valor;
    }

    int esSuma() {
        var valor = esProducto();
        while(true) {
            if(es(Tipo.Suma)) {
                valor += esProducto();
            } else if(es(Tipo.Resta)) {
                valor -= esProducto();
            } else {
                break;
            }
        }
        return valor;
    }

    return esSuma();
}

Debug.Assert(ValidarHerencia("1")     );
Debug.Assert(ValidarHerencia("1+2")   );
Debug.Assert(ValidarHerencia("1+2*3") );
Debug.Assert(ValidarHerencia("1*2+3") );
Debug.Assert(ValidarHerencia("1*2*3") );
Debug.Assert(!ValidarHerencia("1*2*") );
Debug.Assert(!ValidarHerencia("1+*2") );

Debug.Assert(Interpretar("1")     == 1);
Debug.Assert(Interpretar("1+2")   == 3);
Debug.Assert(Interpretar("1+2*3") == 7);
Debug.Assert(Interpretar("1*2+3") == 5);
Debug.Assert(Interpretar("1*2*3") == 6);
Debug.Assert(Interpretar("(1+2)*3") == 9);
#endregion

#region Expresion Compilada

Nodo Compilar(string expresion) {
    var tokens = Tokenizar(expresion);
    var i = 0;

    Tipo proximo() => i < tokens.Count ? tokens[i].Tipo : Tipo.Fin;
    bool es(Tipo tipo){
        if(proximo() == tipo) {
            avanzar();
            return true;
        }        
        return false;
    }
    
    void avanzar() => i++;

    Nodo esFactor() {
        if(proximo() == Tipo.Resta) {
            avanzar();
            return new NegacionNodo(esFactor());
        }
        if(proximo() == Tipo.Abre) {
            var nodo = esSuma();
            if(es(Tipo.Cierra)) {
                return nodo;
            } else {
                throw new Exception("Se esperaba un cierre de paréntesis");
            }
        }
        if(proximo() == Tipo.Numero) {
            var valor = proximo().Valor;
            avanzar();
            return new NumeroNodo(valor);
        }
        throw new Exception("Se esperaba un número o un paréntesis");
    }

    Nodo esProducto() {
        var nodo = esFactor();
        while(true) {
            if(es(Tipo.Multiplicacion)) {
                nodo = new MultiplicacionNodo(nodo, esFactor());
            } else if(es(Tipo.Division)) {
                nodo = new DivisionNodo(nodo, esFactor());
            } else {
                break;
            }
        }
        return nodo;
    }

    Nodo esSuma() {
        var nodo = esProducto();
        while(true) {
            if(es(Tipo.Suma)) {
                nodo = new SumaNodo(nodo, esProducto());
            } else if(es(Tipo.Resta)) {
                nodo = new RestaNodo(nodo, esProducto());
            } else {
                break;
            }
        }
        return nodo;
    }

    return esSuma();
}

int Evaluar(string expresion) {
    var nodo = Compilar(expresion);
    return nodo.Evaluar();
}

Debug.Assert(Evaluar("1")     == 1);
Debug.Assert(Evaluar("1+2")   == 3);
Debug.Assert(Evaluar("1+2*3") == 7);
Debug.Assert(Evaluar("1*2+3") == 5);
Debug.Assert(Evaluar("1*2*3") == 6);
Debug.Assert(Evaluar("(1+2)*3") == 9);
#endregion


#region Tipos de datos 
abstract record Nodo {
    public virtual int Evaluar();
}

record NumeroNodo(int Valor) : Nodo {
    public override int Evaluar() => Valor;
    override string ToString() => Valor.ToString();
};

abstract record NodoUnario(Nodo Operando) : Nodo {
    public Nodo Operando { get; init; } = Operando;
    override string ToString() => $"({Operando})";
};
record NegacionNodo(Nodo Operando) : NodoUnario {
    public override int Evaluar() => -Operando.Evaluar();
    override string ToString() => $"-{Operando}";
};

abstract record NodoBinario(Nodo Izquierda, Nodo Derecha) : Nodo {
    abstract string Operador {get;} 
    override string ToString() => $"({Izquierda} {Operador} {Derecha})";
};

record SumaNodo() : NodoBinario {
    public override int Evaluar() => Izquierda.Evaluar() + Derecha.Evaluar();
    public override string Operador => "+";
};

record RestaNodo() : NodoBinario {
    public override int Evaluar() => Izquierda.Evaluar() - Derecha.Evaluar();
    public override string Operador => "-";
};

record MultiplicacionNodo() : NodoBinario {
    public override int Evaluar() => Izquierda.Evaluar() * Derecha.Evaluar();
    public override string Operador => "*";
};

record DivisionNodo() : NodoBinario {
    public override int Evaluar() => Izquierda.Evaluar() / Derecha.Evaluar();
    public override string Operador => "/";
};

record TokenBase {}
record Numero(string Valor) : TokenBase{
    public int ValorInt => int.Parse(Valor);
};
record Suma() : TokenBase;
record Resta() : TokenBase;
record Multiplicacion() : TokenBase;
record Division() : TokenBase;
record Abre() : TokenBase;
record Cierra() : TokenBase;
record Fin() : TokenBase;

enum Tipo {
    Numero,
    Suma, Resta,
    Multiplicacion, Division,
    Abre, Cierra, Fin
}

public record Token(Tipo tipo, string valor=""){
    int Valor => int.Parse(valor);
} ;
#endregion