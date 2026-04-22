
var visa     = new TarjetaCredito("1234 5678 9012 3456", "Juan Pérez",  new DateTime(2027, 12, 31), 10000);
var maestro  = new TarjetaDebito("9876 5432 1098 7654", "María Gómez",  new DateTime(2027, 6, 30), "Banco XYZ", 5000);
var tarjetas = new List<Tarjeta> { visa, maestro };

void Repartir(double monto, List<Tarjeta> tarjetas) {
    var parte = monto / tarjetas.Count;
    foreach (var tarjeta in tarjetas) {
        if (tarjeta.Disponible >= parte) {
            Console.WriteLine($"Pagando ${parte} con {tarjeta}");
            tarjeta.Pagar(parte);
            Console.WriteLine();
        } else {
            Console.WriteLine($"No se puede pagar ${parte} con {tarjeta}, disponible insuficiente");
        }
    }
}

void PriorizarDisponibildad<T>(List<T> tarjetas) where T : IComparar<T> {
    tarjetas.Sort((t1, t2) => t2.Comparar(t1));
}

PriorizarDisponibildad(tarjetas);
Repartir(1500, tarjetas);


interface IComparar<T> {
    int Comparar(T other);
}

abstract class Tarjeta : IComparar<Tarjeta> {
    public string Numero { get; }
    public string Titular { get; }
    public DateTime Vencimiento { get; }

    public Tarjeta(string Numero, string Titular, DateTime Vencimiento) {
        if (!ValidarNumero(Numero)) {
            throw new ArgumentException("Número de tarjeta inválido");
        }
        if(Vencimiento < DateTime.Now) {
            throw new ArgumentException("La tarjeta ya venció");
        }
        this.Numero = Numero;
        this.Titular = Titular;
        this.Vencimiento = Vencimiento;
    }
    
    public abstract void Pagar(double Monto);
    public abstract double Disponible { get; }

    public override string ToString() => $"Tarjeta {Numero} de {Titular}, vence el {Vencimiento:MM/yyyy}";

    static bool ValidarNumero(string Numero) {
        // Validar formato de número de tarjeta (16 dígitos, espacios opcionales)
        var cleanNumber = Numero.Replace(" ", "");
        return cleanNumber.Length == 16 && cleanNumber.All(char.IsDigit);
    }

    public int Comparar(Tarjeta other) {
        if (other == null) return 1; // Esta tarjeta es mayor que null
        return this.Disponible.CompareTo(other.Disponible);
    }
}

class TarjetaCredito : Tarjeta {
    public double Limite { get; }

    public TarjetaCredito(string Numero, string Titular, DateTime Vencimiento, double Limite) : base(Numero, Titular, Vencimiento) {
        if (Limite < 0) {
            throw new ArgumentException("El límite no puede ser negativo");
        }

        this.Limite = Limite;
    }

    public override void Pagar(double Monto) {
        if (Monto > Limite) {
            Console.WriteLine($"No se puede pagar ${Monto} con {this}, límite excedido");
        } else {
            Console.WriteLine($"Pagando ${Monto} con {this}");
        }
    }
    public override double Disponible => Limite / 3; // Ejemplo de cálculo de disponible
}

class TarjetaDebito : Tarjeta {
    public string Banco { get; }
    public double Saldo { get; }

    public TarjetaDebito(string Numero, string Titular, DateTime Vencimiento, string Banco, double Saldo) : base(Numero, Titular, Vencimiento) {
        if (Saldo < 0) {
            throw new ArgumentException("El saldo no puede ser negativo");
        }

        this.Banco = Banco;
        this.Saldo = Saldo;
    }

    public override void Pagar(double Monto) {
        if (Monto > Saldo) {
            Console.WriteLine($"No se puede pagar ${Monto} con {this}, saldo insuficiente");
        } else {
            Console.WriteLine($"Pagando ${Monto} con {this}");
        }
    }
    public override double Disponible => Saldo; // Ejemplo de cálculo de disponible
}