
var upper = false;
var lower = false;
var posicionales = new List<string>();

foreach (var a in args) {
    if (a == "-u" || a == "--upper") {
        upper = true;
        continue;
    }
    if (a == "-l" || a == "--lower") {
        lower = true;
        continue;
    }

    if (a == "--") {
        continue;
    }   

    posicionales.Add(a);
}

var input  = posicionales.Count > 0 ? posicionales[0] : string.Empty;
var output = posicionales.Count > 1 ? posicionales[1] : string.Empty;

var texto = string.Empty;
if(input == string.Empty) {
    texto = Console.In.ReadToEnd();
} else {
     texto = File.ReadAllText(input);

}
if (upper) {
    texto = texto.ToUpper();
} else if (lower) {
    texto = texto.ToLower();
}

if (string.IsNullOrWhiteSpace(output)) {
    Console.Write(texto);
} else {
    File.WriteAllText(output, texto);
}