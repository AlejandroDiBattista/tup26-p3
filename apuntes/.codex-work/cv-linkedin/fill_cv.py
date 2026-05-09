from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import shutil
from lxml import etree


SRC = Path("CURRICULUM VITAE 1.base.docx")
OUT = Path("CURRICULUM VITAE - Alejandro Di Battista.docx")

NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"


replacements = {
    "mozartamadeus@": "alejandrodibattista@gmail.com",
    "  38178787878": "  381 5343458",
    "Av de las Camelias": "Argentina",
    "JUAN GUTIERREZ": "Alejandro Di Battista",
    "Lic. Tecnología Educativa": "Asesor Tecnológico",
    "Administrador de Empresas con más de 8 años de experiencia en gestión financiera, planificación estratégica y optimización de procesos. Experto en liderazgo de equipos, toma de decisiones basadas en datos y desarrollo de estrategias para mejorar la rentabilidad y eficiencia operativa. Apasionado por la innovación empresarial y la transformación digital.": (
        "Integrando la pasión por emprender y la tecnología. Enfocado completamente en el desarrollo "
        "de productos mobile, con experiencia en asesoramiento tecnológico, desarrollo de software y docencia universitaria."
    ),
    "Gestión financiera y presupuestaria": "C#",
    "Planificación estratégica": "Flutter",
    "Análisis de datos": "Dart",
    "Negociación": "Python",
    "Resolución de conflictos": "JavaScript",
    "Liderazgo y gestión de equipos": "Desarrollo mobile",
    "ANALISTA DE PLANEAMIENTO ESTRATEGICO": "Director / Asesor Tecnológico",
    "2017 2025": "2003 - 2023",
    "Diseño de estrategias de crecimiento empresarial para clientes. ": "UTN / Concejo Deliberante Yerba Buena.",
    "Elaboración de estudios de mercado y análisis de viabilidad financiera para proyectos de inversión.": (
        "Modernización de Hospitales de Tucumán (UTN, 2003-2006). "
        "Concejo Deliberante Yerba Buena (2017-2023). Detalle en página 2."
    ),
    "SOTO Y OCHOA": "Universidades",
    "2025 2024": "1997 - Actualidad",
    "Docente Universitaria – Tecnicatura / Licenciatura UTN FRT ": "Profesor universitario.",
    "Dictado de clases en asignaturas del área Diseño de materiales didácticos y recursos para aula virtual Gestión de cursadas en plataforma (Sysacad)": (
        "UTN, Universidad Santo Tomás de Aquino y Universidad Católica de Santiago del Estero. Ver detalle en página 2."
    ),
    "Gerente de Administración y Finanzas": "Gerente de Sistema",
    "ÁLVAREZ Y ASOCIADOS": "Caja Popular de Ahorros de Tucumán",
    "2026 – Actualidad": "2021 - 2022",
    "Gestión de presupuestos y optimización de recursos financieros. ": "Gestión de sistemas.",
    "Implementación de estrategias para mejorar la rentabilidad.": "San Miguel de Tucumán, Tucumán, Argentina.",
    "Licenciatura en Tecnología Educativa": "Ing. Sistemas (1988-1994) / MBA (1995-1997)",
    "UNIVERSIDAD TECNOLOGICA NACIONAL DE TUCUMAN": "UTN / Fundación del Tucumán",
    "2015  2022": "",
    "2015 2022": "",
    "Ingles": "English - Middle (1100-1500)",
    "Frances": "Español - Nativo",
    "Portugués": "",
    "DNI": "DNI: 18.627.585",
    "Fecha de Nacimiento": "Fecha de nacimiento: 5 de diciembre de 1967",
    "Edad ": "Edad: 58 años",
    "Nacionalidad": "Nacionalidad: argentina",
    "Estado Civil": "Estado civil: divorciado",
    "HABILIDADES": "Habilidades",
    "EDUCACION": "Educación",
    "EXPERIENCIA LABORAL": "Experiencia Laboral",
    "EXPERIENCIA DOCENTE": "Experiencia Docente",
    "IDIOMAS": "Idiomas",
    "CONTACTO": "Contacto",
    "PERFIL": "Perfil",
    "DATOS PERSONALES": "Datos Personales",
}


def para_text(paragraph):
    return "".join(paragraph.xpath(".//w:t/text()", namespaces=NS))


def replace_paragraph_text(paragraph, new_text):
    text_nodes = paragraph.xpath(".//w:t", namespaces=NS)
    if not text_nodes:
        return
    first = text_nodes[0]
    first.text = new_text.split("\n")[0]
    run = first.getparent()
    for line in new_text.split("\n")[1:]:
        br = etree.Element(W + "br")
        extra = etree.Element(W + "t")
        extra.text = line
        run.append(br)
        run.append(extra)
    for node in text_nodes[1:]:
        node.text = ""


def w_el(name, text=None):
    el = etree.Element(W + name)
    if text is not None:
        el.text = text
    return el


def set_attr(el, name, value):
    el.set(W + name, value)


def append_paragraph(body, text="", *, size=22, bold=False, color=None, small_caps=False, page_break=False, before=0, after=120):
    p_el = w_el("p")
    p_pr = w_el("pPr")
    spacing = w_el("spacing")
    set_attr(spacing, "before", str(before))
    set_attr(spacing, "after", str(after))
    p_pr.append(spacing)
    p_el.append(p_pr)

    r_el = w_el("r")
    r_pr = w_el("rPr")
    if bold:
        r_pr.append(w_el("b"))
    if small_caps:
        r_pr.append(w_el("smallCaps"))
    sz = w_el("sz")
    set_attr(sz, "val", str(size))
    r_pr.append(sz)
    if color:
        c = w_el("color")
        set_attr(c, "val", color)
        r_pr.append(c)
    r_el.append(r_pr)
    if page_break:
        br = w_el("br")
        set_attr(br, "type", "page")
        r_el.append(br)
    if text:
        t = w_el("t", text)
        r_el.append(t)
    p_el.append(r_el)
    body.insert(len(body) - 1, p_el)
    return p_el


def append_bullet(body, text):
    append_paragraph(body, "• " + text, size=20, before=0, after=80)


def append_detail_page(root):
    body = root.find(".//" + W + "body")
    append_paragraph(body, page_break=True)
    append_paragraph(body, "Alejandro Di Battista", size=32, bold=True, color="C45512", small_caps=True, after=220)
    append_paragraph(body, "Detalle de Experiencia Laboral", size=28, bold=True, color="666666", small_caps=True, before=120, after=160)
    append_paragraph(body, "Gerente de Sistema", size=24, bold=True, small_caps=True, after=60)
    append_paragraph(body, "Caja Popular de Ahorros de Tucumán | Septiembre 2021 - Septiembre 2022", size=21, bold=True, after=80)
    append_bullet(body, "Gestión de sistemas en San Miguel de Tucumán, Tucumán, Argentina.")
    append_paragraph(body, "", before=80, after=40)
    append_paragraph(body, "Director Proyecto Modernización de Hospitales de Tucumán", size=24, bold=True, small_caps=True, after=60)
    append_paragraph(body, "UTN | Enero 2003 - Diciembre 2006", size=21, bold=True, after=80)
    append_bullet(body, "Dirección integral del proyecto.")
    append_bullet(body, "Elaboración de pliegos de licitación.")
    append_bullet(body, "Supervisión de construcciones.")
    append_bullet(body, "Formación de equipos de desarrollo y gestión.")
    append_bullet(body, "Ejecución del proyecto.")

    append_paragraph(body, "Detalle de Experiencia Docente", size=28, bold=True, color="666666", small_caps=True, before=260, after=160)
    append_paragraph(body, "Universidad Tecnológica Nacional (UTN)", size=24, bold=True, small_caps=True, after=60)
    append_paragraph(body, "Profesor universitario | Marzo 2024 - Actualidad", size=21, after=80)
    append_bullet(body, "Enseñanza de programación en C#, Python y JavaScript.")
    append_paragraph(body, "Universidad Santo Tomás de Aquino", size=24, bold=True, small_caps=True, before=160, after=60)
    append_paragraph(body, "Profesor Introducción a la Comercialización | 1998 - 2010", size=21, after=80)
    append_bullet(body, "Profesor adjunto.")
    append_paragraph(body, "Universidad Santo Tomás de Aquino", size=24, bold=True, small_caps=True, before=160, after=60)
    append_paragraph(body, "Profesor de Informática | 1997 - 2005", size=21, after=80)
    append_bullet(body, "Profesor titular.")
    append_paragraph(body, "Universidad Católica de Santiago del Estero", size=24, bold=True, small_caps=True, before=160, after=60)
    append_paragraph(body, "Profesor Análisis de Desempeño de Sistema de Procesamiento de Datos | 1999 - 2006", size=21, after=80)
    append_bullet(body, "Profesor titular.")


def apply_small_caps(root):
    small_caps_texts = {
        "Alejandro Di Battista",
        "Datos Personales",
        "Contacto",
        "Perfil",
        "Habilidades",
        "Educación",
        "Experiencia Laboral",
        "Experiencia Docente",
        "Idiomas",
        "Director / Asesor Tecnológico",
        "Gerente de Sistema",
        "Universidades",
    }
    for paragraph in root.xpath(".//w:p", namespaces=NS):
        if para_text(paragraph).strip() in small_caps_texts:
            for run in paragraph.xpath(".//w:r", namespaces=NS):
                r_pr = run.find(W + "rPr")
                if r_pr is None:
                    r_pr = w_el("rPr")
                    run.insert(0, r_pr)
                if r_pr.find(W + "smallCaps") is None:
                    r_pr.append(w_el("smallCaps"))


tmp = Path("docx-unpacked")
if tmp.exists():
    shutil.rmtree(tmp)
tmp.mkdir()

with ZipFile(SRC) as zin:
    zin.extractall(tmp)

document = tmp / "word" / "document.xml"
parser = etree.XMLParser(remove_blank_text=False)
root = etree.parse(str(document), parser)

for paragraph in list(root.xpath(".//w:p", namespaces=NS)):
    current = para_text(paragraph)
    if current in {"Portugués", "2015 2022", "2015  2022"}:
        parent = paragraph.getparent()
        if parent is not None:
            parent.remove(paragraph)
        continue
    if current in replacements:
        replace_paragraph_text(paragraph, replacements[current])

apply_small_caps(root)
append_detail_page(root)

root.write(str(document), xml_declaration=True, encoding="UTF-8", standalone=True)

with ZipFile(OUT, "w", ZIP_DEFLATED) as zout:
    for path in sorted(tmp.rglob("*")):
        if path.is_file():
            zout.write(path, path.relative_to(tmp))

shutil.rmtree(tmp)
print(OUT.resolve())
