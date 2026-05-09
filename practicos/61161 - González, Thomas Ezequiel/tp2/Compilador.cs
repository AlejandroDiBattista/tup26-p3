
class Compilador
{
    private string texto;
    private int posicion;

    private Compilador(string texto) 
    {
        this.texto = texto;
        this.posicion = 0;
    }

    public static Nodo Parse(string expresion)
{
    var compilador = new Compilador(expresion);
    return compilador.ParsearExpresion();
}

    private char Actual()
    {
        if (posicion >= texto.Length)// Si hemos llegado al final del texto, devolvemos un carácter nulo
            return '\0'; 

        return texto[posicion];
    }

    private void Avanzar()// Avanza a la siguiente posición en el texto
    {
        posicion++;
    }

    private Nodo ParsearNumero()// Analiza un número en el texto y devuelve un nodo que representa ese número
    {
        int inicio = posicion;

        while (char.IsDigit(Actual()))
        {
            Avanzar();// Avanza mientras el carácter actual sea un dígito
        }

        string numeroTexto = texto.Substring(inicio, posicion - inicio);

        if (numeroTexto.Length == 0)
            throw new Exception("Se esperaba un número");// Si no se encontró ningún dígito, lanzamos una excepción

        int valor = int.Parse(numeroTexto);

        return new NumeroNodo(valor);
    }
    
    private Nodo ParsearFactor()
{
    // Ignorar espacios
    while (char.IsWhiteSpace(Actual()))
        Avanzar();

    char actual = Actual();

    // 🔹 Número
    if (char.IsDigit(actual))
    {
        return ParsearNumero();
    }

    // 🔹 Variable x
    if (actual == 'x' || actual == 'X')
    {
        Avanzar();
        return new VariableNodo();
    }

    // 🔹 Unario +
    if (actual == '+')
    {
        Avanzar();
        return ParsearFactor();
    }

    // 🔹 Unario -
    if (actual == '-')
    {
        Avanzar();
        return new NegativoNodo(ParsearFactor());
    }

    // 🔹 Paréntesis
    if (actual == '(')
    {
        Avanzar(); // consumir '('
        var nodo = ParsearExpresion();

        if (Actual() != ')')
            throw new FormatException("Se esperaba ')'");
            
        Avanzar(); // consumir ')'
        return nodo;
    }

    throw new FormatException("Token inesperado");
}
private Nodo ParsearTermino()
{
    var nodo = ParsearFactor();

    while (true)
    {
        while (char.IsWhiteSpace(Actual()))
            Avanzar();

        if (Actual() == '*')
        {
            Avanzar();
            nodo = new MultiplicacionNodo(nodo, ParsearFactor());
        }
        else if (Actual() == '/')
        {
            Avanzar();
            nodo = new DivisionNodo(nodo, ParsearFactor());
        }
        else
        {
            break;
        }
    }

    return nodo;
}
private Nodo ParsearExpresion()
{
    var nodo = ParsearTermino();

    while (true)
    {
        while (char.IsWhiteSpace(Actual()))
            Avanzar();

        if (Actual() == '+')
        {
            Avanzar();
            nodo = new SumaNodo(nodo, ParsearTermino());
        }
        else if (Actual() == '-')
        {
            Avanzar();
            nodo = new RestaNodo(nodo, ParsearTermino());
        }
        else
        {
            break;
        }
    }

    return nodo;
}
}
