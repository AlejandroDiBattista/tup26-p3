let n = 10;
let s = "alejandro";
let a = [1, 2, 3, 4, 5];
let b = true;

let o = {
    numero: n,
    cadena: s,
    array: a,
    booleano: b
};

let datos = {
    "numero": 10,
    "cadena": "alejandro",
    "array": [ 1, 2, 3, 4, 5 ],
    "booleano": true
}

let json = JSON.stringify(datos, null, 4);
console.log(json);

const fs = require('fs');
fs.writeFileSync('datos.json', json);  

// leer el archivo
let contenido = fs.readFileSync('datos.json', 'utf-8');
let datosLeidos = JSON.parse(contenido);
console.log(datosLeidos);

