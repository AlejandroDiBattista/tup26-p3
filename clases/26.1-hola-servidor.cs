#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false

// Configura el servidor web
const string host = "http://localhost:5001";

var builder = WebApplication.CreateBuilder(args); 
builder.WebHost.UseUrls(host); 
var app = builder.Build(); 

// Configura los endpoints
app.MapGet("/saludo", 
    () => $"Hola - Son las {DateTime.Now:HH:mm:ss}!"
); 

app.MapGet("/despedida", 
    () => $"Chau, nos vemos mañana"
); 

Console.Clear();
Console.WriteLine("=== Servidor de Hola ===\n");

// Inicia el servidor web y espera solicitudes HTTP
app.Run(); 
