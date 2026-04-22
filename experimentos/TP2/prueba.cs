



List<string> Tokenizar(string expresion) {
    var tokens = new List<string>();
    var index = 0;

    bool IsEndOnExpresion() => index >= expresion.Length;
    
    char Next() {
        if (IsEndOnExpresion()) { return '\0'; }
        return expresion[index];
    }
    char Consume() => IsEndOnExpresion() ? '\0' : expresion[index++];
    
    bool IsWhitespace() => char.IsWhiteSpace(Next());
    bool IsDigit()      => char.IsDigit(Next());
    bool IsLetter()     => char.IsLetter(Next());
    bool IsOperator(string operators) => operators.Contains(Next());

    void SkipWhitespace() {
        while (IsWhitespace()) { Consume(); }
    }

    string Number() {
        var number = "";
        while (IsDigit() || Next() == '.' && !number.Contains('.')) { 
            number += Consume();
        }
        return number;
    }
    
    string Name() {
        var name = "";
        while (IsLetter() || IsDigit()) { 
            name += Consume();
        }
        return name;
    }

    while (!IsEndOnExpresion()) {
        SkipWhitespace();
        if(IsDigit()) {
            tokens.Add(Number());
        } else if (IsLetter()) {
            tokens.Add(Name());
        } else if (IsOperator("+-*/(),=\"")) {
            tokens.Add(Consume());
        } else {
            throw new Exception($"Caracter inesperado: {Next()}");
        }
    }

    return tokens; // Placeholder
}

Nodo Compilar(string expresion) {
    var tokens = Tokenizar(expresion);
    var posicion = 0;

    string Next()    => posicion < tokens.Count ? tokens[posicion]   : "\0";
    string Consume() => posicion < tokens.Count ? tokens[posicion++] : "\0";

    Nodo Expresion() {
        var izquiedo = Term();
        while (Next() == "+" || Next() == "-") {
            var operador = Consume();
            var derecho = Term();
            izquiedo = new BinarioNodo(operador[0], izquiedo, derecho);
        }
        return izquiedo;
    }

    Nodo Term() {
        var izquiedo = Factor();
        while (Next() == "*" || Next() == "/") {
            var operador = Consume();
            var derecho = Factor();
            izquiedo = new BinarioNodo(operador[0], izquiedo, derecho);
        }
        return izquiedo;
    }

    Nodo ExtractFuntion(){
        var nombre = Consume();
        if (Next() == "(") {
            Consume();
            var argumentos = new List<Nodo>();
            if (Next() != ")") {
                do {
                    argumentos.Add(Expresion());
                } while (Next() == ",");
            }
            if (Next() != ")") {
                throw new Exception("Se esperaba ')'");
            }
            Consume();
            return new FuncionalNodo(nombre, argumentos);
        }
        return new ConstantNodo(nombre);
    }

    bool IsNumber(string token) => double.TryParse(token, out _);
    bool IsDireccion(string token) => Direccion.TryParse(token, out _);
    bool IsString(string token) => token.StartsWith("\"") && token.EndsWith("\"");
    bool IsIdentifier(string token) => char.IsLetter(token[0]) && token.All(c => char.IsLetterOrDigit(c));

    Nodo Factor() {
        if (Next() == "(") {
            Consume();
            var izquiedo = Expresion();
            if (Next() != ")") {
                throw new Exception("Se esperaba ')'");
            }
            Consume();
            return izquiedo;
        } else if (double.TryParse(Next(), out var numero)) {
            Consume();
            return new NumeroNodo(numero);
        } else if (Direccion.TryParse(Next(), out var direccion)) {
            Consume();
            return new DireccionNodo(direccion.Fila, direccion.Columna);    
        } else if (Next().StartsWith("\"")) {
            var cadena = Next().Trim('"');
            Consume();
            return new CadenaNodo(cadena);
        } else if (IsIdentifier(Next())) {
            return new ConstantNodo(Next()); // Placeholder para referencias a celdas
        } else {
            throw new Exception($"Token inesperado: {Next()}");
        }
    }

    if (Next()=="=") { // Fórmula
        Consume();
        return Expresion();
    } else if (Next() == "\"") { // Cadena
        Consume();
        return new CadenaNodo(Next().Trim('"'));
    } else if (IsNumber(Next())) { // Número
        Consume();
        return new NumeroNodo(double.Parse(Next()));
    }
    var resultado = Expresion();
    return resultado;
}

// Valores posibles
// =1 + 2 + suma(10, A3)    → formulas
// "Hola                    → cadenas
// 202                      → números
class Celda(Direccion Direccion, string Texto, Valor Valor) { }

class Direccion(int Fila, int Columna){ }

class Valor(){}
class Numero(double Valor) : Valor { }
class Cadena(string Valor) : Valor { }
class Formula(string Expresion) : Valor { }



abstract class Nodo{ 
    abstract Valor Evaluar();
}

class DireccionNodo(int Fila, int Columna): Nodo { 
    public override string ToString() => $"({Fila}, {Columna})";
}

class NumeroNodo(double Valor) : Nodo {
    public override string ToString() => Valor.ToString();
}

class ConstantNodo(string Valor) : Nodo {
    public override string ToString() => Valor;
}

class CadenaNodo(string Valor) : Nodo {
    public override string ToString() => $"\"{Valor}\"";
}

class UnarioNodo(char Operador, Nodo Operando) : Nodo {
    public override string ToString() => $"({Operador}{Operando})";
}

class BinarioNodo(char Operador, Nodo Izquierdo, Nodo Derecho) : Nodo {
    public override string ToString() => $"({Izquierdo} {Operador} {Derecho})";
}

class FuncionalNodo(string Nombre, List<Nodo> Argumentos) : Nodo {
    public override string ToString() => $"{Nombre}({string.Join(", ", Argumentos)})";
}

