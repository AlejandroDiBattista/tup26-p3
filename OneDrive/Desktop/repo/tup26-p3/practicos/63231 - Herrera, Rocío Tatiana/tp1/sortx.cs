
// sortx [input [output]] [-b|--by campo[:tipo[:orden]]]...
//       [-i|--input input] [-o|--output output]
//       [-d|--delimiter delimitador]
//       [-nh|--no-header] [-h|--help]

using System;
using System.Collections.Generic;
using System.IO;    
using System.Linq;
using System.Text;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Cryptography;

record SortField(string Name, bool Numeric, bool Descending);
record Appconfig(string ?InputFile, string ?OutputFile, char Delimiter, bool NoHeader, List<SortField> Fields);

var config=ParseArgs(args);
var text=ReadInput(config);
var (header,rows)=ParseDelimited(text,config.Delimiter,config.NoHeader);
SortRows(rows,config.Fields,header);
var output=Serialize(header,rows,config);
WriteOutput(output,config);

Appconfig ParseArgs(string [] a)
{
  string ?inputFile=null,outputFile=null;
   char delimiter=',';
   bool noHeader=false;
   var fields=new List<SortField>();

   for(int i = 0; i < a.Length; i++)
   {
      
      var x=a[i];
      if (x=="-i" || x == "--input") {if (++i>=a.Length)
      ExitError("Falta argumento para -i");
      inputFile=a[i];}
      else if (x=="-o" || x == "--output") {if (++i>=a.Length)      
      ExitError("Falta argumento para -o");
      outputFile=a[i];}
      else if (x=="-d" || x == "--delimiter") {if (++i>=a.Length)
      ExitError("Falta argumento para -d");
      delimiter=Unescape(a[i]); }
      
      else if (x=="-nh" || x == "--no-header"){noHeader=true;}
      else if (x=="-b" || x == "--by"){
      if (++i>=a.Length) ExitError("Falta argumento para -b");
      var p = a[i].Split(':');
      bool num=p.Length>1 && p[1].ToLowerInvariant()=="num";
      bool desc=p.Length>2 && p[2].ToLowerInvariant()=="desc";
      fields.Add(new SortField(p[0],num,desc));}
      else if(x=="-h" || x == "--help") {PrintHelp(); Environment.Exit(0);}
      else if (x.StartsWith("-")){
         ExitError("opcion desconocida: "+x);
      }
      else {
         if(inputFile==null) inputFile=x;
         else if(outputFile==null) outputFile=x;
         else ExitError("demasiados argumentos posicionales");
      }
    
   }
return new Appconfig(inputFile,outputFile,delimiter,noHeader,fields);
}

string ReadInput(Appconfig cfg){
try
{
return cfg.InputFile==null?Console.In.ReadToEnd():File.ReadAllText(cfg.InputFile);
}
catch(Exception ex){ExitError("Error leyendo entrada: "+ex.Message); return "";}
}

(string[] header, List<string[]> rows) ParseDelimited(string text, char delimi, bool noHeader)
{
   text=text.Replace("\r\n","\n").Replace("\r","\n");
   var lines=text.Split(delimi,StringSplitOptions.None).ToList();
   if(lines.Count>0 && lines.Last()=="") lines.RemoveAt(lines.Count-1);
   if(lines.Count==0) return (Array.Empty<string>(),new List<string[]>());

   var first=lines[0].Split(delimi,StringSplitOptions.None);
   int cols=first.Length;
   var rows=new List<string[]>();
   string [] header;

   if(noHeader) {header=Enumerable.Range(0,cols).Select(n=>n.ToString()).ToArray();
   rows.Add(Pad(first,cols));
   }
   else {header=first;
   }
   for(int i=noHeader?0:1;i<lines.Count;i++)
   {
     rows.Add(Pad(lines[i].Split(delimi,StringSplitOptions.None),cols));

    }
   return (header,rows);
}
string [] Pad(string [] arr, int n)
{
   if(arr.Length==n) return arr;
   var r=new string[n];
   Array.Copy(arr,r,Math.Min(arr.Length,n));
   for(int i=arr.Length;i<n;i++) r[i]="";
   return r;
}

void SortRows(List<string[]> rows, List<SortField> fields, string[] header)
{
   if(fields.Count==0) return;
   var rules=new List<(int idx,bool num,bool desc)>();
   foreach(var f in fields)
   {
      int idx=Array.IndexOf(header,f.Name);
      if(int.TryParse(f.Name,out int ni) && ni>=0 && ni<header.Length) idx=ni;
      if(idx<0) ExitError("Columna no encontrada: "+f.Name);
      rules.Add((idx,f.Numeric,f.Descending));
   }
   rows.Sort((a, b) =>
   {
      foreach (var(idx, num, desc) in rules)
      {
         var va = a[idx]??"";
         var vb = b[idx]??"";
         int cmp;
         if(num)
         {
            var okA = double.TryParse(va, NumberStyles.Any, CultureInfo.InvariantCulture, out var na);
            var okB = double.TryParse(vb, NumberStyles.Any, CultureInfo.InvariantCulture, out var nb);
            cmp=(okA && okB) ? na.CompareTo(nb) : string.Compare(va,vb, StringComparer.CurrentCulture);
         }
         else { cmp=string.Compare(va,vb, StringComparer.CurrentCulture); }
         if(cmp!=0) return desc?-cmp:cmp;
      }
      return 0;
   });
}

string Serialize(string[] header, List<string[]> rows, Appconfig cfg)
{
   var sb=new StringBuilder();
   if(!cfg.NoHeader && header.Length>0)
   {
      sb.AppendLine(string.Join(cfg.Delimiter.ToString(),header));
   }
   for(int i=0;i<rows.Count;i++)
   {
      sb.AppendLine(string.Join(cfg.Delimiter.ToString(),rows[i]));
   }
   var s=sb.ToString();
   if(s.EndsWith("\n")) s=s.Substring(0,s.Length-1);
   return s;
}
void WriteOutput(string outText, Appconfig cfg)
{
   try
   {
      if(cfg.OutputFile==null) Console.Write(outText);
      else File.WriteAllText(cfg.OutputFile,outText);
   }
   catch(Exception ex)
   {
      ExitError("Error escribiendo salida: "+ex.Message);
   }
}

static string Unescape(string s) =>
   s.Replace("\\n","\n").Replace("\\r","\r").Replace("\\t","\t");

static void ExitError(string message)
{
   Console.Error.WriteLine(message);
   Environment.Exit(1);
}

static void PrintHelp()
{
   Console.WriteLine("Uso: sortx [entrada [salida]] [-b campo[:tipo[:orden]]]... [-d delim] [-nh] [-h]");
   Console.WriteLine("tipo=num (numerico), orden=desc (descendente)");
   Console.WriteLine("Ejemplo: dotnet run sortx.cs empleados.csv salida.csv -b departamento -b salario:num:desc -d ,");
}