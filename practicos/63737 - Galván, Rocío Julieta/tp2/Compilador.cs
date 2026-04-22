class Compilador {
    public static Nodo Parse(string expresion) {
        throw new NotImplementedException("Implementar el parser para convertir la expresión en un AST.");
    }
}
class VariableNodo : Nodo {
    public override int Evaluar(int x) => x;
}
