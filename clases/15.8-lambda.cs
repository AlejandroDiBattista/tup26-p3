
int Doble(int x){ return x * 2;}
int Triple(int x) => x * 3;
int Cuadrado(int x) => x * x;
Cambiador Cubo = (x) => x * x * x;

Binario Suma = (a, b) => a + b;
Ternario y = (a, b, c) => a * b + c;

List<int> numeros = new List<int> { 1, 2, 3, 4, 5 };
foreach (int numero in numeros.Mapear(x => x * 3)) {
    Console.WriteLine(numero);
}
Console.WriteLine(y(2, 3, 4));

IEnumerable<int> Filtrar(IEnumerable<int> numeros, Filtro filtro) {
    foreach (int numero in numeros) {
        if (filtro(numero)) {
            yield return numero;
        }
    }
}
delegate int Binario(int a, int b);
delegate int Ternario(int a, int b, int c);

delegate int Cambiador(int x);
delegate bool Filtro(int x);

// Mapear -> map     --> Select
// Filtrar -> filter --> Where

static class IEnumerableExtensions {
    extension(IEnumerable<int> numeros) {
        IEnumerable<int> Mapear(Cambiador transformar) {
            foreach (int numero in numeros) {
                yield return transformar(numero);
            }
        }
    }
}

