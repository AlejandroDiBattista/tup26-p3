using System;

class Compilador {
    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion)) {
            throw new FormatException("Token inesperado");
        }

        // Usamos la clase interna ConstructorAST para procesar la cadena
        var constructor = new ConstructorAST(expresion);
        var arbolRaiz = constructor.ParseExpresion();
        constructor.SaltarEspacios();

        if (!constructor.FinDeTexto) {
            throw new FormatException("Token inesperado");
        }

        return arbolRaiz;
    }

    private class ConstructorAST {
        private readonly string texto;
        private int indice;

        public ConstructorAST(string contenido) {
            texto = contenido;
            indice = 0;
        }

        public bool FinDeTexto => indice >= texto.Length;

        private char VerActual() => FinDeTexto ? '\0' : texto[indice];

        private char Avanzar() => FinDeTexto ? '\0' : texto[indice++];

        public void SaltarEspacios() {
            while (!FinDeTexto && char.IsWhiteSpace(VerActual())) {
                indice++;
            }
        }
    }
}