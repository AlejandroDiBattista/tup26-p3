from __future__ import annotations

import json
import re
from pathlib import Path

AGENDA    = Path("/Users/adibattista/Documents/GitHub/tup25-p4/interno/directorio-alumno/deploy/alumnos.json")
DESTINO   = Path("/Users/adibattista/Documents/GitHub/tup25-p4/interno/directorio-alumno/deploy/img")
ORIGEN    = Path("/Users/adibattista/Documents/GitHub/tup26-p3/alumnos/alumnos.md")
PRACTICOS = Path("/Users/adibattista/Documents/GitHub/tup26-p3/practicos")

AÑO     = 2026
MATERIA = "Programación 3"
CODIGO  = "P3"

ESTADOS = {
	"🟢": "Promocionado",
	"🟡": "Pendiente",
	"🟠": "Regular",
	"🔴": "Libre",
	"⚪": "Pendiente",
}


def limpiar_campo(valor: str) -> str:
	texto = valor.strip()
	if texto in {"-", "—"}:
		return ""
	return texto


def normalizar_github(valor: str) -> str:
	github = limpiar_campo(valor)
	if not github:
		return ""
	return github if github.startswith("@") else f"@{github}"


def parsear_estado(practicos: str) -> str:
	valor = practicos.strip()
	if not valor:
		return "Pendiente"

	for emoji, estado in ESTADOS.items():
		if valor.startswith(emoji):
			return estado

	return "Pendiente"


def leer_alumnos_desde_markdown(ruta: Path) -> list[dict]:
	alumnos = []
	comision = ""
	en_bloque = False

	for raw in ruta.read_text(encoding="utf-8").splitlines():
		linea = raw.rstrip()

		if linea.startswith("## "):
			comision = linea[3:].strip()
			en_bloque = False
			continue

		if linea.startswith("```"):
			en_bloque = not en_bloque
			continue

		if not en_bloque or not comision:
			continue

		if not linea.strip() or linea.startswith("Legajo") or linea.startswith("------"):
			continue

		columnas = re.split(r"\s{2,}", linea.strip())
		if len(columnas) < 7:
			continue

		legajo = limpiar_campo(columnas[0])
		if not legajo.isdigit():
			continue

		nombre = limpiar_campo(columnas[1])
		telefono = limpiar_campo(columnas[2])
		foto = limpiar_campo(columnas[3]).lower() == "si"
		github = normalizar_github(columnas[4])
		practicos = columnas[5].strip()
		carpeta_tp = f"{legajo} - {nombre}"

		alumnos.append({
			"legajo": legajo,
			"nombre": nombre,
			"telefono": telefono,
			"github": github,
			"foto": foto,
			"comision": comision,
			"estado": "cursando",
			"carpeta_tp": carpeta_tp,
		})

	return alumnos


def cargar_agenda(ruta: Path) -> dict:
	if not ruta.exists():
		return {}
	return json.loads(ruta.read_text(encoding="utf-8"))


def ordenar_cursos(cursos: list[dict]) -> list[dict]:
	return sorted(
		cursos,
		key=lambda curso: (
			curso.get("año", 0),
			curso.get("codigo", ""),
			curso.get("materia", ""),
			curso.get("comision", ""),
		),
	)


def obtener_foto_real(alumno: dict) -> bool:
	carpeta = PRACTICOS / alumno["carpeta_tp"]
	for extension in ("png", "jpg", "jpeg", "webp"):
		if (carpeta / f"foto.{extension}").exists():
			return True
	return alumno["foto"]


def fusionar_agenda(agenda: dict, alumnos: list[dict]) -> dict:
	resultado = dict(agenda)

	for alumno in alumnos:
		legajo = alumno["legajo"]
		actual = dict(resultado.get(legajo, {}))

		curso_actual = {
			"año": AÑO,
			"materia": MATERIA,
			"comision": alumno["comision"],
			"estado": alumno["estado"],
			"codigo": CODIGO,
			"carpeta_tp": alumno["carpeta_tp"],
		}

		cursos = []
		reemplazado = False
		for curso in actual.get("cursos", []):
			if curso.get("codigo") == CODIGO and curso.get("año") == AÑO:
				cursos.append(curso_actual)
				reemplazado = True
			else:
				cursos.append(curso)

		if not reemplazado:
			cursos.append(curso_actual)

		actual["legajo"] = legajo
		actual["nombre"] = alumno["nombre"]
		actual["cursos"] = ordenar_cursos(cursos)
		actual["foto"] = obtener_foto_real(alumno) or bool(actual.get("foto"))

		if alumno["telefono"]:
			actual["telefono"] = alumno["telefono"]

		if alumno["github"]:
			actual["github"] = alumno["github"]

		resultado[legajo] = actual

	return dict(sorted(resultado.items(), key=lambda item: int(item[0])))


def guardar_agenda(ruta: Path, agenda: dict) -> None:
	ruta.parent.mkdir(parents=True, exist_ok=True)
	ruta.write_text(json.dumps(agenda, ensure_ascii=False, indent=4) + "\n", encoding="utf-8")


def main() -> None:
	alumnos = leer_alumnos_desde_markdown(ORIGEN)
	print(f"Alumnos leídos desde Markdown: {len(alumnos)}")
	for alumno in alumnos[0:5]:
		print(f"  - {alumno['legajo']}: {alumno['nombre']} ({alumno['comision']})")
	agenda = cargar_agenda(AGENDA)
	print(f"Agenda cargada: {len(agenda)} alumnos")
	agenda_actualizada = fusionar_agenda(agenda, alumnos)
	# guardar_agenda(AGENDA, agenda_actualizada)

	# print(f"Alumnos leídos: {len(alumnos)}")
	# print(f"Agenda actualizada: {AGENDA}")
	# print(f"Directorio de imágenes configurado: {DESTINO}")


if __name__ == "__main__":
	main()

