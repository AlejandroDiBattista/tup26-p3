ITarjeta ale = new TarjetaDebito("1234 5678 9012 3456", "Juan Pérez", new DateTime(2025, 12, 31), 1000.0);
ITarjeta mau = new TarjetaCredito("9876 5432 1098 7654", "Mauricio Gómez", new DateTime(2024, 6, 30), 5000.0);

void Repartir(double monto, List<ITarjeta> tarjetas) {
    var parte = monto / tarjetas.Count;
    foreach (var tarjeta in tarjetas) {
        if (tarjeta.Pagar(parte)) {
            Console.WriteLine($"{tarjeta.Titular} pago ${parte} ");
        } else {
            Console.WriteLine($"Che... {tarjeta.Titular} no se pudo pagar ${parte} con {tarjeta}");
        }
        Console.WriteLine();
    }
}

interface ITarjeta {
    string Numero { get; }
    string Titular { get; }
    DateTime Vencimiento { get; }
    bool Pagar(double Monto);
}

abstract class Tarjeta : ITarjeta, IEquatable<Tarjeta> {
    public string Numero { get; private set; }
    public string Titular { get; private set; }
    public DateTime Vencimiento { get; private set; }

    public Tarjeta(string Numero, string Titular, DateTime Vencimiento) {
        if(!ValidarNumero(Numero)) {
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

        bool Equals(Tarjeta? other) {
            if(other == null) return false;
            return Numero == other.Numero;
        }
    }

    public abstract bool Pagar(double Monto);

    public override string ToString() => $"Tarjeta {Numero} de {Titular}, vence el {Vencimiento:MM/yyyy}";
    
    static bool ValidarNumero(string Numero) {
        var cleanNumber = Numero.Replace(" ", "");
        return cleanNumber.Length == 16 &&  cleanNumber.All(char.IsDigit);
    }

}

class TarjetaDebito : Tarjeta {
    public double Saldo { get; private set; }

    public TarjetaDebito(string Numero, string Titular, DateTime Vencimiento, double Saldo) : base(Numero, Titular, Vencimiento) {
        if(Saldo < 0.0) {
            throw new ArgumentException("El saldo no puede ser negativo");
        }
        this.Saldo = Saldo;
    }

    public override string ToString() => $"Tarjeta de débito {Numero} de {Titular}, vence el {Vencimiento:MM/yyyy}, saldo ${Saldo}";
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
    

}

class TarjetaCredito : Tarjeta {
    public double Limite { get; private set; }

    public TarjetaCredito(string Numero, string Titular, DateTime Vencimiento, double Limite) : base(Numero, Titular, Vencimiento) {
        if(Limite < 0.0) {
            throw new ArgumentException("El límite no puede ser negativo");
        }
        this.Limite = Limite;
    }

    public override bool Pagar(double Monto) {
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
    public override string ToString() => $"Tarjeta de crédito {Numero} de {Titular}, vence el {Vencimiento:MM/yyyy}, límite ${Limite}";

}
