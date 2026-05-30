class Compilador {
    public static Nodo Parse(string expresion) {
        throw new NotImplementedException("Implementar el parser para convertir la expresión en un AST.");
    }

    private Nodo Expresion() {

        Nodo nodo = Termino();

        while (Match('+') || Match('-')) {

            char operador = texto[Posicion - 1];
            Nodo derecha = Termino();
            nodo = operador == '+' ? new SumaNodo(nodo, derecha) : new RestaNodo(nodo, derecha);
        }

        return nodo;
    }

    private Nodo Termino() {

        Nodo nodo = Factor();

        while (Match('*') || Match('/')) {

            char operador = texto[Posicion - 1];
            Nodo derecha = Factor();
            nodo = operador == '*' ? new MultiplicacionNodo(nodo, derecha) : new DivisionNodo(nodo, derecha);
        }

        return nodo;
    }
}
