import asyncio
import subprocess
from pathlib import Path
from agents import Agent, Runner, SQLiteSession, ShellCallOutcome, ShellCommandOutput, ShellCommandRequest, ShellResult, ShellTool

Raiz   = Path(__file__).resolve().parent
Prompt = Raiz / "AGENTS.md"
Reglas = f"""
Contexto:
- Trabajás en {Raiz}.
"""

def cargar_instrucciones() -> str:
    return f"{Prompt.read_text()}\n\n{Reglas}" if Prompt.exists() else Reglas

def ejecutar(cmd: str) -> ShellCommandOutput:
    proc = subprocess.run(cmd, shell=True, cwd=Raiz, capture_output=True, text=True)
    return ShellCommandOutput(
        command=cmd, stdout=proc.stdout, stderr=proc.stderr,
        outcome=ShellCallOutcome(type="exit", exit_code=proc.returncode),
    )

async def local_shell(request: ShellCommandRequest) -> ShellResult:
    action = request.data.action.commands
    return ShellResult( output=[ejecutar(cmd) for cmd in action] )

agent = Agent(
    name="Nano",
    model="gpt-5.4",
    instructions=cargar_instrucciones(),
    tools=[ShellTool(executor=local_shell)],
)

async def main() -> None:
    session = SQLiteSession("nanoprog")
    while True:
        user_input = input("Tú> ").strip()
        if user_input.lower() in {"salir", "exit", "quit"}:
            break

        result = await Runner.run(agent, user_input, session=session)
        print(f"\nAgente> {result.final_output}\n")

if __name__ == "__main__":
    asyncio.run(main())