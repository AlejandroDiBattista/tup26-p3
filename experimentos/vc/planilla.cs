

record CeldaValor();

record CeldaNumero(double Valor) : CeldaValor;
record CeldaCadena(string Valor) : CeldaValor;
record CeldaError(string Mensaje) : CeldaValor;

record Celda(Direccion Direccion, string TextoOriginal, CeldaValor? Valor, Nodo? Formula);


record Nodo();
record Numero(double Valor) : Nodo;
record Cadena(string Valor) : Nodo;
record Direccion(string Nombre, int Fila, int Columna) : Nodo;
record Unario(string Operador, Nodo Operando) : Nodo;
record Binario(string Operador, Nodo Izquierdo, Nodo Derecho) : Nodo;
record Funcion(string Nombre, List<Nodo> Argumentos) : Nodo;
