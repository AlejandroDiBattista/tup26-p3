const express = require("express");

const app = express();
let contador = 0;

app.get("/contador", (req, res) => {
  res.json({ contador });
});

app.post("/contador", (req, res) => {
  contador++;
  res.sendStatus(200);
});

app.delete("/contador", (req, res) => {
  contador = 0;
  res.sendStatus(200);
});

console.clear();
console.log("=== Servidor contador (JavaScript) ===\n");
app.listen(5001);
