const baseUrl = "http://localhost:5001/contador";
const host = "http://localhost:5001";

async function leerContador() {
  const response = await fetch(baseUrl);
  return await response.text();
}

async function enviarPost() {
  const response = await fetch(baseUrl, { method: "POST" });
  return response.ok;
}

async function enviarDelete() {
  const response = await fetch(baseUrl, { method: "DELETE" });
  return response.ok;
}

async function main() {
  console.clear();
  console.log(`=== Conectando a ${host} (JavaScript) ===\n`);
  console.log(`Estado inicial:  \n → ${await leerContador()}\n`);
  console.log(`Incrementar 1:   \n → ${await enviarPost()}\n`);
  console.log(`Incrementar 2:   \n → ${await enviarPost()}\n`);
  console.log(`Estado Contador: \n → ${await leerContador()}\n`);
  console.log(`Borrar contador: \n → ${await enviarDelete()}\n`);
  console.log(`Estado final:    \n → ${await leerContador()}\n\n`);
}

main();
