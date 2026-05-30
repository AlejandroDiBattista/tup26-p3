class Compilador {

    private string codigoFuente = "";
    private int posicionActual = 0;

    public static Nodo Parse(
        string expresion
    ) {

        var parser = new Compilador();

        parser.codigoFuente = expresion;
        parser.posicionActual = 0;

        return parser.LeerExpresion();
    }

    private Nodo LeerExpresion() {

        var nodoActual = LeerTermino();

        while (true) {

            if (Coincide('+')) {

                nodoActual =
                    new OperacionSuma(
                        nodoActual,
                        LeerTermino()
                    );
            }
            else if (Coincide('-')) {

                nodoActual =
                    new OperacionResta(
                        nodoActual,
                        LeerTermino()
                    );
            }
            else {
                break;
            }
        }

        return nodoActual;
    }

    private Nodo LeerTermino() {

        var nodoActual = LeerFactor();

        while (true) {

            if (Coincide('*')) {

                nodoActual =
                    new OperacionMultiplicacion(
                        nodoActual,
                        LeerFactor()
                    );
            }
            else if (Coincide('/')) {

                nodoActual =
                    new OperacionDivision(
                        nodoActual,
                        LeerFactor()
                    );
            }
            else {
                break;
            }
        }

        return nodoActual;
    }
}
