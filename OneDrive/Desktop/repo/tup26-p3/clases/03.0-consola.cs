
string LeerCadena(string mensaje) {
    while (true) {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"{mensaje.PadRight(10)} > ");
        Console.ForegroundColor = ConsoleColor.Green;
        var salida = Console.ReadLine();
        Console.ResetColor();
        if (!string.IsNullOrEmpty(salida)) {
            return salida;
        }
        LimpiarLineaActual();
    }
}

void Pausa() {
    Console.WriteLine("Presione cualquier tecla para continuar...");
    Console.ReadKey();
}


void Titulo(string mensaje) {
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.BackgroundColor = ConsoleColor.DarkBlue;
    Console.WriteLine($"| {mensaje.PadRight(20, '-')}|");
    Console.ResetColor();
}

int LeerEntero(string mensaje) {
    while (true) {
        var salida = LeerCadena(mensaje);
        if (int.TryParse(salida, out var numero)) {
            return numero;
        }
        LimpiarLineaActual();
    }
}

void LimpiarLineaActual() {
    Console.CursorTop--;
	Console.Write(new string(' ', Console.WindowWidth - 1));
	Console.SetCursorPosition(0, Console.CursorTop);
}


int Menu() {
    while (true) {
        Console.Clear();
        Titulo("MENU PRINCIPAL");
        Console.WriteLine("1. Agregar cliente");
        Console.WriteLine("2. Salir");
        var opcion = LeerEntero("Opcion");
            if (opcion == 1 || opcion == 2) {
                return opcion;
            }
        }
}


(string Nombre, string Apellido, int Edad) LeerCliente() {
    Titulo("AGREGANDO CLIENTE");
    var nombre   = LeerCadena("Nombre");
    var apellido = LeerCadena("Apellido");
    var edad     = LeerEntero("Edad");
    return (nombre, apellido, edad);
}


while (true) {
    var opcion = Menu();
    if (opcion == 1) {
        var cliente = LeerCliente();
        Console.WriteLine($"\nCliente agregado: {cliente.Nombre} {cliente.Apellido}, {cliente.Edad} años\n");
        Pausa();
    } else if (opcion == 2) {
        break;
    } else {
        Console.WriteLine("Opcion no valida");
    }
}
