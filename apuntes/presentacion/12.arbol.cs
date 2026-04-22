// 10 + 30 * 2
using System.Runtime.CompilerServices;

Numero a = 10;
Numero b = new Numero("20");
var n3 = a + b * 3;
var codigo = Compilar("10 + 20 * 3 * (2 + A3)");


var variables = new Dictionary<string, double>(){
    ["A3"] = 100
};

Console.WriteLine($"{codigo} = {codigo.Evaluar(variables)}");

Nodo Compilar(string formula) {
    int pos = 0;
    string Next(){
        while (pos < formula.Length && char.IsWhiteSpace(formula[pos])) { pos++; }
        if (pos >= formula.Length) { return "\0"; }
        return formula[pos].ToString();
    }
    string Consume() => pos < formula.Length ? formula[pos++].ToString() : "\0";

    Nodo Expresion() {
        var nodo = Termino();
        while (Next() == "+" || Next() == "-") {
            var operador = Consume();
            var derecho = Termino();
            nodo = operador == "+" ? nodo + derecho : nodo - derecho;
        }
        return nodo;
    }

    Nodo Termino() {
        var nodo = Unario();
        while (Next() == "*" || Next() == "/") {
            var operador = Consume();
            var derecho = Unario();
            nodo = operador == "*" ? nodo * derecho : nodo / derecho;
        }
        return nodo;
    }

    Nodo Unario() {
        if (Next() == "+" || Next() == "-") {
            var operador = Consume();
            var operando = Unario();
            return operador == "+" ? operando : -operando;
        }
        return Factor();
    }

    Nodo Factor() {
        if (Next() == "(") {
            Consume();
            var nodo = Expresion();
            if (Next() != ")") {
                throw new Exception("Se esperaba ')'");
            }
            Consume();
            return nodo;
        } else if (char.IsDigit(Next()[0])) {
            var numero = "";
            while (char.IsDigit(Next()[0]) || Next() == ".") {
                numero += Consume();
            }
            return new Numero(numero);
        } if (char.IsLetter(Next()[0])) {
            var nombre = "";
            while (char.IsLetterOrDigit(Next()[0])) {
                nombre += Consume();
            }
            return new Variable(nombre);
        } else {
            throw new Exception($"Token inesperado: {Next()}");
        }
    }
    return Expresion();
}
abstract record Nodo {
    public static implicit operator Nodo(int valor)    => new Numero(valor);
    public static implicit operator Nodo(string valor) => new Numero(valor);

    public static Nodo operator -(Nodo operando) => new Unario("-", operando);
    public static Nodo operator +(Nodo operando) => new Unario("+", operando);
    public static Nodo operator +(Nodo izquierdo, Nodo derecho) => new Binario(izquierdo, "+", derecho);
    public static Nodo operator -(Nodo izquierdo, Nodo derecho) => new Binario(izquierdo, "-", derecho);
    public static Nodo operator *(Nodo izquierdo, Nodo derecho) => new Binario(izquierdo, "*", derecho);
    public static Nodo operator /(Nodo izquierdo, Nodo derecho) => new Binario(izquierdo, "/", derecho);
    public abstract double Evaluar(Dictionary<string, double> variables);
}

record Variable(string Nombre) : Nodo {
    public override string ToString() => Nombre;
    public override double Evaluar(Dictionary<string, double> variables) {
        if (variables.TryGetValue(Nombre, out var valor)) {
            return valor;
        }
        throw new Exception($"Variable '{Nombre}' no definida");
    }
}

record Numero(double Valor) : Nodo {
    public Numero(string valor) : this(double.Parse(valor)) { }
    public static implicit operator Numero(int valor) => new Numero((double)valor);

    public override string ToString() => Valor.ToString();
    public override double Evaluar(Dictionary<string, double> variables) => Valor;
}

record Unario(string Operador, Nodo Operando) : Nodo {
    public override string ToString() => $"({Operador}{Operando})";
    public override double Evaluar(Dictionary<string, double> variables) {
        var valor = Operando.Evaluar(variables);
        return Operador switch {
            "+" => valor,
            "-" => -valor,
            _ => throw new InvalidOperationException($"Operador unario `{Operador}` no soportado")
        };
    }
}

record Binario(Nodo Izquierdo, string Operador, Nodo Derecho) : Nodo {
    public override string ToString() => $"({Izquierdo} {Operador} {Derecho})";
    public override double Evaluar(Dictionary<string, double> variables) {
        var izquierdo = Izquierdo.Evaluar(variables);
        var derecho = Derecho.Evaluar(variables);
        return Operador switch {
            "+" => izquierdo + derecho,
            "-" => izquierdo - derecho,
            "*" => izquierdo * derecho,
            "/" => izquierdo / derecho,
            _ => throw new InvalidOperationException($"Operador binario '{Operador}' no soportado")
        };
    }
}

