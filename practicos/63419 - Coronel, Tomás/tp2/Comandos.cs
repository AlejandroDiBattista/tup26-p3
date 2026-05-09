using System;

class Comandos

{
    
  public bool Ayuda {get;}
  public bool Test {get;}
  public bool ModoDirecto {get;}
  public string Expresion {get;}
  public int Valor {get;}

  public Comandos(string[] args)
    {
        if (args.Length == 0)
        return;
         
         if (args[0] == "--help" || args[0] == "-h")
        {
            Ayuda = true;
            return;

      
        }

    
            if (args[0] == "--test" || args[0] == "-t")
            {
                Test = true;
                return;
            }
    
           if (args.Length == 2)
            {
                ModoDirecto = true;
                Expresion = args[0];
                if (!int.TryParse(args[1], out int v))
                {
                    Console.WriteLine("Valor inválido: " + args[1]);
                    Environment.Exit(1);
                }
                Valor = v;
            }
            else
            {
                Console.WriteLine("Uso:");
                Console.WriteLine("calculadora \"expresion\" valor");
                Console.WriteLine("calculadora --help");
                Console.WriteLine("calculadora --test");
                Environment.Exit(1);
            }
    
          
    
    }

}