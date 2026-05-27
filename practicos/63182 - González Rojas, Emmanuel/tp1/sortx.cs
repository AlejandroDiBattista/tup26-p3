using System;
using System.IO;
using System.Collections.Generic;




record CampoOrden(string Nombre, bool EsNumerico, bool EsDescendente);
record ConfiguracionApp(
    string? ArchivoEntrada, 
    string? ArchivoSalida, 
    string Delimitador, 
    bool SinEncabezado, 
    List<CampoOrden> CamposParaOrdenar);