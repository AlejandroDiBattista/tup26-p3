using static System.Console;

// Variables compartidas entre Intepretar y Compilar
var vars = new Dictionary<string, double>();

if (args.Length > 0) {
    EvaluarEntrada(args[0]);
    return;
}

WriteLine("== Calculadora con Variables ==");
WriteLine("Ejemplos: x = 5   |   x + 3   |   y = x * 2");
WriteLine("Presiona Enter vacio para salir.");
WriteLine();

while (true) {
    Write("> ");
    var entrada = ReadLine() ?? "";
    if (string.IsNullOrWhiteSpace(entrada)) { break; }

    EvaluarEntrada(entrada);
    WriteLine();
}

void EvaluarEntrada(string entrada) {
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
}

// SINTAXIS DE LA CALCULADORA
//
// entrada     -> identificador '=' expresion | expresion
// expresion   -> termino ('+' | '-' expresion)
// termino     -> factor  ('*' | '/' termino)
// factor      -> numero | identificador | '(' expresion ')' | '-' factor

double Intepretar(string texto) {
    var posicion = 0;

    double Entrada() {
        SaltarEspacios();
        var guardado = posicion;
        var nombre = LeerIdentificador();

        if (nombre != "") {
            SaltarEspacios();
            if (Coincide('=')) {
                var valor = Expresion();
                vars[nombre] = valor;
                return valor;
            }
        }

        posicion = guardado;
        return Expresion();
    }

    double Expresion() {
        var valor = Termino();
        SaltarEspacios();
        if (Coincide('+')) { return valor + Expresion(); }
        if (Coincide('-')) { return valor - Expresion(); }
        return valor;
    }

    double Termino() {
        var valor = Factor();
        SaltarEspacios();
        if (Coincide('*')) { return valor * Termino(); }
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
        if (Coincide('-')) { return -Factor(); }
        var nombre = LeerIdentificador();
        if (nombre != "") { return ResolverVariable(nombre); }
        return Numero();
    }

    double Numero() {
        SaltarEspacios();
        var numero = "";
        while (char.IsDigit(texto[posicion]) || texto[posicion] == '.') {
            numero += texto[posicion++];
        }
        if (numero == "") {
            throw new FormatException($"Se esperaba un numero en la posicion {posicion + 1}.");
        }
        return double.Parse(numero);
    }

    string LeerIdentificador() {
        SaltarEspacios();
        var id = "";
        while (char.IsLetter(texto[posicion]) || (id != "" && char.IsDigit(texto[posicion]))) {
            id += texto[posicion++];
        }
        return id;
    }

    double ResolverVariable(string nombre) {
        if (!vars.TryGetValue(nombre, out var valor)) {
            throw new InvalidOperationException($"Variable '{nombre}' no definida.");
        }
        return valor;
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
    return Entrada();
}

// Implementacion 2:
// primero compila la expresion a un AST y despues evalua ese arbol.

Nodo Compilar(string texto) {
    var posicion = 0;

    Nodo Entrada() {
        SaltarEspacios();
        var guardado = posicion;
        var nombre = LeerIdentificador();

        if (nombre != "") {
            SaltarEspacios();
            if (Coincide('=')) {
                return new Asignacion(nombre, Expresion(), vars);
            }
        }

        posicion = guardado;
        return Expresion();
    }

    Nodo Expresion() {
        var nodo = Termino();
        SaltarEspacios();
        if (Coincide('+')) { return new Operacion('+', nodo, Expresion()); }
        if (Coincide('-')) { return new Operacion('-', nodo, Expresion()); }
        return nodo;
    }

    Nodo Termino() {
        var nodo = Factor();
        SaltarEspacios();
        if (Coincide('*')) { return new Operacion('*', nodo, Termino()); }
        if (Coincide('/')) { return new Operacion('/', nodo, Termino()); }
        return nodo;
    }

    Nodo Factor() {
        SaltarEspacios();
        if (Coincide('(')) {
            var nodo = Expresion();
            SaltarEspacios();
            if (!Coincide(')')) { throw new FormatException("Falta cerrar un parentesis."); }
            return nodo;
        }
        if (Coincide('-')) {
            return new Operacion('-', new Numero(0.0), Factor());
        }
        var nombre = LeerIdentificador();
        if (nombre != "") { return new Variable(nombre, vars); }
        return Numero();
    }

    Nodo Numero() {
        SaltarEspacios();
        var numero = "";
        while (char.IsDigit(texto[posicion]) || texto[posicion] == '.') {
            numero += texto[posicion++];
        }
        if (numero == "") {
            throw new FormatException($"Se esperaba un numero en la posicion {posicion + 1}.");
        }
        return new Numero(double.Parse(numero));
    }

    string LeerIdentificador() {
        SaltarEspacios();
        var id = "";
        while (char.IsLetter(texto[posicion]) || (id != "" && char.IsDigit(texto[posicion]))) {
            id += texto[posicion++];
        }
        return id;
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
    return Entrada();
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

class Variable(string Nombre, Dictionary<string, double> Vars) : Nodo {
    public override double Evaluar() {
        if (!Vars.TryGetValue(Nombre, out var valor)) {
            throw new InvalidOperationException($"Variable '{Nombre}' no definida.");
        }
        return valor;
    }
    public override string ToString() => Nombre;
}

class Asignacion(string Nombre, Nodo Valor, Dictionary<string, double> Vars) : Nodo {
    public override double Evaluar() {
        var resultado = Valor.Evaluar();
        Vars[Nombre] = resultado;
        return resultado;
    }
    public override string ToString() => $"({Nombre} = {Valor})";
}
