// Argumentos -> Valores que se pasan a un programa o función al momento de su ejecución
// En C#, los argumentos se reciben como un arreglo de cadenas en las sentencias de nivel superior.

// Imprimir los argumentos recibidos
Console.WriteLine("Argumentos recibidos:");
foreach (var arg in args) {
    Console.WriteLine(arg);
}

// Como se ejecuta pasando argumentos:
// `dotnet run -- arg1 arg2 arg3` (para un proyecto de consola en .NET)
// `dotnet run 03-argumentos.cs -- arg1 arg2 arg3` (para un script de C# sin proyecto - file based)

// Con alias 
// >alias p3="dotnet 03-argumentos.cs --"
// >p3 arg1 arg2 arg3
// En PowerShell
// >Set-Alias p3 "dotnet 03-argumentos.cs --"
// >p3 arg1 arg2 arg3
// En Windows Command Prompt (CMD)
// >doskey p3=dotnet 03-argumentos.cs -- $*
// >p3 arg1 arg2 arg3
