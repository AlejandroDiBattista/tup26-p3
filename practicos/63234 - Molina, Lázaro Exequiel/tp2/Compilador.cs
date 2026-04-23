class Compilador
{
    private string texto;
    private int posicion;

    private Compilador(string expresion)
    {
        texto = expresion.Replace(" ", "");
        posicion = 0;
    }
