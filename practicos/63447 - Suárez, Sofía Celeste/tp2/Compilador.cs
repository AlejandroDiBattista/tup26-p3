namespace CalculadoraArimetica;
public class Compilador {
    private string _texto;
    private int _posicion;

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion)) {
            throw new FormatException("Token inesperado");
        }
        var compilador = new Compilador(expresion);
        Nodo resultado = compilador.ParseExpresion();
        return resultado;
    }



}
