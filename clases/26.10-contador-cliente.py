import urllib.request
import os

host = "http://localhost:5001"
base_url = f"{host}/contador"

def leer_contador():
    with urllib.request.urlopen(base_url) as response:
        return response.read().decode("utf-8")


def enviar_post():
    request = urllib.request.Request(base_url, method="POST")
    with urllib.request.urlopen(request) as response:
        return response.status == 200

def enviar_delete():
    request = urllib.request.Request(base_url, method="DELETE")
    with urllib.request.urlopen(request) as response:
        return response.status == 200

os.system("clear")
print(f"=== Conectando a {host} (Python) ===\n")
print(f"Estado inicial:  \n → {leer_contador()}\n")
print(f"Incrementar 1:   \n → {enviar_post()}\n")
print(f"Incrementar 2:   \n → {enviar_post()}\n")
print(f"Estado Contador: \n → {leer_contador()}\n")
print(f"Borrar contador: \n → {enviar_delete()}\n")
print(f"Estado final:    \n → {leer_contador()}\n\n")
