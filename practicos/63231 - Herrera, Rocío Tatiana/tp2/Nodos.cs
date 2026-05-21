using System.Diagnostics.Contracts;

abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

public abstract class Expresion
{ public abstract int Calcular(int x=0);
}
 
public class constante : Expresion
{
    private readonly int valor;
    public constante(int valor) => valor=valor;
    public override int Calcular(int x=0) => valor;
}

public class variable : Expresion
{
    public override int Calcular (int x=0) => x;
}
  
public class UnarioNegativo : Expresion {
    private readonly Expresion _exp;
    public UnarioNegativo(Expresion exp) => _exp = exp;
    public override int Calcular(int x = 0) => -_exp.Calcular(x);
}

public abstract class Binario : Expresion {
    protected readonly Expresion Izq;
    protected readonly Expresion Der;

    protected Binario(Expresion izq, Expresion der) {
        Izq = izq;
        Der = der;
    }
}


public class Suma : Binario {
    public Suma(Expresion izq, Expresion der) : base(izq, der) { }
    public override int Calcular(int x = 0) => Izq.Calcular(x) + Der.Calcular(x);
}

public class Resta : Binario {
    public Resta(Expresion izq, Expresion der) : base(izq, der) { }
    public override int Calcular(int x = 0) => Izq.Calcular(x) - Der.Calcular(x);
}

public class Multiplicacion : Binario {
    public Multiplicacion(Expresion izq, Expresion der) : base(izq, der) { }
    public override int Calcular(int x = 0) => Izq.Calcular(x) * Der.Calcular(x);
}

public class Division : Binario
{
    public Cociente (Expresion izq, Expresion der) : base(izq, der) { }
    public override int Calcular(int x = 0) {
        int divisor = Der.Calcular(x);
        if (divisor == 0) {
            throw new DivideByZeroException("No se puede dividir por cero.");
        }
        return Izq.Calcular(x) / divisor;
    }
}
