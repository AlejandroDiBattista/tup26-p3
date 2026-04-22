var a = new Persona("Juan", "Pérez");
var b = new Persona("Juan", "Pérez");
if(a.Equals(b)) { // a == b
    Console.WriteLine("a y b son iguales");
} else {
    Console.WriteLine("a y b son diferentes");
}

class Persona(string Nombre, string Apellido) :IEquatable<Persona> {
    public string NombreCompleto => $"{Nombre} {Apellido}";

    public bool Equals(Persona? other) {
        if(other == null) return false;
        return Nombre == other.Nombre && Apellido == other.Apellido;
    }
}

var a = 10;
var b = 5 + 5;

if(a == b) {
    Console.WriteLine("a y b son iguales");
} else {
    Console.WriteLine("a y b son diferentes");
}   

var uno = "Juan Pérez";
var nombre = "Juan";
var apellido = "Pérez";
var otro = nombre + " " + apellido;

if(uno == otro) {
    Console.WriteLine("uno y otro son iguales");
} else {
    Console.WriteLine("uno y otro son diferentes");
}

var v = [10, 20, 30];
var w = v;

bool EsIgual(int[] x, int[] y) {
    if(x.Length != y.Length) return false;
    for(int i = 0; i < x.Length; i++) {
        if(x[i] != y[i]) return false;
    }
    return true;
}

if(v == w) {
    Console.WriteLine("v y w son iguales");
} else {
    Console.WriteLine("v y w son diferentes");
}