using System.Globalization;
using static System.Console;

// Creamos una agenda de contactos en memoria para simular una base de datos.
List<Contacto> agenda = [
    new("Analia",   "Altamira",  5551234, 36),
    new("Beatriz",  "Benitez",   5555678, 41),
    new("Carlos",   "Córdoba",   5559012, 16),
    new("Diego",    "Dominguez", 5553456, 28),
    new("Elena",    "Espósito",  5557890, 22),
    new("Federico", "Fernandez", 5552345, 31),
    new("Gabriela", "Garcia",    5556789, 27),
    new("Hector",   "Hernandez", 5550123, 45),
];


// Preparar la consola para mostrar el menú.
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Este es el bucle principal del programa, que muestra el menú y responde a las opciones seleccionadas por el usuario.
while(true) {
    Clear();
    Titulo("Menú Principal");
    int opcion = Menu( "Listar Contactos", "Agregar Contacto", "Editar Contacto", "Borrar Contacto", "Salir");
    switch(opcion) {
        case 0: ListarContactos(); break;
        case 1: AgregarContacto(); break;
        case 2: EditarContacto();  break;
        case 3: BorrarContacto();  break;
        case 4: case -1: WriteLine("\nChau!"); return;
    }
}

// --- Funciones del sistema ---
void ListarContactos() {
    Clear();
    Titulo("LISTA DE CONTACTOS");

    var y = CursorTop;
    var pos = 0;
    while(true){
        Console.SetCursorPosition(0, y);
        for(int i = 0; i < agenda.Count; i++) {
            var c = agenda[i];
            MostrarCursor($"{i + 1}. {c.NombreCompleto.PadRight(30)} | {c.Telefono.ToString("000-0000").PadRight(15)} | {c.Edad.ToString().PadLeft(3)} años", i == pos);
        }

        var key = ReadKey(true).Key;
        if(key == ConsoleKey.Escape || key == ConsoleKey.Enter) { return; }
        
        MoverCursor(key, agenda.Count, ref pos);
    }
}

void AgregarContacto() {
    Clear();
    Titulo("Agregar Contacto");

    Contacto nuevo = new(
        Nombre:   Leer<string>("Nombre",   TryParseNombre),
        Apellido: Leer<string>("Apellido", TryParseNombre),
        Telefono: Leer<int>("Teléfono",    TryParseNumero),
        Edad:     Leer<int>("Edad",        TryParseEdad)
    );

    agenda.Add(nuevo);
    WriteLine();
    MostrarContacto(nuevo);
    Pausa("Contacto agregado. \n\nPresione una tecla para continuar...");
}

void EditarContacto() {
    Titulo("Editar Contacto");
    Pausa("Funcionalidad no implementada. \n\nPresione una tecla para continuar...");
}

void BorrarContacto() {
    Titulo("Borrar Contacto");
    Pausa("Funcionalidad no implementada. \n\nPresione una tecla para continuar...");
}

void MostrarContacto(Contacto contacto) {
    WriteLine($"      Nombre: {contacto.Nombre}");
    WriteLine($"    Apellido: {contacto.Apellido}");
    WriteLine($"    Teléfono: {contacto.Telefono.ToTelefono()}");
    WriteLine($"        Edad: {contacto.Edad}");
}

// --- Funciones de utilidad para la interfaz de consola ---

void MostrarCursor(string texto, bool seleccionada) {
    ForegroundColor = seleccionada ? ConsoleColor.Green : ConsoleColor.Gray;
    WriteLine($" {(seleccionada ? ">" : " ")} {texto}");
    ResetColor();
}

void MoverCursor(ConsoleKey key, int cantidad, ref int actual) {
    actual = key switch {
        ConsoleKey.UpArrow   => actual - 1,
        ConsoleKey.DownArrow => actual + 1,
        >= ConsoleKey.D0 and <= ConsoleKey.D9 => key - ConsoleKey.D1,
        _ => 0
    };
    actual = Math.Clamp(actual, 0, cantidad - 1);
}

void Titulo(string mensaje) {
    ForegroundColor = ConsoleColor.DarkBlue;
    WriteLine($"\n=== {mensaje} ===");
    ResetColor();
}

int Menu(params string[] opciones) {
    int y = CursorTop;
    int pos = 0;
    while(true) {
        SetCursorPosition(0, y);
        for(int i = 0; i < opciones.Length; i++) {
            MostrarCursor($"{i + 1}. {opciones[i]}", i == pos);
        }

        var key = ReadKey(true).Key;
        if(key == ConsoleKey.Escape) return -1;
        if(key == ConsoleKey.Enter)  return pos;
    
        MoverCursor(key, opciones.Length, ref pos);
    }
}

void Pausa(string mensaje = "Presione una tecla para continuar...") {
    WriteLine();
    ForegroundColor = ConsoleColor.DarkGray;
    Write(mensaje);
    ResetColor();
    ReadKey(true);
    LimpiarLinea();
}

T Leer<T>(string mensaje, Convertidor<T> convertir) {
    Console.ForegroundColor = ConsoleColor.Cyan;
    Write($"{mensaje.PadLeft(12)}: ");
    ResetColor();

    int y = CursorTop;

    while (true) {
        Console.SetCursorPosition(14, y);
        LimpiarLinea();

        var linea = ReadLine() ?? "";
        if (convertir(linea, out var valor)) {
            LimpiarLinea();
            return valor;
        }
    }
}

void MostrarError(string mensaje = "") {
    Console.SetCursorPosition(14, CursorTop);
    LimpiarLinea();
    ForegroundColor = ConsoleColor.Red;
    Write(mensaje);
    ResetColor();
}

void LimpiarLinea() {
    int x = CursorLeft, y = CursorTop;
    Write(new string(' ', WindowWidth - 1));
    Console.SetCursorPosition(x, y);
}

bool TryParseNombre(string texto, out string valor) {
    valor = texto.Trim().ToTitle();
    if( !string.IsNullOrWhiteSpace(valor)) { return true; }
    MostrarError("El nombre no puede estar vacío."); 
    return false;
}

bool TryParseNumero(string texto, out int valor) {
    if (int.TryParse(texto, out valor)) { return true; }
    MostrarError("Por favor, ingrese un número válido.");
    return false;
}

bool TryParseEdad(string texto, out int valor) {
    if (int.TryParse(texto, out valor) && valor >= 18 && valor <= 65) { return true; }
    MostrarError("Por favor, ingrese una edad válida (entre 18 y 65).");
    return false;
}


// --- Funciones de formato y modelo de datos ---

static class Formatos {
    public static string ToTitle(this string texto) {
        CultureInfo cultura = new("es-AR");
        return cultura.TextInfo.ToTitleCase( texto.ToLower(cultura) );
    }

    public static string ToTelefono(this int valor) {
        return valor.ToString("000-0000");
    }
}

// --- Modelo de datos ---

delegate bool Convertidor<T>(string texto, out T valor);

record Contacto(string Nombre, string Apellido, int Telefono, int Edad) {
    public string NombreCompleto => $"{Apellido}, {Nombre}";
    public override string ToString() => $"{Nombre} {Apellido} - {Telefono.ToTelefono()} - {Edad} años";
}

