#!/usr/bin/env -S dotnet run
#:property PublishAot=false

Console.CancelKeyPress += (_, e) => {
    Console.WriteLine("¡Cancelado!");
    e.Cancel = false;
};

while(true) {
    await Task.Delay(1000);
    Console.WriteLine($"Es la hora de {DateTime.Now:HH:mm:ss}");
}
