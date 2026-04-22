using static System.Console;

var variables = new Dictionary<string, double>();

if (args.Length > 0) {
    WriteLine($"{Intepretar(args[0])} {Ejecutar(Compilar(args[0]))}");
    return;
}

WriteLine("== Calculadora con Variables ==");

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

// SINTAXIS DE LA CALCULADORA CON VARIABLES
//
// programa    -> identificador '=' expresion | expresion
// expresion   -> termino ('+' | '-' expresion)
// termino     -> factor  ('*' | '/' termino)
// factor      -> numero | identificador | '(' expresion ')' | '-' factor

double Intepretar(string texto) {
    var posicion = 0;
    texto += ";";

    double Programa() {
        SaltarEspacios();
        var guardado = posicion;
        var nombre = LeerIdentificador();
        SaltarEspacios();
        if (nombre != "" && Coincide('=')) {
            var valor = Expresion();
            variables[nombre] = valor;
            return valor;
        }
        posicion = guardado;
        return Expresion();
    }

    double Expresion() {
        var valor = Termino();
        SaltarEspacios();

        if (Coincide('+')) return valor + Expresion();
        if (Coincide('-')) return valor - Expresion();

        return valor;
    }

    double Termino() {
        var valor = Factor();
        SaltarEspacios();

        if (Coincide('*')) return valor * Termino();
        if (Coincide('/')) {
            var divisor = Termino();
            if (divisor == 0) throw new DivideByZeroException("No se puede dividir por cero.");
            return valor / divisor;
        }
        return valor;
    }

    double Factor() {
        SaltarEspacios();

        if (Coincide('(')) {
            var valor = Expresion();
            SaltarEspacios();
            if (!Coincide(')')) throw new FormatException("Falta cerrar un parentesis.");
            return valor;
        }

        if (Coincide('-')) return -Factor();

        var nombre = LeerIdentificador();
        if (nombre != "") {
            if (!variables.TryGetValue(nombre, out var valor))
                throw new InvalidOperationException($"Variable '{nombre}' no definida.");
            return valor;
        }

        return Numero();
    }

    double Numero() {
        SaltarEspacios();

        var numero = "";
        while (char.IsDigit(texto[posicion]) || texto[posicion] == '.') {
            numero += texto[posicion++];
        }

        if (numero == "") throw new FormatException($"Se esperaba un numero en la posicion {posicion + 1}.");

        return double.Parse(numero);
    }

    string LeerIdentificador() {
        var id = "";
        while (char.IsLetter(texto[posicion]) || texto[posicion] == '_' || (id != "" && char.IsDigit(texto[posicion]))) {
            id += texto[posicion++];
        }
        return id;
    }

    void SaltarEspacios() {
        while (char.IsWhiteSpace(texto[posicion])) posicion++;
    }

    bool Coincide(char caracter) {
        if (texto[posicion] == caracter) {
            posicion++;
            return true;
        }
        return false;
    }

    return Programa();
}

// Implementacion 2:
// primero compila la expresion a un AST y despues evalua ese arbol.
//
// AST con variables:
// - Numero: guarda un double
// - Variable: guarda el nombre de la variable
// - Asignacion: guarda (nombre, nodo expresion)
// - Operacion: guarda (operador, nodo izquierdo, nodo derecho)

Nodo Compilar(string texto) {
    var posicion = 0;
    texto += ";";

    Nodo Programa() {
        SaltarEspacios();
        var guardado = posicion;
        var nombre = LeerIdentificador();
        SaltarEspacios();
        if (nombre != "" && Coincide('=')) {
            return new Asignacion(nombre, Expresion());
        }
        posicion = guardado;
        return Expresion();
    }

    Nodo Expresion() {
        var nodo = Termino();
        SaltarEspacios();

        if (Coincide('+')) return new Operacion('+', nodo, Expresion());
        if (Coincide('-')) return new Operacion('-', nodo, Expresion());

        return nodo;
    }

    Nodo Termino() {
        var nodo = Factor();
        SaltarEspacios();

        if (Coincide('*')) return new Operacion('*', nodo, Termino());
        if (Coincide('/')) return new Operacion('/', nodo, Termino());

        return nodo;
    }

    Nodo Factor() {
        SaltarEspacios();

        if (Coincide('(')) {
            var nodo = Expresion();
            SaltarEspacios();
            if (!Coincide(')')) throw new FormatException("Falta cerrar un parentesis.");
            return nodo;
        }

        if (Coincide('-')) return new Operacion('-', new Numero(0.0), Factor());

        var nombre = LeerIdentificador();
        if (nombre != "") return new Variable(nombre);

        return Numero();
    }

    Nodo Numero() {
        SaltarEspacios();

        var numero = "";
        while (char.IsDigit(texto[posicion]) || texto[posicion] == '.') {
            numero += texto[posicion++];
        }

        if (numero == "") throw new FormatException($"Se esperaba un numero en la posicion {posicion + 1}.");

        return new Numero(double.Parse(numero));
    }

    string LeerIdentificador() {
        var id = "";
        while (char.IsLetter(texto[posicion]) || texto[posicion] == '_' || (id != "" && char.IsDigit(texto[posicion]))) {
            id += texto[posicion++];
        }
        return id;
    }

    void SaltarEspacios() {
        while (char.IsWhiteSpace(texto[posicion])) posicion++;
    }

    bool Coincide(char caracter) {
        if (texto[posicion] == caracter) {
            posicion++;
            return true;
        }
        return false;
    }

    return Programa();
}

double Ejecutar(Nodo nodo) {
    return nodo.Evaluar(variables);
}

abstract class Nodo {
    public abstract double Evaluar(Dictionary<string, double> variables);
}

class Numero(double Valor) : Nodo {
    public override double Evaluar(Dictionary<string, double> variables) => Valor;
    public override string ToString() => Valor.ToString();
}

class Variable(string Nombre) : Nodo {
    public override double Evaluar(Dictionary<string, double> variables) {
        if (!variables.TryGetValue(Nombre, out var valor))
            throw new InvalidOperationException($"Variable '{Nombre}' no definida.");
        return valor;
    }
    public override string ToString() => Nombre;
}

class Asignacion(string Nombre, Nodo Expresion) : Nodo {
    public override double Evaluar(Dictionary<string, double> variables) {
        var valor = Expresion.Evaluar(variables);
        variables[Nombre] = valor;
        return valor;
    }
    public override string ToString() => $"({Nombre} = {Expresion})";
}

class Operacion(char Operador, Nodo Izquierdo, Nodo Derecho) : Nodo {
    public override double Evaluar(Dictionary<string, double> variables) {
        var izquierdo = Izquierdo.Evaluar(variables);
        var derecho   = Derecho.Evaluar(variables);

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
