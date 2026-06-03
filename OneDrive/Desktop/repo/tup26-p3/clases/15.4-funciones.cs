
Transformacion doble    = x => 2 * x;
Transformacion triple   = x => 3 * x;
Transformacion cuadrado = x => x * x;
Transformacion cubo     = x => x * x * x;

var numeros = new List<int> { 1, 2, 3, 4, 5 };

var d = Mapear(numeros, x => 2 * x);
var t = Mapear(numeros, x => 3 * x);
var c = Mapear(numeros, x => x * x);
var u = Mapear(numeros, x => x * x * x);
var x = Mapear(numeros, x => x * x * x * x);

var esPar = x => x % 2 == 0;
var esImpar = x => x % 2 != 0;

var pares = Filtrar(numeros, esPar);
var impares = Filtrar(numeros, x => x % 2 != 0);

var mayores = Filtrar(numeros, x => x >= 18);

var mayorTres= x => x > 3;



IEnumerable<int> Filtrar(IEnumerable<int> numeros, Filtro filtrar) {
    // var resultado = new List<int>();
    foreach (int numero in numeros) {
        if (filtrar(numero)) {
            yield return numero;
        }
    }
}

IEnumerable<S> Mapear<E, S>(IEnumerable<E> numeros, Func<E, S> transformacion) {
    var resultado = new List<S>();
    foreach (E numero in numeros) {
        resultado.Add(transformacion(numero));
    }
    return resultado;
}

