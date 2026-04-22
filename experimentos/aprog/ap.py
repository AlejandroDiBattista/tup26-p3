import asyncio
import subprocess
from pathlib import Path

from openai.types.responses.response_text_delta_event import ResponseTextDeltaEvent

WORKSPACE = Path(__file__).resolve().parent
ENV_FILE = WORKSPACE / ".env"

from agents import ( Agent, AgentUpdatedStreamEvent, RawResponsesStreamEvent, RunItemStreamEvent, Runner, ShellTool, function_tool, )


def safe_path(path: str) -> Path:
    """
    Resuelve una ruta dentro del workspace y evita escapes tipo ../../
    """
    p = (WORKSPACE / path).resolve()
    try:
        p.relative_to(WORKSPACE)
    except ValueError:
        raise ValueError("Ruta fuera del workspace.")
    return p


@function_tool
def read_file(path: str) -> str:
    """
    Lee un archivo de texto dentro del workspace.

    Args:
        path: Ruta relativa al workspace.
    """
    file_path = safe_path(path)

    if not file_path.exists():
        return f"ERROR: no existe el archivo {path}"

    if not file_path.is_file():
        return f"ERROR: {path} no es un archivo"

    return file_path.read_text(encoding="utf-8")


@function_tool
def write_file(path: str, content: str) -> str:
    """
    Escribe un archivo de texto dentro del workspace.

    Args:
        path: Ruta relativa al workspace.
        content: Contenido completo a guardar.
    """
    file_path = safe_path(path)
    file_path.parent.mkdir(parents=True, exist_ok=True)
    file_path.write_text(content, encoding="utf-8")
    return f"OK: archivo escrito en {path}"


def run_shell(request) -> str:
    """
    Executor para ShellTool.

    El SDK actual entrega request.data.action.commands.
    Conservamos compatibilidad con request.data.commands.
    """
    outputs = []

    data = request.data
    action = getattr(data, "action", None)

    commands = getattr(action, "commands", None)
    if commands is None:
        commands = getattr(data, "commands", [])

    timeout_ms = getattr(action, "timeout_ms", None)
    if timeout_ms is None:
        timeout_ms = getattr(data, "timeout_ms", None)
    timeout_ms = timeout_ms or 30_000

    for cmd in commands:
        print(f"\n[shell] ejecutando: {cmd}", flush=True)
        try:
            result = subprocess.run( cmd, shell=True, cwd=WORKSPACE, capture_output=True, text=True, timeout=timeout_ms / 1000, )

            outputs.append(
                "\n".join(
                    [
                        f"$ {cmd}",
                        f"exit_code: {result.returncode}",
                        "--- STDOUT ---",
                        result.stdout.strip() or "(vacío)",
                        "--- STDERR ---",
                        result.stderr.strip() or "(vacío)",
                    ]
                )
            )
        except subprocess.TimeoutExpired:
            outputs.append( "\n".join( [ f"$ {cmd}", "ERROR: timeout", ] ) )
        except Exception as e:
            outputs.append( "\n".join( [ f"$ {cmd}", f"ERROR: {e}", ] ) )

    return "\n\n".join(outputs)


def tool_label(item) -> str:
    if getattr(item, "title", None):
        return item.title

    raw_item = getattr(item, "raw_item", None)

    name = getattr(raw_item, "name", None)
    if isinstance(name, str) and name:
        return name

    if isinstance(raw_item, dict):
        for key in ("name", "tool_name", "title", "type"):
            value = raw_item.get(key)
            if isinstance(value, str) and value:
                return value

    raw_type = getattr(raw_item, "type", None)
    if isinstance(raw_type, str) and raw_type:
        return raw_type

    return "herramienta"


async def run_turn(run_input):
    result = Runner.run_streamed(agent, run_input)
    started_text = False

    async for event in result.stream_events():
        if isinstance(event, RawResponsesStreamEvent):
            if isinstance(event.data, ResponseTextDeltaEvent):
                if not started_text:
                    print("\nAgente> ", end="", flush=True)
                    started_text = True
                print(event.data.delta, end="", flush=True)
        elif isinstance(event, RunItemStreamEvent):
            if event.name == "tool_called":
                print(f"[tool] ejecutando: {tool_label(event.item)}", flush=True)
            elif event.name == "tool_output":
                print(f"[tool] finalizo: {tool_label(event.item)}", flush=True)
        elif isinstance(event, AgentUpdatedStreamEvent):
            print(f"\n[agent] ahora responde: {event.new_agent.name}", flush=True)

    if started_text:
        print()
    elif result.final_output is not None:
        print(f"\nAgente> {result.final_output}")

    return result


agent = Agent(
    name="Programmer",
    model="gpt-5.4",
    instructions=f"""
Sos un agente de programación que trabaja dentro del directorio:

{WORKSPACE}

Reglas:
- Antes de modificar código existente, leé los archivos relevantes.
- Para crear o cambiar archivos, usá read_file y write_file.
- Para inspeccionar o ejecutar el proyecto, usá shell.
- No inventes resultados de comandos.
- Mantené las respuestas cortas y concretas.
- Cuando hagas cambios, explicá qué cambiaste en pocas líneas.
""".strip(),
    tools=[
        ShellTool(executor=run_shell),
        read_file,
        write_file,
    ],
)


def main():
    print("Agente listo.")
    print(f"Workspace: {WORKSPACE}")
    print("Escribí 'salir' para terminar.\n")

    history = []

    while True:
        user_input = input("Tú> ").strip()
        if not user_input:
            continue
        if user_input.lower() in {"salir", "exit", "quit"}:
            break

        try:
            run_input = history + [{"role": "user", "content": user_input}]
            result = asyncio.run(run_turn(run_input))
            print()

            history = result.to_input_list()

        except KeyboardInterrupt:
            print("\nInterrumpido.\n")
        except Exception as e:
            print(f"\nERROR: {e}\n")


if __name__ == "__main__":
    main()
