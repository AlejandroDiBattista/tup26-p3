using static System.Console;
Clear();

int a = int.Parse("10");

if(int.TryParse("10", out int b)) {
    WriteLine(b);
} else {
    WriteLine("No se pudo convertir el valor a un número entero.");
}   

float f = float.Parse("3.14");


WriteLine(a);

if(float.TryParse("3.14", out float c)) {
    WriteLine(c);
} else {
    WriteLine("No se pudo convertir el valor a un número entero.");
}

bool EsNumeroValido(string valor) {
    return int.TryParse(valor, out _);
}
// Nara, Wanda -> ["Nara", "Wanda"]
// Wanda Solange Nara  -> ["Nara", "Wanda Solange"]

(string Apellido, string Nombre) SepararNombreApellido(string nombreCompleto) {
    if(nombreCompleto.Contains(", ")) {
        var partes = nombreCompleto.Split(", ");
        return (partes[0], partes[1]);
    } else  if(nombreCompleto.Contains(" ")) {
        var partes = nombreCompleto.Split(" ");
        var apellido = partes[^1];
        var nombre = string.Join(" ", partes[0..^1]);
        return (apellido, nombre);
    } else {
        return (nombreCompleto, "");
    }
}

var nombreCompleto = "Pérez, Juan Carlos";
string[] partes = nombreCompleto.Split(", ");
var apellido = partes[0];
var nombre = partes[1];

var resultado = SepararNombreApellido("Pérez, Juan Carlos");
WriteLine($"Apellido: {resultado.Apellido}, Nombre: {resultado.Nombre}");


(int x, int y) = (10, 20);
int[] par = [10, 20];

var coordenada = (longitud: 10, latitud: 20);
var color = (r: 255, g: 0, b: 0);


