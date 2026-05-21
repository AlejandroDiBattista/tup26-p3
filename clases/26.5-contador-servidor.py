from fastapi import FastAPI, Response, status
import uvicorn
import os

host = "localhost"
port = 5001

app = FastAPI(title="Servidor de Contador")

contador = 0

@app.get("/contador")
def leer_contador():
    return {"contador": contador}

@app.post("/contador")
def incrementar_contador():
    global contador
    contador += 1
    return Response(status_code=status.HTTP_200_OK)

@app.delete("/contador")
def borrar_contador():
    global contador
    contador = 0
    return Response(status_code=status.HTTP_200_OK)

os.system("cls" if os.name == "nt" else "clear")
print("=== Servidor de Contador (Python) ===\n")
print(f"Docs:  http://{host}:{port}/docs")
print(f"OpenAPI: http://{host}:{port}/openapi.json\n")
uvicorn.run(app, host=host, port=port)