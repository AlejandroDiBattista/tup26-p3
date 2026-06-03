using static System.Console;
class Program {

    static void Mostrar<T>(string titulo, T[] arreglo) {
        WriteLine($"\n {titulo}\n [{string.Join(", ", arreglo)}]");
    }
    
    static void Mostrar<T>(string titulo, List<T> arreglo) {
        WriteLine($"\n {titulo}\n [{string.Join(", ", arreglo)}]");
    }


    static void Main() {
        Clear();
        int[] pares = {2, 4, 6, 8, 10};
        List<int> impares = [1, 3, 5, 7, 9];
        Mostrar("Arreglo de pares", pares);
        Mostrar("Arreglo de impares", impares);
        pares[0] = 20; // Modifica el primer elemento del arreglo
        Mostrar("Arreglo de pares después de modificar", pares);
        impares[0] = 11; // Modifica el primer elemento de la lista
        Mostrar("Arreglo de impares después de modificar", impares);

        var nuevos = impares.OrderBy(x => x).ToList(); // Ordena la lista de impares
        Mostrar("Arreglo de impares después de ordenar", nuevos);

    }

}