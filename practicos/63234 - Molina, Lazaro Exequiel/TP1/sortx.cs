using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

var cfg = ParseArgs(args);
var text = ReadInput(cfg);
var (header, rows) = ParseDelimited(text, cfg.Delimiter, cfg.NoHeader);
SortRows(rows, header, cfg.SortFields);
var output = Serialize(header, rows, cfg);
WriteOutput(output, cfg);

AppConfig ParseArgs(string[] a){
    string? inputFile = null, outputFile = null;
    string delim = ",";
    bool noHeader = false;
    var fields = new List<SortField>();
    for(int k=0;k<a.Length;k++){
        var x = a[k];
        if(x=="-i"||x=="--input"){ if(++k>=a.Length) ExitWithError("falta argumento para -i"); inputFile = a[k]; }
        else if(x=="-o"||x=="--output"){ if(++k>=a.Length) ExitWithError("falta argumento para -o"); outputFile = a[k]; }
        else if(x=="-d"||x=="--delimiter"){ if(++k>=a.Length) ExitWithError("falta argumento para -d"); delim = Unescape(a[k]); }
        else if(x=="-nh"||x=="--no-header"){ noHeader = true; }
        else if(x=="-b"||x=="--by"){
            if(++k>=a.Length) ExitWithError("falta especificación para -b");
            var p = a[k].Split(':');
            bool num = p.Length>1 && p[1].ToLowerInvariant()=="num";
            bool desc = p.Length>2 && p[2].ToLowerInvariant()=="desc";
            fields.Add(new SortField(p[0], num, desc));
        }
        else if(x=="-h"||x=="--help"){ PrintHelp(); Environment.Exit(0); }
        else if(!x.StartsWith("-")){ if(inputFile==null) inputFile = x; else if(outputFile==null) outputFile = x; else ExitWithError("demasiados argumentos posicionales"); }
        else ExitWithError("opción desconocida: "+x);
    }
    return new AppConfig(inputFile, outputFile, delim, noHeader, fields);
}

string ReadInput(AppConfig cfg){
    try{
        return cfg.InputFile == null ? Console.In.ReadToEnd() : File.ReadAllText(cfg.InputFile);
    } catch(Exception ex){
        ExitWithError("error leyendo entrada: " + ex.Message);
        return "";
    }
}

(string[] header, List<string[]> rows) ParseDelimited(string text, string delim, bool noHeader){
    text = text.Replace("\r\n","\n").Replace("\r","\n");
    var lines = text.Split('\n', StringSplitOptions.None).ToList();
    if(lines.Count>0 && lines[^1]=="") lines.RemoveAt(lines.Count-1);
    if(lines.Count==0) return (Array.Empty<string>(), new List<string[]>());
    var first = lines[0].Split(new[]{delim}, StringSplitOptions.None);
    int cols = first.Length;
    var rows = new List<string[]>();
    string[] header;
    if(noHeader){
        header = Enumerable.Range(0,cols).Select(i=>i.ToString()).ToArray();
        rows.Add(Pad(first, cols));
    } else header = first;
    for(int i=1;i<lines.Count;i++) rows.Add(Pad(lines[i].Split(new[]{delim}, StringSplitOptions.None), cols));
    return (header, rows);
}
string[] Pad(string[] a,int n){ if(a.Length==n) return a; var r=new string[n]; Array.Copy(a,r,Math.Min(a.Length,n)); for(int i=a.Length;i<n;i++) r[i]=""; return r; }

void SortRows(List<string[]> rows, string[] header, List<SortField> sortFields){
    if(sortFields.Count==0) return;
    var rules = new List<(int idx,bool num,bool desc)>();
    foreach(var sf in sortFields){
        int idx = Array.IndexOf(header, sf.Name);
        if(idx<0){ if(int.TryParse(sf.Name, out var ni) && ni>=0 && ni<header.Length) idx=ni; else ExitWithError($"columna no encontrada: {sf.Name}"); }
        rules.Add((idx, sf.Numeric, sf.Descending));
    }
    rows.Sort((a,b)=>{
        foreach(var (idx,num,desc) in rules){
            var va = a[idx] ?? ""; var vb = b[idx] ?? "";
            int cmp;
            if(num){
                var okA = double.TryParse(va, NumberStyles.Any, CultureInfo.InvariantCulture, out var na);
                var okB = double.TryParse(vb, NumberStyles.Any, CultureInfo.InvariantCulture, out var nb);
                cmp = (okA && okB) ? na.CompareTo(nb) : StringComparer.CurrentCulture.Compare(va, vb);
            } else cmp = StringComparer.CurrentCulture.Compare(va, vb);
            if(cmp!=0) return desc ? -cmp : cmp;
        }
        return 0;
    });
}

string Serialize(string[] header, List<string[]> rows, AppConfig cfg){
    var sb = new StringBuilder();
    if(!cfg.NoHeader && header.Length>0) sb.AppendLine(string.Join(cfg.Delimiter, header));
    for(int i=0;i<rows.Count;i++) sb.AppendLine(string.Join(cfg.Delimiter, rows[i]));
    var s = sb.ToString(); if(s.EndsWith("\n")) s = s.Substring(0, s.Length-1); return s;
}

void WriteOutput(string text, AppConfig cfg){
    try{
        if(cfg.OutputFile==null) Console.Write(text); else File.WriteAllText(cfg.OutputFile, text);
    } catch(Exception ex){ ExitWithError("error escribiendo salida: " + ex.Message); }
}

static string Unescape(string s) => s.Replace("\\t","\t").Replace("\\n","\n").Replace("\\r","\r");
static void ExitWithError(string msg){ Console.Error.WriteLine(msg); Environment.Exit(1); }
static void PrintHelp(){ Console.WriteLine("Uso: sortx [entrada [salida]] -b campo[:tipo[:orden]] [-d delim] [-nh] [-h]\nTipo: num para numérico. Orden: desc para descendente."); }
record SortField(string Name, bool Numeric, bool Descending);
record AppConfig(string? InputFile, string? OutputFile, string Delimiter, bool NoHeader, List<SortField> SortFields);
