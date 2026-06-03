using static System.Console;

Clear();
WriteLine(" 1. Texto en la consola");

// Caracteres de control  \r, \n, \t, \b, \f, \a (retorno de carro, nueva línea, tabulación, retroceso, salto de página, alerta)
WriteLine(" 2. Esto esta al comienzo\r 3. Esto esta al final de la línea");

// Caracter unicode 
WriteLine(" 4. Esto es un carácter unicode: \u2603");
// Carateres Unicode interesantes: 
// ☀ (sol) \u2600, ☁ (nube) \u2601, ☂ (paraguas) \u2602,
// ★ (estrella) \u2605, ☎ (teléfono) \u2606, ☕ (taza de café) \u2615   
// \u001b => Esc => Abre secuencia de control ANSI para colores y estilos, por ejemplo: \u001b[31m para texto rojo, \u001b[1m para negrita, \u001b[0m para resetear estilos.

WriteLine(" 5. \u2600 (sol) \u2601 (nube) \u2602 (paraguas) \u2605 (estrella) \u2606 (teléfono) \u2615 (taza de café)");

// Caracteres de control ANSI para colores y estilos
WriteLine(" 6. \u001b[31mTexto en rojo\u001b[0m");
WriteLine(" 7. \u001b[1mTexto en negrita\u001b[0m");
WriteLine(" 8. \u001b[4mTexto subrayado\u001b[0m");
WriteLine(" 9. \u001b[32m\u001b[1mTexto verde y negrita\u001b[0m");
WriteLine("10. \u001b[44mFondo azul\u001b[0m");

Mostrar(@"11. [rojo]Rojo[\], [negrita]Negrita[\], [verde]Verde[\], [azul]Azul[\], [subrayado]Subrayado[\]");

Console.ForegroundColor = ConsoleColor.Yellow;
Console.BackgroundColor = ConsoleColor.DarkRed;
WriteLine("12. Texto con colores usando Console.ForegroundColor y Console.BackgroundColor");
Console.ResetColor();
var anterior = Console.ForegroundColor;
Console.ForegroundColor = ConsoleColor.Magenta;
WriteLine("13. Cambiando el color de texto a magenta y guardando el color anterior");
Console.ForegroundColor = anterior;


// Una funcion para mostrar texto con formato personalizado usando etiquetas como [rojo], [verde], etc.
void Mostrar(string texto) {
    texto = texto.Replace("[rojo]", "\u001b[31m"); // Escapar caracteres de control para que se muestren literalmente
    texto = texto.Replace("[verde]", "\u001b[32m");
    texto = texto.Replace("[azul]", "\u001b[34m");
    texto = texto.Replace("[negrita]", "\u001b[1m");
    texto = texto.Replace("[subrayado]", "\u001b[4m");
    texto = texto.Replace(":sol:", "\u2600");
    texto = texto.Replace(":nube:", "\u2601");
    texto = texto.Replace(":mate:", "\u2615");
    texto = texto.Replace("[\\]", "\u001b[0m");
    WriteLine(texto);
}