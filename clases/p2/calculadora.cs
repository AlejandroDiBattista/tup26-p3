// Calculadora - Evaluador de expresiones aritméticas

using System;
using System.Collections.Generic;

// ── Punto de entrada ──────────────────────────────────────────────────────────

Console.WriteLine("Calculadora de expresiones aritméticas");
Console.WriteLine("Operadores : + - * / % ^");
Console.WriteLine("Funciones  : sin cos tan sqrt abs log ln ceil floor");
Console.WriteLine("Constantes : pi  e          (escribe 'salir' para terminar)");
Console.WriteLine(new string('─', 60));

while (true)
{
    Console.Write("> ");
    string? linea = Console.ReadLine();
    if (linea is null || linea.Trim().ToLower() is "salir" or "exit" or "q") break;
    if (string.IsNullOrWhiteSpace(linea)) continue;

    try
    {
        var tokens = new Lexer(linea).Tokenizar();
        double resultado = new Parser(tokens).Evaluar();

        string fmt = resultado == Math.Truncate(resultado) && Math.Abs(resultado) < 1e15
                     ? resultado.ToString("0")
                     : resultado.ToString("G10");
        Console.WriteLine($"  = {fmt}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
    }
}

// ── Lexer ─────────────────────────────────────────────────────────────────────

enum TipoToken { Numero, Mas, Menos, Por, Div, Mod, Potencia, ParenIzq, ParenDer, Funcion, Fin }

record Token(TipoToken Tipo, string Texto, double Valor = 0);

class Lexer(string fuente)
{
    int pos = 0;

    public List<Token> Tokenizar()
    {
        var tokens = new List<Token>();
        while (pos < fuente.Length)
        {
            char c = fuente[pos];

            if (char.IsWhiteSpace(c)) { pos++; continue; }

            if (char.IsDigit(c) || c == '.')
            {
                int inicio = pos;
                while (pos < fuente.Length && (char.IsDigit(fuente[pos]) || fuente[pos] == '.'))
                    pos++;
                string txt = fuente[inicio..pos];
                tokens.Add(new Token(TipoToken.Numero, txt, double.Parse(txt, System.Globalization.CultureInfo.InvariantCulture)));
                continue;
            }

            if (char.IsLetter(c))
            {
                int inicio = pos;
                while (pos < fuente.Length && char.IsLetter(fuente[pos]))
                    pos++;
                string nombre = fuente[inicio..pos].ToLower();
                if (nombre is "sin" or "cos" or "tan" or "sqrt" or "abs" or "log" or "ln"
                           or "ceil" or "floor" or "pi" or "e")
                    tokens.Add(new Token(TipoToken.Funcion, nombre));
                else
                    throw new Exception($"Identificador desconocido: '{nombre}'");
                continue;
            }

            Token tok = c switch
            {
                '+' => new Token(TipoToken.Mas,      "+"),
                '-' => new Token(TipoToken.Menos,    "-"),
                '*' => new Token(TipoToken.Por,      "*"),
                '/' => new Token(TipoToken.Div,      "/"),
                '%' => new Token(TipoToken.Mod,      "%"),
                '^' => new Token(TipoToken.Potencia, "^"),
                '(' => new Token(TipoToken.ParenIzq, "("),
                ')' => new Token(TipoToken.ParenDer, ")"),
                _   => throw new Exception($"Carácter inesperado: '{c}'")
            };
            tokens.Add(tok);
            pos++;
        }
        tokens.Add(new Token(TipoToken.Fin, ""));
        return tokens;
    }
}

// ── Parser / Evaluador (Recursive Descent) ────────────────────────────────────
//
//  expr     = term   { ('+' | '-') term   }
//  term     = factor { ('*' | '/' | '%') factor }
//  factor   = unario { '^' unario }          ← derecha-asociativo
//  unario   = '-' unario | primario
//  primario = número | constante | función '(' expr ')' | '(' expr ')'

class Parser(List<Token> tokens)
{
    int pos = 0;
    Token Actual => tokens[pos];

    Token Consumir(TipoToken tipo)
    {
        if (Actual.Tipo != tipo)
            throw new Exception($"Se esperaba '{tipo}' pero se encontró '{Actual.Texto}'");
        return tokens[pos++];
    }

    public double Evaluar() { double v = Expr(); Consumir(TipoToken.Fin); return v; }

    double Expr()
    {
        double v = Term();
        while (Actual.Tipo is TipoToken.Mas or TipoToken.Menos)
        {
            bool suma = Actual.Tipo == TipoToken.Mas; pos++;
            v = suma ? v + Term() : v - Term();
        }
        return v;
    }

    double Term()
    {
        double v = Factor();
        while (Actual.Tipo is TipoToken.Por or TipoToken.Div or TipoToken.Mod)
        {
            TipoToken op = Actual.Tipo; pos++;
            double d = Factor();
            v = op switch
            {
                TipoToken.Por => v * d,
                TipoToken.Div => d == 0 ? throw new Exception("División por cero") : v / d,
                _             => v % d
            };
        }
        return v;
    }

    double Factor()
    {
        double b = Unario();
        if (Actual.Tipo == TipoToken.Potencia) { pos++; return Math.Pow(b, Unario()); }
        return b;
    }

    double Unario()
    {
        if (Actual.Tipo == TipoToken.Menos) { pos++; return -Unario(); }
        return Primario();
    }

    double Primario()
    {
        if (Actual.Tipo == TipoToken.Funcion)
        {
            string nombre = Actual.Texto; pos++;

            if (nombre == "pi") return Math.PI;
            if (nombre == "e")  return Math.E;

            Consumir(TipoToken.ParenIzq);
            double arg = Expr();
            Consumir(TipoToken.ParenDer);

            return nombre switch
            {
                "sin"   => Math.Sin(arg),
                "cos"   => Math.Cos(arg),
                "tan"   => Math.Tan(arg),
                "sqrt"  => arg < 0 ? throw new Exception("Raíz de negativo") : Math.Sqrt(arg),
                "abs"   => Math.Abs(arg),
                "log"   => Math.Log10(arg),
                "ln"    => Math.Log(arg),
                "ceil"  => Math.Ceiling(arg),
                "floor" => Math.Floor(arg),
                _       => throw new Exception($"Función desconocida: {nombre}")
            };
        }

        if (Actual.Tipo == TipoToken.Numero)
        {
            double v = Actual.Valor; pos++; return v;
        }

        if (Actual.Tipo == TipoToken.ParenIzq)
        {
            pos++;
            double v = Expr();
            Consumir(TipoToken.ParenDer);
            return v;
        }

        throw new Exception(Actual.Tipo == TipoToken.Fin
            ? "Expresión incompleta"
            : $"Token inesperado: '{Actual.Texto}'");
    }
}
