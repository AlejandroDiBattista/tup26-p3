@function_tool
def exec(command: str) -> str:
    """Execute a shell command on the host machine."""
    result = subprocess.run(command, shell=True, capture_output=True, text=True)
    return result.stdout + result.stderr

agent = Agent(
    name="Coding Agent",
    instructions="You are a helpful coding assistant.",
    tools=[exec],
)

result = Runner.run_sync(agent, "Use npm to install dependencies")
print(result.final_output)