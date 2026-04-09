using static System.Console;

if(args.Length > 0) {
    WriteLine($"{Intepretar(args[0])} {Ejecutar(Compilar(args[0]))}");
    return;
}

WriteLine("== Calculadora de Consola ==");

while (true) {
    Write("> ");
    var entrada = ReadLine() ?? "";
    if (string.IsNullOrWhiteSpace(entrada)) { break; }
    
    try {
        var resultadoDirecto = Intepretar(entrada);
        var codigo = Compilar(entrada);
        var resultadoDesdeAst = Ejecutar(codigo);
        WriteLine($"""
        {entrada}

        Directo   : {resultadoDirecto}
        AST       : {codigo}
        Desde AST : {resultadoDesdeAst}
        
        """);

    } catch (Exception ex) {
        WriteLine($"Error     : {ex.Message}");
    }

    WriteLine();
}

// SINTAXIS DE LA CALCULADORA
//
// expresion -> termino ('+' | '-' expresion)
// termino   -> factor  ('*' | '/' termino)
// factor    -> numero | '(' expresion ')' | '-'factor

double Intepretar(string texto) {
    var posicion = 0;

    double Expresion() {
        var valor = Termino();
        SaltarEspacios();

        if (Coincide('+')) {
            return valor + Expresion();
        } 
        if (Coincide('-')) {
            return valor - Expresion();
        }

        return valor;
    }

    double Termino() {
        var valor = Factor();

        SaltarEspacios();

        if (Coincide('*')) {
            return valor * Termino();
        } 
        if (Coincide('/')) {
            var divisor = Termino();

            if (divisor == 0) { throw new DivideByZeroException("No se puede dividir por cero."); }

            return valor / divisor;
        }
        return valor;
    }

    double Factor() {
        SaltarEspacios();

        if (Coincide('(')) {
            var valor = Expresion();
            SaltarEspacios();

            if (!Coincide(')')) { throw new FormatException("Falta cerrar un parentesis."); }

            return valor;
        }

        if (Coincide('-')) {
            return -Factor();
        }

        return Numero();
    }

    double Numero() {
        SaltarEspacios();

        var numero = "";
        while ( char.IsDigit(texto[posicion]) || texto[posicion] == '.' ) {
            numero += texto[posicion++];
        }

        if (numero == "") {
            throw new FormatException($"Se esperaba un numero en la posicion {posicion + 1}.");
        }

        return double.Parse(numero);
    }

    void SaltarEspacios() {
        while (char.IsWhiteSpace(texto[posicion])) {
            posicion++;
        }
    }

    bool Coincide(char caracter) {
        if (texto[posicion] == caracter) {
            posicion++;
            return true;
        }

        return false;
    }

    texto += ";"; // para evitar el error de indice fuera de rango
    return Expresion();
}

// Implementacion 2:
// primero compila la expresion a un AST y despues evalua ese arbol.
//
// AST minimalista:
// - un numero se guarda como double
// - una operacion se guarda como (char operador, object izquierdo, object derecho)

Nodo Compilar(string texto) {
    var posicion = 0;

    Nodo Expresion() {
        var nodo = Termino();

        SaltarEspacios();

        if (Coincide('+')) {
            return new Operacion('+', nodo, Expresion());
        } 

        if (Coincide('-')) {
            return new Operacion('-', nodo, Expresion());
        }

        return nodo;
    }

    Nodo Termino() {
        var nodo = Factor();

        SaltarEspacios();

        if (Coincide('*')) {
            return new Operacion('*', nodo, Termino());
        } 

        if (Coincide('/')) {
            return new Operacion('/', nodo, Termino());
        }

        return nodo;
    }

    Nodo Factor() {
        SaltarEspacios();

        if (Coincide('(')) {
            var nodo = Expresion();
            SaltarEspacios();

            if (!Coincide(')')) {
                throw new FormatException("Falta cerrar un parentesis.");
            }

            return nodo;
        }

        if (Coincide('-')) {
            return new Operacion('-', new Numero(0.0), Factor());
        }

        return Numero();
    }

    Nodo Numero() {
        SaltarEspacios();

        var numero = "";

        while( char.IsDigit(texto[posicion]) || texto[posicion] == '.' ) {
            numero += texto[posicion++];
        }

        if (numero == "") {
            throw new FormatException($"Se esperaba un numero en la posicion {posicion + 1}.");
        }

        return new Numero(double.Parse(numero));
    }

    void SaltarEspacios() {
        while (char.IsWhiteSpace(texto[posicion])) {
            posicion++;
        }
    }

    bool Coincide(char caracter) {
        if (texto[posicion] == caracter) {
            posicion++;
            return true;
        }

        return false;
    }

    texto += ";"; // para evitar el error de indice fuera de rango
    return Expresion();
}

double Ejecutar(Nodo nodo) {
    return nodo.Evaluar();
}

abstract class Nodo {
    public abstract double Evaluar();
}

class Numero(double Valor) : Nodo {
    public override double Evaluar()  => Valor;
    public override string ToString() => Valor.ToString();
}

class Operacion(char Operador, Nodo Izquierdo, Nodo Derecho) : Nodo {
    public override double Evaluar() {
        var izquierdo = Izquierdo.Evaluar();
        var derecho   = Derecho.Evaluar();

        return Operador switch {
            '+' => izquierdo + derecho,
            '-' => izquierdo - derecho,
            '*' => izquierdo * derecho,
            '/' => derecho == 0 ? throw new DivideByZeroException("No se puede dividir por cero.") : izquierdo / derecho,
            _ => throw new InvalidOperationException("Operador no esperado.")
        };
    }
    public override string ToString() => $"({Izquierdo} {Operador} {Derecho})";
}   
