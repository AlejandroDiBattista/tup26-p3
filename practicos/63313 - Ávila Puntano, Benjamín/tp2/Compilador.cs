using System.Reflection.Metadata.Ecma335;

class Compilador {
    public static Nodo Parse(string expresion) {

        if (string.IsNullOrEmpty(expresion)) {
            throw new FormatException("Token inesperado");
        }

        var recorrer = new Recorrer(expresion);
        var raiz = recorrer.ParseExpresion();
        recorrer.saltarespacios();

        if (!recorrer.eof) {
            throw new FormatException("Token inesperado");

        }
        return raiz;


    }
    private class Recorrer {
        private readonly string expresion;
        private int posicion;

        public Recorrer(string expresion) {
            this.expresion = expresion;
            this.posicion = 0;
        }

        public bool eof => posicion >= expresion.Length; // Verifica si se ha llegado al final de la expresión
        private char peek() => eof ? '\0' : expresion[posicion]; // Devuelve el siguiente carácter sin avanzar la posición
        private char next() => eof ? '\0' : expresion[posicion++]; // Devuelve el siguiente carácter y avanza la posición

        public void saltarespacios() {
            while (char.IsWhiteSpace(peek())) {
                next();
            }
        }

        public Nodo ParseExpresion() {
            saltarespacios();
            if (eof) {
                throw new FormatException("Token inesperado");
            }

            var izquierda = ParseTermino();
            while (true) {
                saltarespacios();
                var operador = peek();
                if (operador != '+' && operador != '-') { break; }
                posicion++;
                var derecho = ParseTermino();
                izquierda = operador == '+' ? new Suma(izquierda, derecho) : new Resta(izquierda, derecho);
            }
            return izquierda;
        }
        private Nodo ParseTermino() {
            saltarespacios();
            var izquierda = ParseFactor();
            while (true) {
                saltarespacios();
                var operador = peek();
                if (operador != '*' && operador != '/') { break; }
                posicion++;
                var derecho = ParseFactor();
                izquierda = operador == '*' ? new Multiplicacionprod(izquierda, derecho) : new Division(izquierda, derecho);
            }
            return izquierda;
        }

        private Nodo ParseFactor() {
            saltarespacios();
            if (eof) {
                throw new FormatException("Token inesperado");
            }

            var caracter = peek();

            if (caracter == '(') {

                posicion++;
                var nodo = ParseExpresion();

                saltarespacios();

                if (eof || peek() != ')') {
                    throw new FormatException("Se esperaba ')'");
                }

                posicion++;
                return nodo;
            }

            if (caracter == '+') {
                posicion++;
                return ParseFactor();
            }


            if (caracter == '-') {
                posicion++;
                return new N_negativos(ParseFactor());
            }
            if (caracter == 'x' || caracter == 'X') {
                posicion++;
                return new Variable();
            }
            if (char.IsDigit(caracter)) {
                return ParseNumero();
            }
            throw new FormatException("Token inesperado");
        }
        private Nodo ParseNumero() {
            var inicio = posicion;
            while (posicion < expresion.Length && char.IsDigit(expresion[posicion])) posicion++;
            return new Numero(int.Parse(expresion.Substring(inicio, posicion - inicio)));
        }
    }
}
