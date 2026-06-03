try
{
    Console.WriteLine("Programa iniciado");
}
catch (Exception e)
{
    Console.Error.WriteLine("Error: " + e.Message);
    Environment.Exit(1);
}