using static System.Console ;
using Persona = (string nombre, int edad);

static class Program {
    static void WL(object x) => System.Console.WriteLine(x);

    static int Min(int[] arreglo) {
        int min = arreglo[0];
        foreach (var num in arreglo) {
            if (num < min) {
                min = num;
            }
        }
        return min;
    }

    static int Max(int[] arreglo) {
        int max = arreglo[0];
        foreach (var num in arreglo) {
            if (num > max) {
                max = num;
            }
        }
        return max;
    }

    static (int min, int max) MinMax(int[] arreglo) {
        int min = arreglo[0];
        int max = arreglo[0];
        foreach (var num in arreglo) {
            if (num < min) {
                min = num;
            }
            if (num > max) {
                max = num;
            }

        }
        return (min, max);
    }

    
    static void Main(string[] args) {
        var numeroso = new int[] { 1, 2, 32, 32, 31, 2 };
        var minimo = Min(numeroso);
        var maximo = Max(numeroso);
        var mm = MinMax(numeroso);
        WL($"El número mínimo en el arreglo es: {minimo}");
        WL($"El número máximo en el arreglo es: {maximo}");
        WL($"El número mínimo en el arreglo es: {mm.min}");
        WL($"El número máximo en el arreglo es: {mm.max}");

        var agenda = new (string nombre, int edad)[] {
            ("Adrian", 30),
            ("Maria", 25),
            ("Juan", 35)
        };
        foreach (var persona in agenda) {
            WL($"Nombre: {persona.nombre}, Edad: {persona.edad}");
        }

    }
}