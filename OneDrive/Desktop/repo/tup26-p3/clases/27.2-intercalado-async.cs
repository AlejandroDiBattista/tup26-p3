using System;
using System.Threading.Tasks;

// Objeto de bloqueo para evitar que los colores de la consola se mezclen si imprimen exactamente al mismo tiempo.
object lockObject = new();

void Log(string mensaje, ConsoleColor color) {
    lock (lockObject) {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {mensaje}");
        Console.ResetColor();
    }
}

// Función asincrónica que simula una tarea que reporta progreso cada segundo
async Task TareaSimuladaAsync(string nombre, int duracionSegundos, ConsoleColor color) {
    Log($"➔ {nombre} Iniciada. Duración: {duracionSegundos}s", color);
    
    for (int i = 1; i <= duracionSegundos; i++) {
        // Task.Delay(1000) simula una espera de 1 segundo de forma no bloqueante
        await Task.Delay(1000); 
        
        Log($"  [{nombre}] Progreso: {i}/{duracionSegundos}s", color);
    }
    
    Log($"✔ {nombre} Finalizada!", color);
}

// === EJECUCIÓN PRINCIPAL ===

Log("Iniciando programa de demostración asincrónica...", ConsoleColor.White);
Log("Disparando 3 tareas al mismo tiempo (en paralelo) de forma no bloqueante...", ConsoleColor.White);
Console.WriteLine("------------------------------------------------------------------");

// Disparamos las tareas sin usar 'await' inmediatamente. Esto inicia su ejecución en segundo plano.
Task tareaA = TareaSimuladaAsync("Tarea Roja", 5, ConsoleColor.Red);
Task tareaB = TareaSimuladaAsync("Tarea Verde", 3, ConsoleColor.Green);
Task tareaC = TareaSimuladaAsync("Tarea Azul", 6, ConsoleColor.Blue);

Log("Las tareas ya están corriendo. El hilo principal está libre y esperando que todas terminen...", ConsoleColor.Yellow);
Console.WriteLine("------------------------------------------------------------------");

// Ahora sí, esperamos asincrónicamente a que las tres finalicen
await Task.WhenAll(tareaA, tareaB, tareaC);

Console.WriteLine("------------------------------------------------------------------");
Log("Todas las tareas asincrónicas han finalizado. Programa terminado.", ConsoleColor.White);
