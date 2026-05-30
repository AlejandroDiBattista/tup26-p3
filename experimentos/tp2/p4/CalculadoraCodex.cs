using System.Globalization;
using static System.Console;

var variablesActuales = new Dictionary<string, double>();

if(args.Length > 0) {
    EvaluarEntrada(args[0]);
    return;
}

WriteLine("== Calculadora de Consola con Variables ==");
WriteLine("Escribe una expresion con numeros y variables.");
WriteLine("Ejemplo: x + 2 * (y - 3)");

while (true) {
    Write("> ");
    var entrada = ReadLine() ?? "";
    if (string.IsNullOrWhiteSpace(entrada)) { break; }
    
    EvaluarEntrada(entrada);
    WriteLine();
}

void EvaluarEntrada(string entrada) {
    try {
        var codigo = Compilar(entrada);
        var optimizado = Optimizar(codigo);
        var variables = ObtenerVariables(codigo);

        PedirValores(variables);

        var resultadoDirecto = Intepretar(entrada);
        var resultadoDesdeAst = Ejecutar(codigo);
        var resultadoOptimizado = Ejecutar(optimizado);

        WriteLine($"""
        {entrada}

        Directo       : {resultadoDirecto}
        AST           : {codigo}
        Optimizado    : {optimizado}
        Desde AST     : {resultadoDesdeAst}
        Desde Opt AST : {resultadoOptimizado}
        
        """);

    } catch (Exception ex) {
        WriteLine($"Error     : {ex.Message}");
    }
}

void PedirValores(List<string> variables) {
    variablesActuales.Clear();

    foreach (var nombre in variables) {
        while (true) {
            Write($"{nombre} = ");
            var texto = ReadLine() ?? "";

            if (double.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out var valor)) {
                variablesActuales[nombre] = valor;
                break;
            }

            WriteLine("Ingresa un numero valido usando '.' para decimales.");
        }
    }

    if (variables.Count > 0) {
        WriteLine();
    }
}

List<string> ObtenerVariables(Nodo codigo) {
    var variables = new List<string>();
    var visitadas = new HashSet<string>();

    void Recorrer(Nodo nodo) {
        switch (nodo) {
            case Numero:
                return;
            case Variable variable:
                if (visitadas.Add(variable.Nombre)) {
                    variables.Add(variable.Nombre);
                }
                return;
            case Operacion operacion:
                Recorrer(operacion.Izquierdo);
                Recorrer(operacion.Derecho);
                return;
            default:
                throw new InvalidOperationException("Nodo no esperado.");
        }
    }

    Recorrer(codigo);
    return variables;
}

// SINTAXIS DE LA CALCULADORA
//
// expresion -> termino ('+' | '-' expresion)
// termino   -> factor  ('*' | '/' termino)
// factor    -> numero | variable | '(' expresion ')' | '-'factor

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

        var nombre = Identificador();

        if (nombre != "") {
            return Variable(nombre);
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

        return double.Parse(numero, CultureInfo.InvariantCulture);
    }

    string Identificador() {
        SaltarEspacios();

        var identificador = "";

        while (char.IsLetter(texto[posicion]) || (identificador != "" && char.IsDigit(texto[posicion]))) {
            identificador += texto[posicion++];
        }

        return identificador;
    }

    double Variable(string nombre) {
        if (!variablesActuales.TryGetValue(nombre, out var valor)) {
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

    void VerificarFin() {
        SaltarEspacios();

        if (texto[posicion] != ';') {
            throw new FormatException($"Caracter inesperado '{texto[posicion]}' en la posicion {posicion + 1}.");
        }
    }

    texto += ";"; // para evitar el error de indice fuera de rango
    var resultado = Expresion();
    VerificarFin();
    return resultado;
}

// Implementacion 2:
// primero compila la expresion a un AST y despues evalua ese arbol.
//
// AST minimalista:
// - un numero se guarda como double
// - una variable se guarda como su nombre
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

        var nombre = Identificador();

        if (nombre != "") {
            return new Variable(nombre, variablesActuales);
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

        return new Numero(double.Parse(numero, CultureInfo.InvariantCulture));
    }

    string Identificador() {
        SaltarEspacios();

        var identificador = "";

        while (char.IsLetter(texto[posicion]) || (identificador != "" && char.IsDigit(texto[posicion]))) {
            identificador += texto[posicion++];
        }

        return identificador;
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

    void VerificarFin() {
        SaltarEspacios();

        if (texto[posicion] != ';') {
            throw new FormatException($"Caracter inesperado '{texto[posicion]}' en la posicion {posicion + 1}.");
        }
    }

    texto += ";"; // para evitar el error de indice fuera de rango
    var codigo = Expresion();
    VerificarFin();
    return codigo;
}

Nodo Optimizar(Nodo codigo) {
    return codigo switch {
        Numero => codigo,
        Variable => codigo,
        Operacion operacion => OptimizarOperacion(operacion),
        _ => throw new InvalidOperationException("Nodo no esperado.")
    };

    Nodo OptimizarOperacion(Operacion operacion) {
        var izquierdo = Optimizar(operacion.Izquierdo);
        var derecho = Optimizar(operacion.Derecho);

        if (izquierdo is Numero && derecho is Numero) {
            return new Numero(new Operacion(operacion.Operador, izquierdo, derecho).Evaluar());
        }

        return new Operacion(operacion.Operador, izquierdo, derecho);
    }
}

double Ejecutar(Nodo nodo) {
    return nodo.Evaluar();
}

abstract class Nodo {
    public abstract double Evaluar();
}

class Numero(double valor) : Nodo {
    public double Valor { get; } = valor;
    public override double Evaluar()  => Valor;
    public override string ToString() => Valor.ToString(CultureInfo.InvariantCulture);
}

class Variable(string nombre, Dictionary<string, double> variables) : Nodo {
    public string Nombre { get; } = nombre;

    public override double Evaluar() {
        if (!variables.TryGetValue(Nombre, out var valor)) {
            throw new InvalidOperationException($"Variable '{Nombre}' no definida.");
        }

        return valor;
    }

    public override string ToString() => Nombre;
}

class Operacion(char operador, Nodo izquierdo, Nodo derecho) : Nodo {
    public char Operador { get; } = operador;
    public Nodo Izquierdo { get; } = izquierdo;
    public Nodo Derecho { get; } = derecho;

    public override double Evaluar() {
        var valorIzquierdo = Izquierdo.Evaluar();
        var valorDerecho   = Derecho.Evaluar();

        return Operador switch {
            '+' => valorIzquierdo + valorDerecho,
            '-' => valorIzquierdo - valorDerecho,
            '*' => valorIzquierdo * valorDerecho,
            '/' => valorDerecho == 0 ? throw new DivideByZeroException("No se puede dividir por cero.") : valorIzquierdo / valorDerecho,
            _ => throw new InvalidOperationException("Operador no esperado.")
        };
    }

    public override string ToString() => $"({Izquierdo} {Operador} {Derecho})";
}
