import os
import signal
import threading
import time

from agents import Agent, Runner, SQLiteSession
from neonize.client import NewClient
from neonize.events import ConnectedEv, MessageEv
from neonize.utils.message import extract_text

agent = Agent(
    name="Whaty", model= "gpt-5.4-mini-2026-03-17",
    instructions= "Sos un asistente que responde conversaciones de WhatsApp. Contestá en español, breve y claro."
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
