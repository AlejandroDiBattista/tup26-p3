
class Program {
    static Predicate<int> MayorQue(int umbral) {
        return x => x > umbral;
    }

    static Predicate<int> Negar(Predicate<int> filtrar) {
        return x => !filtrar(x);
    }

    static void Mostrar(string titulo, IEnumerable<int> lista) {
        Console.WriteLine($"{titulo, 20}: {string.Join(", ", lista)}");
    }

    static void Main(string[] args) {
        List<int> numeros = new List<int> { 1, 2, 3, 4, 5 };

        Func<int, int> cuadrado       = x => x * x;
        Predicate<int> esPar          = x => x % 2 == 0;
        Predicate<int> menorIgualTres = Negar(MayorQue(3));

        var dobles    = numeros.Mapear(x => x * 2);
        var cuadrados = numeros.Mapear(cuadrado);
        var pares     = numeros.Filtrar(esPar);
        var impares   = numeros.Filtrar(Negar(esPar));
        var mayores3  = numeros.Filtrar(MayorQue(3));


        Mostrar("Impares", impares);
        Mostrar("Dobles", dobles);
        Mostrar("Cuadrados", cuadrados);
        Mostrar("Pares", pares);
        Mostrar("Mayores que tres", mayores3);
    }
}

static class Extensiones {
    extension<T>(IEnumerable<T> lista) {
        public IEnumerable<S> Mapear<S>(Func<T, S> transformar) {
            foreach (T item in lista) {
                yield return transformar(item);
            }
        }

        public IEnumerable<T> Filtrar(Predicate<T> filtrar) {
            foreach (T item in lista) {
                if (filtrar(item)) {
                    yield return item;
                }
            }
        }
    }
}

// Función que toma una lista de enteros y una función de transformación, y devuelve una nueva lista con los resultados de aplicar la función a cada elemento.
