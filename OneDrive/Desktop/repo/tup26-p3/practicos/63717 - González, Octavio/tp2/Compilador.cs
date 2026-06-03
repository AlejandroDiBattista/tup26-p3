using System;
using System.Collections.Generic;

// Aca pongo los tipos de cosas que podemos encontrar en la cuenta
public enum TipoToken {
    Numero,
    Suma,
    Resta,
    Multiplicacion,
    Division,
    ParentesisIzquierdo,
    ParentesisDerecho,
    VariableX,
    FinDeLinea // Para saber cuando se termina la formula
}

// Esta clase guarda que tipo es y si tiene algun valor (como los numeros)
public class Token {
    public TipoToken Tipo;
    public string Valor;

    public Token(TipoToken tipo, string valor = "") {
        Tipo = tipo;
        Valor = valor;
    }
}

// Esta clase es la que lee la formula y la separa en partes (tokens)
public class Lexer {
    private string _texto;
    private int _posicion;

    public Lexer(string texto) {
        _texto = texto;
        _posicion = 0;
    }

    public List<Token> Escanear() {
        List<Token> tokens = new List<Token>();

        // Si el texto esta vacio o es solo espacios
        if (string.IsNullOrWhiteSpace(_texto)) {
            throw new FormatException("Token inesperado: entrada vacia");
        }

        while (_posicion < _texto.Length) {
            char actual = _texto[_posicion];

            // Si es un espacio, lo saltamos y seguimos
            if (char.IsWhiteSpace(actual)) {
                _posicion++;
                continue;
            }

            // Si es una x (o X), guardamos el token de variable
            if (actual == 'x' || actual == 'X') {
                tokens.Add(new Token(TipoToken.VariableX, "x"));
                _posicion++;
                continue;
            }

            // Si es un numero, leemos todos los digitos que vengan juntos
            if (char.IsDigit(actual)) {
                string numero = "";
                while (_posicion < _texto.Length && char.IsDigit(_texto[_posicion])) {
                    numero = numero + _texto[_posicion];
                    _posicion++;
                }
                tokens.Add(new Token(TipoToken.Numero, numero));
                continue;
            }

            // Aca miramos los signos uno por uno
            switch (actual) {
                case '+': tokens.Add(new Token(TipoToken.Suma)); break;
                case '-': tokens.Add(new Token(TipoToken.Resta)); break;
                case '*': tokens.Add(new Token(TipoToken.Multiplicacion)); break;
                case '/': tokens.Add(new Token(TipoToken.Division)); break;
                case '(': tokens.Add(new Token(TipoToken.ParentesisIzquierdo)); break;
                case ')': tokens.Add(new Token(TipoToken.ParentesisDerecho)); break;
                default:
                    // Si no es nada de lo anterior, usamos la excepcion que pide el test
                    throw new FormatException("Token inesperado: " + actual);
            }

            _posicion++;
        }

        // Al final de todo avisamos que terminamos
        tokens.Add(new Token(TipoToken.FinDeLinea));
        return tokens;
    }
}

// Esta clase es la que arma el arbol (AST) usando los tokens que nos da el Lexer
public class Parser {
    private List<Token> _tokens;
    private int _indiceActual;

    public Parser(List<Token> tokens) {
        _tokens = tokens;
        _indiceActual = 0;
    }

    // Para ver que token sigue sin mover el indice
    private Token TokenActual {
        get { return _tokens[_indiceActual]; }
    }

    // Para avanzar al siguiente token
    private void Avanzar() {
        if (_indiceActual < _tokens.Count - 1) {
            _indiceActual++;
        }
    }

    public Nodo Parsear() {
        Nodo resultado = Expresion();

        if (TokenActual.Tipo != TipoToken.FinDeLinea) {
            throw new FormatException("Token inesperado al final");
        }

        return resultado;
    }

    // Aca manejamos las sumas y restas
    private Nodo Expresion() {
        Nodo izquierda = Termino();

        while (TokenActual.Tipo == TipoToken.Suma || TokenActual.Tipo == TipoToken.Resta) {
            TipoToken tipo = TokenActual.Tipo;
            Avanzar();
            Nodo derecha = Termino();

            if (tipo == TipoToken.Suma) {
                izquierda = new SumaNodo(izquierda, derecha);
            } else {
                izquierda = new RestaNodo(izquierda, derecha);
            }
        }

        return izquierda;
    }

    // Aca van las multiplicaciones y divisiones
    private Nodo Termino() {
        Nodo izquierda = Factor();

        while (TokenActual.Tipo == TipoToken.Multiplicacion || TokenActual.Tipo == TipoToken.Division) {
            TipoToken tipo = TokenActual.Tipo;
            Avanzar();
            Nodo derecha = Factor();

            if (tipo == TipoToken.Multiplicacion) {
                izquierda = new MultiplicacionNodo(izquierda, derecha);
            } else {
                izquierda = new DivisionNodo(izquierda, derecha);
            }
        }

        return izquierda;
    }

    // Aca manejamos numeros, x, parentesis y signos unarios (+ y -)
    private Nodo Factor() {
        if (TokenActual.Tipo == TipoToken.Suma) {
            Avanzar();
            return Factor();
        }

        if (TokenActual.Tipo == TipoToken.Resta) {
            Avanzar();
            return new NegativoNodo(Factor());
        }

        if (TokenActual.Tipo == TipoToken.ParentesisIzquierdo) {
            Avanzar();
            Nodo nodo = Expresion();

            if (TokenActual.Tipo != TipoToken.ParentesisDerecho) {
                // El mensaje que espera la prueba de Octavio
                throw new FormatException("Se esperaba ')'");
            }
            Avanzar();
            return nodo;
        }

        if (TokenActual.Tipo == TipoToken.Numero) {
            int valor = int.Parse(TokenActual.Valor);
            Avanzar();
            return new NumeroNodo(valor);
        }

        if (TokenActual.Tipo == TipoToken.VariableX) {
            Avanzar();
            return new VariableNodo();
        }

        throw new FormatException("Token inesperado");
    }
}

// Una clase de ayuda para usar el compilador mas facil
public class Compilador {

    public static Nodo Parse(string formula) {
        Lexer lexer = new Lexer(formula);
        List<Token> tokens = lexer.Escanear();
        Parser parser = new Parser(tokens);
        return parser.Parsear();
    }
}
