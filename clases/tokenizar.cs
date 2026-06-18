var Textos = @"""
ana ama a omar.
omar ama a ana.
omar no ama.
ana no nada.
omar nada.
ana nada.
omar anda.
ana anda.
dame amor.
dame una mano.
dame una moneda.
omar me da amor.
ana me da una moneda.
omar me da una mano.
ana me manda una moneda.
omar manda una nota.
ana manda una nota.
omar demora.
ana demora.
dame un remo.
omar rema.
ana rema.
omar nada en mar.
ana nada en mar.
omar anda en arena.
ana anda en arena.
omar ama a morena.
morena ama a omar.
ana ordena.
omar ordena.
""".Trim();


var tokenizador = new Tokenizador();
tokenizador.Aprender(Textos, 30);

var tokens = tokenizador.Tokenizar("ana ama a omar y omar ");
Console.WriteLine(string.Join(", ", tokens));

Console.WriteLine("Tokens aprendidos:");
foreach (var token in tokenizador.tokens) {
    Console.WriteLine($"{token,-10} - ID: {tokenizador.tokens.IndexOf(token),3}");
}

public class Tokenizador {
    public readonly List<string> tokens = [];

    public void Aprender(string texto, int cantidad) {
        var secuencia = Secuenciar(texto);

        foreach (var token in secuencia.Distinct()) {
            tokens.Add(token);
        }
        
        while (tokens.Count < cantidad && secuencia.Count > 1) {
            var token = BuscarParMasFrecuente(secuencia);
            tokens.Add(token);
            Fusionar(secuencia, token);
        }
    }

    public List<int> Tokenizar(string texto) {
        var secuencia = Secuenciar(texto);
        foreach (var regla in tokens.Where(t => t.Length > 1)) {
            Fusionar(secuencia, regla);
        }
        return secuencia.Select(token => tokens.IndexOf(token)).ToList();
    }

    // Datos un texto me da una lista de caracteres (tokens) que lo componen 
    // Ejemplo: "hola" -> ["h", "o", "l", "a"]
    private static List<string> Secuenciar(string texto) {
        return texto.Replace("\n", " ").Select(c => c.ToString()).ToList();
    }

    // Dada una lista de tokens, encuentra el par de tokens adyacentes que aparece con mayor frecuencia
    private static string BuscarParMasFrecuente(List<string> tokens) {
        var contador = new Dictionary<string, int>();

        for (var i = 0; i < tokens.Count - 1; i++) {
            var par = tokens[i] + tokens[i + 1];
            if (!contador.TryAdd(par, 1)) { contador[par]++; }
        }

        return contador.OrderByDescending(p => p.Value).First().Key;
    }

    // Dada una lista de tokens, reemplaza todas las ocurrencias de un par específico por un nuevo token
    // Ejemplo: secuencia = ["h", "o", "l", "a"], nuevo = "la" -> secuencia se convierte en ["h", "o", "la"]
    private static void Fusionar(List<string> secuencia, string nuevo ) {
        var i = 0;
        while (i < secuencia.Count - 1) {
            var token = secuencia[i] + secuencia[i + 1];
            if (token == nuevo) {
                secuencia[i] = nuevo;
                secuencia.RemoveAt(i + 1);
            } else {
                i++;
            }
        }
    }
}

// Ejemplo de uso:
// var tokenizador = new Tokenizador();
// tokenizador.Aprender("hola mundo este es un mundo todos dicen hola aunque hola no diga este mundo", 50);
// var tokens = tokenizador.Tokenizar("hola mundo");

// Inicialmente tendremos tokens individuales: ["h", "o", "l", "a", " ", "m", "u", "n", "d", "e", "s", "t", "a", ...]
// Luego, al aprender, se irán fusionando los pares más frecuentes: ["ho", "la", " ", "mu", "nd", "o", " ", "es", "t", "a"]
// Finalmente, al tokenizar "hola mundo", obtendremos los IDs correspondientes a los tokens fusionados: [ID("ho"), ID("la"), ID(" "), ID