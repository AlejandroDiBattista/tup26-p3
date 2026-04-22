import subprocess
from pathlib import Path
from agents import Agent, Runner, ShellTool, function_tool

Workspace = Path(__file__).resolve().parent
Prompt = Workspace / "nanop_prompt.txt"
IntruccionesBase = f"""
Reglas operativas:
- Trabajás en {Workspace}.
- Antes de modificar, leé archivos relevantes.
- Usá read_file y write_file para archivos.
- Usá shell para comandos.
- No inventes resultados.
- Respondé corto.
""".strip()


def cargar_instrucciones() -> str:
    if not Prompt.exists():
        raise FileNotFoundError(f"No existe el prompt: {Prompt}")
    template = Prompt.read_text(encoding="utf-8")
    prompt_from_file = template.format(workspace=Workspace).strip()
    return f"{prompt_from_file}\n\n{IntruccionesBase}"


@function_tool
def read_file(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


@function_tool
def write_file(path: str, content: str) -> str:
    p = Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(content, encoding="utf-8")
    return f"OK: {p}"


def ejecutar(cmd):
    r = subprocess.run(cmd, shell=True, cwd=Workspace, capture_output=True, text=True)
    stdout = r.stdout.strip() or "(vacío)"
    stderr = r.stderr.strip() or "(vacío)"
    return f"$ {cmd}\nexit_code: {r.returncode}\n--- STDOUT ---\n{stdout}\n--- STDERR ---\n{stderr}"


def run_shell(request) -> str:
    data = request.data
    commands = getattr(getattr(data, "action", None), "commands", None) or getattr(data, "commands", [])
    return "\n\n".join(ejecutar(cmd) for cmd in commands)


agent = Agent(
    name="NanoProg",
    model="gpt-5.4-mini",
    instructions=cargar_instrucciones(),
    tools=[ShellTool(executor=run_shell), read_file, write_file],
)


def main():
    history = []
    while True:
        user_input = input("Tú> ").strip()
        if not user_input:
            continue
        if user_input.lower() in {"salir", "exit", "quit"}:
            break

        result = Runner.run_sync(agent, history + [{"role": "user", "content": user_input}])
        print(f"\nAgente> {result.final_output}\n")
        history = result.to_input_list()


if __name__ == "__main__":
    main()
