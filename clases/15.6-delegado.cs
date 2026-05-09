var numeros = new List<int> { 1, 2, 3, 4, 5 };

foreach (int numero in Mapear(numeros, "doble")) {
    Console.WriteLine(numero);
}
foreach (int numero in Mapear(numeros, "triple")) {
    Console.WriteLine(numero);
}
IEnumerable<int> Mapear(IEnumerable<int> numeros, string operacion) {
    var resultado = new List<int>();
    foreach (int numero in numeros) {
        var y = operacion switch {
            "doble"    => 2 * numero,
            "triple"   => 3 * numero,
            "cuadrado" => numero * numero,
            "cubo"     => numero * numero * numero,
            "quintuple" => numero * 5,
            _ => throw new ArgumentException($"Operación desconocida: {operacion}")
        };
        resultado.Add(transformacion(numero));
    }
    return resultado;
}