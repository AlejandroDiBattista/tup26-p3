#!/usr/bin/env python3

from __future__ import annotations

import csv
import os
import re
import subprocess
import sys
import tempfile
import unicodedata
from dataclasses import dataclass
from pathlib import Path
from typing import TypedDict


REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_PRACTICOS = REPO_ROOT / "practicos"
RESULTS_CSV = REPO_ROOT / "resultados-p1.csv"
RESULTS_MD = REPO_ROOT / "resultado-p1.md"
MIN_PRESENTED_LINES = 20
BUILD_TIMEOUT = 20
RUN_TIMEOUT = 5

EMPLEADOS_CSV = """nombre,apellido,edad,salario,departamento
Carlos,García,35,85000,Ingeniería
Ana,Martínez,28,72000,Diseño
Luis,Rodríguez,42,120000,Gerencia
María,López,31,88000,Ingeniería
Pedro,Sánchez,25,65000,Diseño
Laura,González,38,95000,Gerencia
"""

ORDENADO_POR_APELLIDO = """nombre,apellido,edad,salario,departamento
Carlos,García,35,85000,Ingeniería
Laura,González,38,95000,Gerencia
María,López,31,88000,Ingeniería
Ana,Martínez,28,72000,Diseño
Luis,Rodríguez,42,120000,Gerencia
Pedro,Sánchez,25,65000,Diseño
"""

ORDENADO_POR_SALARIO_DESC = """nombre,apellido,edad,salario,departamento
Luis,Rodríguez,42,120000,Gerencia
Laura,González,38,95000,Gerencia
María,López,31,88000,Ingeniería
Carlos,García,35,85000,Ingeniería
Ana,Martínez,28,72000,Diseño
Pedro,Sánchez,25,65000,Diseño
"""

ORDENADO_POR_DEPTO_Y_SALARIO = """nombre,apellido,edad,salario,departamento
Ana,Martínez,28,72000,Diseño
Pedro,Sánchez,25,65000,Diseño
Luis,Rodríguez,42,120000,Gerencia
Laura,González,38,95000,Gerencia
María,López,31,88000,Ingeniería
Carlos,García,35,85000,Ingeniería
"""


@dataclass(frozen=True)
class TestCase:
    name: str
    args: tuple[str, ...]
    expected_exit: int | None = 0
    stdin_text: str | None = None
    input_file_name: str | None = "empleados.csv"
    expected_stdout: str | None = None
    output_file_name: str | None = None
    expected_output_file: str | None = None
    require_stdout_nonempty: bool = False
    require_stderr_nonempty: bool = False
    optional: bool = False


@dataclass(frozen=True)
class Evaluation:
    legajo: int
    resultado: str
    detalle: str = ""


class InformeFila(TypedDict):
    legajo: int
    nombre: str
    estado: str
    pruebas: list[str]


def build_test_cases() -> list[TestCase]:
    return [
        TestCase(
            name="help",
            args=("--help",),
            expected_exit=0,
            input_file_name=None,
            require_stdout_nonempty=True,
            optional=True,
        ),
        TestCase(
            name="apellido",
            args=("empleados.csv", "-b", "apellido"),
            expected_exit=0,
            expected_stdout=ORDENADO_POR_APELLIDO,
        ),
        TestCase(
            name="salario-desc",
            args=("empleados.csv", "-b", "salario:num:desc"),
            expected_exit=0,
            expected_stdout=ORDENADO_POR_SALARIO_DESC,
        ),
        TestCase(
            name="departamento-salario",
            args=("empleados.csv", "-b", "departamento", "-b", "salario:num:desc"),
            expected_exit=0,
            expected_stdout=ORDENADO_POR_DEPTO_Y_SALARIO,
        ),
        TestCase(
            name="salida-archivo",
            args=("empleados.csv", "-b", "apellido:alpha:asc", "-o", "salida.csv"),
            expected_exit=0,
            output_file_name="salida.csv",
            expected_output_file=ORDENADO_POR_APELLIDO,
        ),
        TestCase(
            name="stdin",
            args=("-b", "apellido"),
            expected_exit=0,
            stdin_text=EMPLEADOS_CSV,
            input_file_name=None,
            expected_stdout=ORDENADO_POR_APELLIDO,
        ),
    ]


def listar_legajos(practicos_dir: Path = DEFAULT_PRACTICOS) -> list[int]:
    if not practicos_dir.is_dir():
        return []

    legajos: set[int] = set()

    for child in practicos_dir.iterdir():
        if not child.is_dir():
            continue

        coincidencia = re.match(r"^(\d+)", child.name)
        if coincidencia:
            legajos.add(int(coincidencia.group(1)))

    return sorted(legajos)


def normalize_text(text: str) -> str:
    normalized = unicodedata.normalize("NFC", text)
    normalized = normalized.replace("\r\n", "\n").replace("\r", "\n")
    return normalized.rstrip("\n")


def count_lines(path: Path) -> int:
    with path.open("r", encoding="utf-8", errors="replace") as handle:
        return sum(1 for _ in handle)


def short_output(text: str, limit: int = 12) -> str:
    lines = normalize_text(text).splitlines()
    if not lines:
        return "(sin salida)"
    if len(lines) <= limit:
        return " | ".join(lines)
    head = " | ".join(lines[:limit])
    return f"{head} | ..."


def format_failed_tests(failed_tests: list[tuple[int, str]]) -> str:
    return "falla " + " ".join(f"{index}" for index, name in failed_tests)


def render_progress(current: int, total: int, label: str = "", width: int = 24) -> None:
    if total <= 0:
        return

    current = max(0, min(current, total))
    filled = int(width * current / total)
    bar = "█" * filled + "░" * (width - filled)
    suffix = f" {label}" if label else ""
    sys.stderr.write(f"\r[{bar}] {current}/{total}{suffix}")
    sys.stderr.flush()


def run_command( cmd: list[str], *, cwd: Path, timeout: int, stdin_text: str | None = None, ) -> subprocess.CompletedProcess[str]:
    env = os.environ.copy()
    env.setdefault("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
    env.setdefault("DOTNET_NOLOGO", "1")
    env.setdefault("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1")

    return subprocess.run(
        cmd,
        cwd=cwd,
        input=stdin_text,
        text=True,
        encoding="utf-8",
        errors="replace",
        capture_output=True,
        timeout=timeout,
        env=env,
        check=False,
    )


def es_error_compilacion(completed: subprocess.CompletedProcess[str]) -> bool:
    salida = f"{completed.stdout}\n{completed.stderr}"
    return (
        "Build FAILED" in salida
        or "The build failed" in salida
        or bool(re.search(r"\berror (CS|NETSDK|MSB)\d+\b", salida, re.IGNORECASE))
    )


def prepare_case_dir(case_dir: Path, case: TestCase) -> None:
    case_dir.mkdir(parents=True, exist_ok=True)

    if case.input_file_name:
        (case_dir / case.input_file_name).write_text(EMPLEADOS_CSV, encoding="utf-8")

    if case.output_file_name:
        output_file = case_dir / case.output_file_name
        if output_file.exists():
            output_file.unlink()


def evaluate_case(source_file: Path, case: TestCase, case_dir: Path, timeout: int) -> tuple[bool, str, bool]:
    prepare_case_dir(case_dir, case)

    command = ["dotnet", "run", str(source_file), "--", *case.args]

    try:
        completed = run_command(
            command,
            cwd=case_dir,
            timeout=timeout,
            stdin_text=case.stdin_text,
        )
    except subprocess.TimeoutExpired:
        return False, f"{case.name}: timeout", False

    if es_error_compilacion(completed):
        details = completed.stderr.strip() or completed.stdout.strip() or "compilacion fallida"
        return False, short_output(details), True

    if case.expected_exit is None:
        if completed.returncode == 0:
            return False, f"{case.name}: esperaba codigo de salida distinto de 0", False
    elif completed.returncode != case.expected_exit:
        return False, (
            f"{case.name}: codigo de salida {completed.returncode}, "
            f"esperado {case.expected_exit}"
        ), False

    if case.require_stdout_nonempty and not completed.stdout.strip():
        return False, f"{case.name}: salida estandar vacia", False

    if case.require_stderr_nonempty and not completed.stderr.strip():
        return False, f"{case.name}: salida de error vacia", False

    if case.expected_stdout is not None:
        actual_stdout = normalize_text(completed.stdout)
        expected_stdout = normalize_text(case.expected_stdout)
        if actual_stdout != expected_stdout:
            return False, (
                f"{case.name}: stdout inesperado. "
                f"recibido={short_output(completed.stdout)}"
            ), False

    if case.output_file_name and case.expected_output_file is not None:
        output_file = case_dir / case.output_file_name
        if not output_file.is_file():
            return False, f"{case.name}: no genero {case.output_file_name}", False
        actual_output = normalize_text(output_file.read_text(encoding="utf-8", errors="replace"))
        expected_output = normalize_text(case.expected_output_file)
        if actual_output != expected_output:
            return False, (
                f"{case.name}: {case.output_file_name} inesperado. "
                f"recibido={short_output(actual_output)}"
            ), False

    return True, "", False

def ubicar_archivo(legajo: int, practicos_dir: Path) -> Path | None:
    patron = f"{legajo}*/TP1/sortx.cs"
    for child in practicos_dir.glob(patron):
        if child.is_file():
            return child

    return None

def evaluate_submission(legajo: int, practicos_dir: Path, temp_root: Path, timeout: int, test_cases: list[TestCase]) -> Evaluation:
    source_file = ubicar_archivo(legajo, practicos_dir)
    if not source_file:
        return Evaluation(legajo, "no-presentado")

    line_count = count_lines(source_file)
    if line_count < MIN_PRESENTED_LINES:
        return Evaluation(legajo, "no-presentado", f"archivo con {line_count} lineas")

    warnings: list[str] = []
    failed_tests: list[tuple[int, str]] = []
    failed_details: list[str] = []

    for index, case in enumerate(test_cases, start=1):
        case_dir = temp_root / f"case-{legajo}-{index:02d}-{slugify(case.name)}"
        ok, details, es_compilacion = evaluate_case(source_file, case, case_dir, timeout)
        if not ok:
            if es_compilacion:
                return Evaluation(legajo, "no-compila", details)
            if case.optional:
                warnings.append(details)
                continue
            failed_tests.append((index, case.name))
            if details:
                failed_details.append(f"{index}: {details}")
            continue

    if failed_tests:
        resultado = format_failed_tests(failed_tests)
        return Evaluation(legajo, resultado, " | ".join(failed_details))

    return Evaluation(legajo, "funciona", " | ".join(warnings))


def slugify(value: str) -> str:
    value = value.lower()
    value = re.sub(r"[^a-z0-9]+", "-", value)
    return value.strip("-") or "caso"


def emit_results(results: list[Evaluation]) -> None:
    write_results_csv(results, RESULTS_CSV)

    if RESULTS_CSV.is_file():
        print(f"✅ Resultados guardados en: {RESULTS_CSV}", file=sys.stderr)


def write_results_csv(results: list[Evaluation], output_path: Path) -> None:
    alumnos = leer_alumnos()
    with output_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle, lineterminator="\n")
        writer.writerow(["legajo", "nombre", "comision", "resultado"])

        for item in results:
            alumno = alumnos.get(int(item.legajo), {"nombre": "Desconocido", "comision": "X"})
            writer.writerow([item.legajo, alumno["nombre"], alumno["comision"], item.resultado])


def leer_alumnos() -> dict[int, dict[str, str]]:
    lineas = (REPO_ROOT / "alumnos" / "alumnos.md").read_text(encoding="utf-8").splitlines()
    salida = {}
    comision = None
    for linea in lineas:
        if linea.startswith("## C"):
            comision = linea.removeprefix("## ").strip()
            continue
        if not re.match(r"^\d{5}\s{2,}", linea):
            continue

        columnas = [columna.strip() for columna in re.split(r"\s{2,}", linea.strip())]
        columnas = (columnas + [""] * 7)[:7]
        legajo, nombre, telefono, foto, github, practicos, pruebas = columnas
        salida[int(legajo)] = {
            "nombre": nombre,
            "comision": comision or "",
            "telefono": telefono,
            "foto": foto,
            "github": github,
            "practicos": practicos,
            "pruebas": pruebas,
        }
    return salida


def main() -> int:
    legajos = listar_legajos()

    if not legajos:
        print("No se encontraron legajos para evaluar.", file=sys.stderr)
        return 1

    test_cases = build_test_cases()

    with tempfile.TemporaryDirectory(prefix="tp1-sortx-") as tmp:
        temp_root = Path(tmp)
        results: list[Evaluation] = []
        total = len(legajos)
        render_progress(0, total, "iniciando")

        for index, legajo in enumerate(legajos, start=1):
            render_progress(index - 1, total, f"legajo {legajo}")
            resultado = evaluate_submission(
                    legajo=legajo,
                    practicos_dir=DEFAULT_PRACTICOS,
                    temp_root=temp_root,
                    timeout=BUILD_TIMEOUT + RUN_TIMEOUT,
                    test_cases=test_cases,
                )
            results.append(resultado)
            
            render_progress(index, total, f"legajo {legajo}")

        if total > 0:
            sys.stderr.write("\n")

    emit_results(results)

    return 0


def _orden_estado(estado: str) -> int:
    return {"🟢": 1, "🔵": 2, "🟡": 3, "🔴": 4}.get(estado, 0)


def _orden_comision(comision: str) -> tuple[int, str]:
    coincidencia = re.match(r"^C(\d+)$", comision.strip())
    if coincidencia:
        return int(coincidencia.group(1)), comision
    return 999, comision


def _estado_y_pruebas(resultado: str) -> tuple[str, list[str]]:
    estado = resultado.split()[0]

    if estado == "funciona":
        return "🟢", ["✅"] * 6
    if estado == "no-compila":
        return "🟡", ["-"] * 6
    if estado == "no-presentado":
        return "🔴", ["-"] * 6

    fallas = {int(valor) for valor in resultado.split()[1:] if valor.isdigit()}
    pruebas = ["❌" if indice in fallas else "✅" for indice in range(1, 7)]
    return "🔵", pruebas


def _imprimir_bloque_estado(estado: str, titulo: str, filas: list[InformeFila], lineas: list[str]) -> None:
    if not filas:
        return

    lineas.append(f"|  {estado}  | **{titulo}** |")
    for fila in filas:
        lineas.append(
            f"|{fila['legajo']} | {fila['nombre']:<30} | {fila['estado']} | "
            f"{' | '.join(fila['pruebas'])} |"
        )


def generar_informe(path: Path = RESULTS_CSV, output_path: Path = RESULTS_MD) -> None:
    lineas: list[str] = ["# Resultados TP1: Sortx", ""]

    por_comision: dict[str, list[InformeFila]] = {}

    with path.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.reader(handle)
        next(reader, None)

        for legajo, nombre, comision, resultado in reader:
            estado, pruebas = _estado_y_pruebas(resultado)
            por_comision.setdefault(comision or "X", []).append(
                {
                    "legajo": int(legajo),
                    "nombre": nombre.strip('"'),
                    "estado": estado,
                    "pruebas": pruebas,
                }
            )

    for comision in sorted(por_comision, key=_orden_comision):
        filas = por_comision[comision]
        filas.sort(key=lambda fila: (_orden_estado(fila["estado"]), fila["nombre"]))

        lineas.append(f"## {comision}")
        lineas.append("")
        lineas.append("| Legajo | Nombre                       | R  | T1 | T2 | T3 | T4 | T5 |")
        lineas.append("|------|--------------------------------|----|----|----|----|----|----|")

        for estado, titulo in [
            ("🟢", "FUNCIONA"),
            ("🔵", "CON ERRORES"),
            ("🟡", "NO COMPILA"),
            ("🔴", "NO PRESENTO"),
        ]:
            bloque = [fila for fila in filas if fila["estado"] == estado]
            if not bloque:
                continue
            _imprimir_bloque_estado(estado, titulo, bloque, lineas)

        lineas.append("")

    texto = """

### Comandos para las pruebas:

1. Help
>   dotnet run sortx.cs -- --help

2. Ordenar por apellido
>   dotnet run sortx.cs -- empleados.csv -b apellido

3. Ordenar por salario descendente
>   dotnet run sortx.cs -- empleados.csv -b salario:num:desc

4. Ordenar por departamento y salario descendente
>   dotnet run sortx.cs -- empleados.csv -b departamento -b salario:num:desc

5. Salida a archivo
>   dotnet run sortx.cs -- empleados.csv -b apellido:alpha:asc -o salida.csv

### Archivo de entrada (empleados.csv):
```
nombre,apellido,edad,salario,departamento
Carlos,García,35,85000,Ingeniería
Ana,Martínez,28,72000,Diseño
Luis,Rodríguez,42,120000,Gerencia
María,López,31,88000,Ingeniería
Pedro,Sánchez,25,65000,Diseño
Laura,González,38,95000,Gerencia
```
"""
    
    texto = "\n".join(lineas).rstrip() + "\n" +texto 
    output_path.write_text(texto, encoding="utf-8")
    print(f"✅ Informe guardado en: {RESULTS_MD}", file=sys.stderr)


if __name__ == "__main__":
    # if main() == 0:
    generar_informe()
    
# Comandos ejecutados por cada entrega:
#
# Pruebas:
# 1. Help
#    dotnet run sortx.cs -- --help
#
# 2. Ordenar por apellido
#    dotnet run sortx.cs -- empleados.csv --by apellido
#
# 3. Ordenar por salario descendente
#    dotnet run sortx.cs -- empleados.csv --by salario:num:desc
#
# 4. Ordenar por departamento y salario descendente
#    dotnet run sortx.cs -- empleados.csv --by departamento --by salario:num:desc
#
# 5. Salida a archivo
#    dotnet run sortx.cs -- empleados.csv --by apellido:alpha:asc --output salida.csv
#
# 6. Entrada por stdin
#    dotnet run sortx.cs -- --by apellido
#    stdin: contenido de EMPLEADOS_CSV
#
        
# empleados.css
# nombre,apellido,edad,salario,departamento
# Carlos,García,35,85000,Ingeniería
# Ana,Martínez,28,72000,Diseño
# Luis,Rodríguez,42,120000,Gerencia
# María,López,31,88000,Ingeniería
# Pedro,Sánchez,25,65000,Diseño
# Laura,González,38,95000,Gerencia