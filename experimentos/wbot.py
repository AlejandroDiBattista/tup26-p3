import logging
import os
import signal
import threading
import time
from pathlib import Path

from agents import Agent, Runner, SQLiteSession, function_tool
from neonize.client import NewClient
from neonize.events import ConnectedEv, MessageEv
from neonize.utils.message import extract_text

raiz = Path(__file__).resolve().parent

logging.getLogger("httpx").setLevel(logging.WARNING)
logging.getLogger("httpcore").setLevel(logging.WARNING)
logging.getLogger("openai").setLevel(logging.WARNING)

@function_tool
def leer_archivo(ruta: str) -> str:
    """Lee un archivo de texto dentro del directorio del bot."""
    path = (raiz / ruta).resolve()
    path.relative_to(raiz)
    return path.read_text(encoding="utf-8")

@function_tool
def escribir_archivo(ruta: str, contenido: str) -> str:
    """Escribe un archivo de texto dentro del directorio del bot."""
    path = (raiz / ruta).resolve()
    path.relative_to(raiz)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(contenido, encoding="utf-8")
    return "ok"

agent = Agent(
    name="Whaty", model= "gpt-5.4-mini-2026-03-17",
    instructions= "Sos un ayudante de catedra de Programación III que colabora usando WhatsApp. Contestá en español, breve y claro. Los datos de los alumnos estan en 'alumnos.md'",
    tools=[leer_archivo, escribir_archivo],
)

client   = NewClient( "whaty")
db       = "./whaty.sessions.db"
sessions = {}

@client.event(ConnectedEv)
def on_connected(_: NewClient, __: ConnectedEv) -> None:
    print("Whaty conectado. Si es la primera vez, escaneá el QR que muestre Neonize.")

@client.event(MessageEv)
def on_message(client: NewClient, event: MessageEv) -> None:
    source = event.Info.MessageSource
    chat   = source.Chat
    is_me  = source.IsFromMe
    text   = extract_text(event.Message).strip()

    if is_me or not chat or not text:
        return

    key = str(chat)
    session = sessions.setdefault( key, SQLiteSession(f"whatsapp:{key}", db_path=db), )
    result  = Runner.run_sync(agent, text, session=session)
    client.send_message(chat, str(result.final_output))

if __name__ == "__main__":
    # signal.signal(signal.SIGINT, lambda *_: os._exit(130))
    threading.Thread(target=client.connect, daemon=True).start()

    while True:
        time.sleep(1)
