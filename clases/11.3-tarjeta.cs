ITarjeta ale = new TarjetaDebito("1234 5678 9012 3456", "Alejandro Di Battista", new DateTime(2025, 12, 31), 2000);
ITarjeta mau = new TarjetaCredito("9876 5432 1098 7654", "Mauricio Gómez", new DateTime(2024, 6, 30), 5000);

void Repartir(double monto, List<ITarjeta> tarjetas) {
    var parte = monto / tarjetas.Count;
    foreach (var tarjeta in tarjetas) {
        if (tarjeta.Pagar(parte)) {
            Console.WriteLine($"Pago de ${parte} realizado con éxito con {tarjeta}");
        } else {
            Console.WriteLine($"No se pudo pagar ${parte} con {tarjeta}");
        }
        Console.WriteLine();
    }
}
Repartir(3000, new List<ITarjeta> { ale, mau });

if(ale.Pagar(1500)) {
    Console.WriteLine("Pago realizado con éxito");
} else {
    Console.WriteLine("No se pudo realizar el pago");
}
if(mau.Pagar(6000)) {
    Console.WriteLine("Pago realizado con éxito");
} else {
    Console.WriteLine("No se pudo realizar el pago");
}
Console.WriteLine(ale   );
Console.WriteLine(mau   );

interface ITarjeta {
    string Numero { get; }
    string Titular { get; }
    DateTime Vencimiento { get; }
    bool Pagar(double Monto);
}


abstract class Tarjeta : ITarjeta {
    public string Numero { get; }
    public string Titular { get; }
    public DateTime Vencimiento { get; }    

    public Tarjeta(string Numero, string Titular, DateTime Vencimiento) {
        
        if (!ValidarNumero(Numero)) {
            throw new ArgumentException("Número de tarjeta inválido");
        }
        if(string.IsNullOrEmpty(Titular)) {
            throw new ArgumentException("El titular de la tarjeta es requerido");
        }
        if(Vencimiento < DateTime.Now) {
            throw new ArgumentException("La tarjeta ya venció");
        }
        this.Numero = Numero;
        this.Titular = Titular;
        this.Vencimiento = Vencimiento;
    }

    private static bool ValidarNumero(string Numero) {
        var cleanNumber = Numero.Replace(" ", "");
        return cleanNumber.Length == 16 && cleanNumber.All(char.IsDigit);
    }

    public abstract bool Pagar(double Monto);

    public override string ToString() => $"Tarjeta {Numero} de {Titular}, vence el {Vencimiento:MM/yyyy}";

}

class TarjetaDebito : Tarjeta {
    public double Saldo { get; private set; }

    public TarjetaDebito(string Numero, string Titular, DateTime Vencimiento, double Saldo) : base(Numero, Titular, Vencimiento) {
        if(Saldo < 0.0) {
            throw new ArgumentException("El saldo no puede ser negativo");
        }
        this.Saldo = Saldo;
    }

    public override bool Pagar(double Monto) {
        if(Monto < 0) {
            throw new ArgumentException("El monto a pagar no puede ser negativo");  
        }
        if(Monto > this.Saldo) {
            Console.WriteLine($"No se puede pagar ${Monto} con {this}, saldo insuficiente");
            return false;
        }
        this.Saldo -= Monto;
        Console.WriteLine($"Pagando ${Monto} con {this}");
        return true;
    }

    public override string ToString() => $"Tarjeta {Numero} de {Titular}, vence el {Vencimiento:MM/yyyy} (con saldo ${Saldo})";

}

class TarjetaCredito : Tarjeta {
    public double Limite { get; private set; }
    public TarjetaCredito(string Numero, string Titular, DateTime Vencimiento, double Limite) : base(Numero, Titular, Vencimiento) {
        if(Limite < 0.0) {
            throw new ArgumentException("El límite no puede ser negativo");
        }
        this.Limite = Limite;
    }

    public override string ToString() => $"Tarjeta {Numero} de {Titular}, vence el {Vencimiento:MM/yyyy} (con límite ${Limite})";
    public bool Pagar(double Monto) {
        if(Monto < 0) {
            throw new ArgumentException("El monto a pagar no puede ser negativo");  
        }
        if(Monto > this.Limite) {
            Console.WriteLine($"No se puede pagar ${Monto} con {this}, límite excedido");
            return false;
        }
        this.Limite -= Monto;
        Console.WriteLine($"Pagando ${Monto} con {this}");
        return true;
    }

    var a = [10, 20, 30];
    var b = a;
    if(a == b) {
        Console.WriteLine("a y b son iguales");
    } else {
        Console.WriteLine("a y b son diferentes");
    }

    var uno = "Juan Perez";
    var nombre = "Juan";
    var apellido = "Perez";
    var otro = nombre + " " + apellido;
    
    if(uno == otro) {
        Console.WriteLine("Los nombres son iguales");
    } else {
        Console.WriteLine("Los nombres son diferentes");
    }