List<Tarjeta> tarjetas = new List<Tarjeta> {
    new TarjetaCredito("1234 5678 9012 3456", "Juan Pérez", new DateTime(2025, 12, 31), 5000),
    new TarjetaDebito("9876 5432 1098 7654", "María Gómez", new DateTime(2024, 6, 30), "Banco XYZ", 2000),
    new TarjetaCredito("1111 2222 3333 4444", "Carlos López", new DateTime(2026, 3, 31), 10000)
};

foreach (var tarjeta in tarjetas) {
    Console.WriteLine($"Intentando pagar $1500 con {tarjeta}");
    tarjeta.Pagar(1500);
    Console.WriteLine();
}

void RepartirPago(double Monto, List<Tarjeta> tarjetas) {
    var parte = Monto / tarjetas.Count;
    foreach (var tarjeta in tarjetas) {
        Console.WriteLine($"Intentando pagar ${parte} con {tarjeta}");
        tarjeta.Pagar(parte);
        Console.WriteLine();
    }
}

interface ITarjeta {
    string Numero { get; }
    string Titular { get; }
    DateTime Vencimiento { get; }
    void Pagar(double Monto);
}

abstract class Tarjeta(string Numero, string Titular, DateTime Vencimiento) : ITarjeta {
    public abstract string Tipo { get; }
    public abstract void Pagar(double Monto);

    public override string ToString() => $"Tarjeta de {Tipo} | {Numero} de {Titular}, vence el {Vencimiento:MM/yyyy}";
}

class TarjetaCredito(string Numero, string Titular, DateTime Vencimiento, double Limite) : Tarjeta(Numero, Titular, Vencimiento) {
    public override string Tipo => "Crédito";   
    public override void Pagar(double Monto) {
        if (Monto > Limite) {
            Console.WriteLine($"No se puede pagar ${Monto} con {this}, límite excedido");
        } else {
            Console.WriteLine($"Pagando ${Monto} con {this}");
        }
    }
}

class TarjetaDebito(string Numero, string Titular, DateTime Vencimiento, string Banco, double Saldo) : Tarjeta(Numero, Titular, Vencimiento) {
    public override string Tipo => "Débito";
    public override void Pagar(double Monto) {
        if (Monto > Saldo) {
            Console.WriteLine($"No se puede pagar ${Monto} con {this}, saldo insuficiente");
        } else {
            Console.WriteLine($"Pagando ${Monto} con {this}");
        }
    }
}
