class Compilador {

        private string codigoFuente = "";
        private int posicionActual = 0;

        public static Nodo Parse(
        string expresion
        ) {

        if (
            string.IsNullOrWhiteSpace(
                expresion
            )
        ) {

            throw new FormatException(
                "Token inesperado"
            );
        }

        var parser = new Compilador();

        parser.codigoFuente = expresion;
        parser.posicionActual = 0;

        var resultadoFinal =
            parser.LeerExpresion();

        parser.OmitirEspacios();

        if (
            parser.posicionActual
            < parser.codigoFuente.Length
        ) {

            throw new FormatException(
                "Token inesperado"
            );
        }

        return resultadoFinal;
    }

    private Nodo LeerExpresion() {

        var nodoActual = LeerTermino();

        while (true) {

            OmitirEspacios();

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

            OmitirEspacios();

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

    private Nodo LeerFactor() {

    OmitirEspacios();

    if (Coincide('+')) {
        return LeerFactor();
    }

    if (Coincide('-')) {

        return new CambioSigno(
            LeerFactor()
        );
    }

    if (Coincide('(')) {

        var nodoInterno =
            LeerExpresion();

        if (!Coincide(')')) {

            throw new FormatException(
                "Se esperaba ')'"
            );
        }

        return nodoInterno;
    }

    if (
        char.IsDigit(
            CaracterActual()
        )
    ) {

        return LeerNumero();
    }

    if (
        char.ToLower(
            CaracterActual()
        ) == 'x'
    ) {

        posicionActual++;

        return new VariableX();
    }

    throw new FormatException(
        "Token inesperado"
    );
}

private char CaracterActual() {

    return posicionActual
        < codigoFuente.Length
        ? codigoFuente[posicionActual]
        : '\0';
}

private bool Coincide(char simbolo) {

    if (
        CaracterActual()
        == simbolo
    ) {

        posicionActual++;

        return true;
    }

    return false;
}

private void OmitirEspacios() {

    while (
        char.IsWhiteSpace(
            CaracterActual()
        )
    ) {

        posicionActual++;
    }
}

private Nodo LeerNumero() {

    int inicio = posicionActual;

    while (
        char.IsDigit(
            CaracterActual()
        )
    ) {

        posicionActual++;
    }

    var textoNumero =
        codigoFuente[
            inicio..posicionActual
        ];

    return new ValorNumero(
        int.Parse(textoNumero)
    );
}
}


