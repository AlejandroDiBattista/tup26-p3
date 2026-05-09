#!/usr/bin/env python3

from __future__ import annotations

import datetime as dt
import fnmatch
import html
import subprocess
import re
import sys
import unicodedata
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape


WORKDIR = Path.cwd().resolve()
OUTPUT = WORKDIR / "Apuntes-Tup26-P3.epub"
BOOK_ID         = "Apuntes-TUP26-P3"
BOOK_TITLE      = "Apuntes de Programación III"
BOOK_LANGUAGE   = "es"
BOOK_SUBTITLE   = "C#, .NET y herramientas de desarrollo"
BOOK_AUTHOR     = "Ing. Alejandro Di Battista"
BOOK_COVER      = WORKDIR / "portada.jpg"
EXCLUDED        = ["00.*.md", "05.*.md", "09.*.md", "README.md", "CONTRIBUTING.md", "LICENSE.md", "examen.md"]


def is_excluded(path: Path) -> bool:
    return any(fnmatch.fnmatch(path.name, pattern) for pattern in EXCLUDED)


def slugify(text: str) -> str:
    normalized = unicodedata.normalize("NFKD", text)
    ascii_only = normalized.encode("ascii", "ignore").decode("ascii")
    slug = re.sub(r"[^a-zA-Z0-9]+", "-", ascii_only).strip("-").lower()
    return slug or "section"


def first_heading(markdown_text: str, fallback: str) -> str:
    for line in markdown_text.splitlines():
        stripped = line.strip()
        if stripped.startswith("# "):
            return normalize_heading_text(stripped[2:])
    return fallback


def normalize_heading_text(text: str) -> str:
    return re.sub(r"\s+#+\s*$", "", text.strip()).strip()


def inline_markdown(text: str) -> str:
    code_spans: list[str] = []

    def stash_code(match: re.Match[str]) -> str:
        code_spans.append(f"<code>{html.escape(match.group(1), quote=False)}</code>")
        return f"@@CODE{len(code_spans) - 1}@@"

    text = re.sub(r"`([^`]+)`", stash_code, text)
    text = html.escape(text, quote=False)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"\*([^*]+)\*", r"<em>\1</em>", text)
    text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", r'<a href="\2">\1</a>', text)
    for index, code_html in enumerate(code_spans):
        text = text.replace(f"@@CODE{index}@@", code_html)
    return text


def strip_leading_title(markdown_text: str, chapter_title: str) -> str:
    lines = markdown_text.splitlines()
    for index, line in enumerate(lines):
        stripped = line.strip()
        if not stripped:
            continue
        if stripped == f"# {chapter_title}":
            remainder = lines[index + 1 :]
            while remainder and not remainder[0].strip():
                remainder = remainder[1:]
            return "\n".join(remainder)
        break
    return markdown_text


def _render_plain_code(code: str, language_class: str = "") -> str:
    class_attr = f' class="{language_class}"' if language_class else ""
    return f"<pre><code{class_attr}>{html.escape(code)}</code></pre>"


def code_language_label(language: str) -> str:
        lang = language.strip().lower()
        aliases = {
                "": "texto",
                "txt": "texto",
                "text": "texto",
                "plaintext": "texto",
                "cs": "csharp",
                "shell": "bash",
                "sh": "bash",
                "zsh": "bash",
        }
        return aliases.get(lang, lang)


def wrap_code_block(content: str, language: str) -> str:
        label = html.escape(code_language_label(language), quote=False)
        return f"""
<div class="code-block">
    <div class="code-block-header">
        <span class="code-block-language">{label}</span>
    </div>
    {content}
</div>
""".strip()


def split_table_row(line: str) -> list[str]:
    stripped = line.strip()
    if stripped.startswith("|"):
        stripped = stripped[1:]
    if stripped.endswith("|"):
        stripped = stripped[:-1]

    cells: list[str] = []
    current: list[str] = []
    escaped = False

    for ch in stripped:
        if escaped:
            current.append(ch)
            escaped = False
            continue

        if ch == "\\":
            escaped = True
            continue

        if ch == "|":
            cells.append("".join(current).strip())
            current = []
            continue

        current.append(ch)

    if escaped:
        current.append("\\")

    cells.append("".join(current).strip())
    return [cell.replace("\\|", "|") for cell in cells]


def is_table_separator(line: str) -> bool:
    stripped = line.strip().strip("|")
    if not stripped:
        return False
    parts = [part.strip() for part in stripped.split("|")]
    if not parts:
        return False
    for part in parts:
        if not part or not re.fullmatch(r":?-{3,}:?", part):
            return False
    return True


def render_table(header_line: str, separator_line: str, rows: list[str]) -> str:
    headers = split_table_row(header_line)
    alignments = []
    for cell in split_table_row(separator_line):
        left = cell.startswith(":")
        right = cell.endswith(":")
        if left and right:
            alignments.append("center")
        elif left:
            alignments.append("left")
        elif right:
            alignments.append("right")
        else:
            alignments.append("")

    body_rows = [split_table_row(row) for row in rows]
    column_count = max([len(headers), len(alignments), *(len(row) for row in body_rows)] or [0])

    while len(headers) < column_count:
        headers.append("")
    while len(alignments) < column_count:
        alignments.append("")

    normalized_rows: list[list[str]] = []
    for row in body_rows:
        padded = row[:column_count] + [""] * max(0, column_count - len(row))
        normalized_rows.append(padded)

    def cell_attr(index: int) -> str:
        align = alignments[index]
        return f' style="text-align: {align};"' if align else ""

    header_html = "".join(
        f"<th{cell_attr(index)}>{inline_markdown(headers[index])}</th>"
        for index in range(column_count)
    )
    rows_html = []
    for row in normalized_rows:
        cells = "".join(
            f"<td{cell_attr(index)}>{inline_markdown(row[index])}</td>"
            for index in range(column_count)
        )
        rows_html.append(f"<tr>{cells}</tr>")

    return f"<table>\n<thead><tr>{header_html}</tr></thead>\n<tbody>\n{''.join(rows_html)}\n</tbody>\n</table>"


def _highlight_regex(code: str, patterns: list[tuple[str, str]], language_class: str) -> str:
    combined = re.compile("|".join(f"(?P<{name}>{pattern})" for name, pattern in patterns), re.MULTILINE)
    pieces: list[str] = []
    last = 0

    for match in combined.finditer(code):
        start, end = match.span()
        if start > last:
            pieces.append(html.escape(code[last:start]))
        token_type = match.lastgroup or "txt"
        pieces.append(f'<span class="tok-{token_type}">{html.escape(code[start:end])}</span>')
        last = end

    if last < len(code):
        pieces.append(html.escape(code[last:]))

    return f'<pre><code class="{language_class}">{"".join(pieces)}</code></pre>'


def render_code_block(code: str, language: str) -> str:
    lang = language.lower()

    if lang in {"cs", "csharp"}:
        keywords = (
            "using|namespace|class|record|struct|interface|enum|public|private|protected|internal|static|"
            "void|int|string|bool|var|new|return|if|else|switch|case|default|break|continue|for|foreach|"
            "while|do|try|catch|finally|throw|null|true|false|this|base|out|ref|in|is|as|params"
        )
        patterns = [
            ("comment", r"//[^\n]*"),
            ("string", r'"(?:\\.|[^"\\])*"'),
            ("char", r"'(?:\\.|[^'\\])+'"),
            ("number", r"\b\d+(?:\.\d+)?\b"),
            ("keyword", rf"\b(?:{keywords})\b"),
            ("type", r"\b(?:Console|List|File|Directory|Path|Environment|Exception|ConsoleKeyInfo|ConsoleKey)\b"),
        ]
        return wrap_code_block(_highlight_regex(code, patterns, "language-csharp"), lang)

    if lang in {"bash", "sh", "zsh", "shell"}:
        keywords = "if|then|else|fi|for|in|do|done|case|esac|while|function"
        patterns = [
            ("comment", r"#[^\n]*"),
            ("string", r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''),
            ("var", r"\$[A-Za-z_][A-Za-z0-9_]*|\$\{[^}]+\}"),
            ("number", r"\b\d+\b"),
            ("keyword", rf"\b(?:{keywords})\b"),
            ("command", r"^(?:\s*)(?:dotnet|git|cd|ls|cat|rg|sed|python3|bash|zsh|mkdir|cp|mv|rm)\b"),
        ]
        return wrap_code_block(_highlight_regex(code, patterns, "language-shell"), lang)

    language_class = f"language-{lang}" if lang else ""
    return wrap_code_block(_render_plain_code(code, language_class), lang)


def wrap_xhtml_page(title: str, body: str, *, nav: bool = False) -> str:
    nav_attr = ' xmlns:epub="http://www.idpf.org/2007/ops"'
    return f"""<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml"{nav_attr} xml:lang="{BOOK_LANGUAGE}">
  <head>
    <title>{escape(title)}</title>
    <link rel="stylesheet" type="text/css" href="styles.css" />
  </head>
  <body>
    {body}
  </body>
</html>
"""


def build_cover_page() -> str:
    body = """
<section epub:type="cover" class="cover-page">
  <div class="cover-frame">
    <img src="portada.jpg" alt="Portada de Apuntes de Programación III" />
  </div>
</section>
"""
    return wrap_xhtml_page("Portada", body)


def markdown_to_xhtml(markdown_text: str, chapter_title: str, chapter_number: int) -> str:
    markdown_text = strip_leading_title(markdown_text, chapter_title)
    lines = markdown_text.splitlines()
    parts: list[str] = []
    paragraph: list[str] = []
    in_code = False
    code_lines: list[str] = []
    code_language = ""
    list_stack: list[str] = []
    in_blockquote = False
    skipped_first_h1 = False

    def flush_paragraph() -> None:
        nonlocal paragraph
        if paragraph:
            joined = " ".join(line.strip() for line in paragraph).strip()
            if joined:
                parts.append(f"<p>{inline_markdown(joined)}</p>")
        paragraph = []

    def close_lists() -> None:
        nonlocal list_stack
        while list_stack:
            parts.append(f"</{list_stack.pop()}>")

    def close_blockquote() -> None:
        nonlocal in_blockquote
        if in_blockquote:
            flush_paragraph()
            close_lists()
            parts.append("</blockquote>")
            in_blockquote = False

    i = 0
    while i < len(lines):
        line = lines[i].rstrip("\n")
        stripped = line.strip()

        if stripped.startswith("```"):
            flush_paragraph()
            close_lists()
            if in_blockquote:
                close_blockquote()
            if in_code:
                code = "\n".join(code_lines)
                parts.append(render_code_block(code, code_language))
                code_lines = []
                code_language = ""
                in_code = False
            else:
                in_code = True
                code_language = stripped[3:].strip().lower()
            i += 1
            continue

        if in_code:
            code_lines.append(line)
            i += 1
            continue

        if not stripped:
            flush_paragraph()
            close_lists()
            close_blockquote()
            i += 1
            continue

        if stripped == "---":
            flush_paragraph()
            close_lists()
            close_blockquote()
            parts.append("<hr />")
            i += 1
            continue

        if stripped.startswith(">"):
            flush_paragraph()
            close_lists()
            if not in_blockquote:
                parts.append("<blockquote>")
                in_blockquote = True
            quote_text = stripped[1:].lstrip()
            parts.append(f"<p>{inline_markdown(quote_text)}</p>")
            i += 1
            continue

        close_blockquote()

        if "|" in line and i + 1 < len(lines) and is_table_separator(lines[i + 1]):
            flush_paragraph()
            close_lists()
            table_rows: list[str] = []
            cursor = i + 2
            while cursor < len(lines):
                candidate = lines[cursor]
                if not candidate.strip():
                    break
                if "|" not in candidate:
                    break
                table_rows.append(candidate)
                cursor += 1

            parts.append(render_table(line, lines[i + 1], table_rows))
            i = cursor
            continue

        heading_match = re.match(r"^(#{1,6})\s+(.*)$", stripped)
        if heading_match:
            flush_paragraph()
            close_lists()
            level = len(heading_match.group(1))
            title = normalize_heading_text(heading_match.group(2))
            if level == 1 and not skipped_first_h1:
                skipped_first_h1 = True
                i += 1
                continue
            anchor = slugify(title)
            parts.append(f'<h{level} id="{anchor}">{inline_markdown(title)}</h{level}>')
            i += 1
            continue

        ordered_match = re.match(r"^(\d+)\.\s+(.*)$", stripped)
        unordered_match = re.match(r"^[-*]\s+(.*)$", stripped)
        if ordered_match or unordered_match:
            flush_paragraph()
            tag = "ol" if ordered_match else "ul"
            content = ordered_match.group(2) if ordered_match else unordered_match.group(1)
            if not list_stack or list_stack[-1] != tag:
                close_lists()
                parts.append(f"<{tag}>")
                list_stack.append(tag)
            parts.append(f"<li>{inline_markdown(content.strip())}</li>")
            i += 1
            continue

        paragraph.append(line)
        i += 1

    flush_paragraph()
    close_lists()
    close_blockquote()
    if in_code:
        code = "\n".join(code_lines)
        parts.append(render_code_block(code, code_language))

    body = "\n".join(parts)
    chapter_body = f"""
<section epub:type="chapter">
  <header class="chapter-header">
    <p class="chapter-kicker">Capítulo {chapter_number}</p>
    <h1>{inline_markdown(chapter_title)}</h1>
  </header>
  {body}
</section>
"""
    return wrap_xhtml_page(chapter_title, chapter_body)


def build_epub(markdown_files: list[Path]) -> None:
    now = dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat()
    css = """
body {
  color: #202428;
  font-family: Georgia, "Times New Roman", serif;
  font-size: 1em;
  line-height: 1.58;
  margin: 6%;
}
p, ul, ol, table, blockquote, .code-block { margin-top: 0; margin-bottom: 1em; }
h1, h2, h3, h4, h5, h6 {
  color: #111820;
  line-height: 1.18;
  margin: 1.6em 0 0.55em;
  page-break-after: avoid;
}
h1 { font-size: 2em; }
h2 { font-size: 1.45em; }
h3 { font-size: 1.15em; }
h4, h5, h6 { font-size: 1em; }
a { color: #245f73; }
code {
  background: #eef3ef;
  border-radius: 0.25em;
  color: #1e4037;
  font-family: Menlo, Consolas, monospace;
  font-size: 0.92em;
  padding: 0.05em 0.25em;
}
pre { margin: 0; white-space: pre-wrap; }
pre code {
  background: transparent;
  border-radius: 0;
  color: inherit;
  display: block;
  line-height: 1.58;
  padding: 0;
}
.code-block {
  background: #f5f7f4;
  border-left: 0.28em solid #5b7f6b;
  margin: 1.35em 0;
  page-break-inside: avoid;
}
.code-block-header { padding: 0.75em 1em 0; }
.code-block-language {
  color: #56625a;
  font-family: Helvetica, Arial, sans-serif;
  font-size: 0.72em;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}
.code-block pre { padding: 0.45em 1em 1em; }
.code-block code { font-size: 0.9em; }
table {
  border-collapse: collapse;
  font-size: 0.92em;
  margin: 1.2em 0;
  width: 100%;
}
th, td {
  border-bottom: 1px solid #d8ddd8;
  padding: 0.5em 0.65em;
  vertical-align: top;
}
th {
  color: #334137;
  font-family: Helvetica, Arial, sans-serif;
  font-size: 0.82em;
  text-align: left;
}
.tok-comment { color: #5c6f62; font-style: italic; }
.tok-string, .tok-char { color: #27623f; }
.tok-number { color: #7656a0; }
.tok-keyword { color: #8a3b1d; font-weight: 700; }
.tok-type { color: #245f73; }
.tok-var { color: #755a1d; }
.tok-command { color: #245f73; font-weight: 700; }
blockquote {
  border-left: 0.22em solid #8ea899;
  color: #3a4640;
  font-style: italic;
  margin-left: 0;
  padding-left: 1em;
}
hr { border: none; border-top: 1px solid #d8ddd8; margin: 1.5em 0; }
.chapter-header {
  border-bottom: 2px solid #111820;
  margin-bottom: 2.2em;
  padding-bottom: 1em;
}
.chapter-header h1 { margin: 0.25em 0 0; }
.chapter-kicker {
  color: #5b7f6b;
  font-family: Helvetica, Arial, sans-serif;
  font-size: 0.76em;
  font-weight: 700;
  letter-spacing: 0.09em;
  margin: 0;
  text-transform: uppercase;
}
.book-title {
  border-bottom: 2px solid #111820;
  margin: 18% 0 2em;
  padding-bottom: 1em;
}
.book-title h1 { margin-bottom: 0.2em; }
.book-title p {
  color: #5b7f6b;
  font-family: Helvetica, Arial, sans-serif;
  font-size: 0.82em;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}
.toc-list { padding-left: 1.4em; }
.toc-list li { margin: 0.45em 0; padding-left: 0.2em; }
.cover-page { margin: 0; padding: 0; }
.cover-frame { margin: 0 auto; text-align: center; }
.cover-frame img { display: block; height: auto; width: 100%; }
"""

    chapters: list[tuple[str, str, str]] = []
    for index, path in enumerate(markdown_files, start=1):
        source = path.read_text(encoding="utf-8")
        title = first_heading(source, path.stem)
        chapter_file = f"chapter-{index:02d}.xhtml"
        xhtml = markdown_to_xhtml(source, title, index)
        chapters.append((chapter_file, title, xhtml))

    toc_items = "\n".join(
        f'        <li><a href="{filename}">Capítulo {index}: {inline_markdown(title)}</a></li>'
        for index, (filename, title, _) in enumerate(chapters, start=1)
    )

    index_body = f"""
<section epub:type="frontmatter toc">
  <div class="book-title">
    <h1>{escape(BOOK_TITLE)}</h1>
    <p>Índice general</p>
  </div>
  <nav epub:type="toc" id="toc">
    <ol class="toc-list">
{toc_items}
    </ol>
  </nav>
</section>
"""
    nav_xhtml = wrap_xhtml_page("Índice", index_body, nav=True)
    cover_xhtml = build_cover_page()

    manifest_items = [
        '    <item id="cover" href="cover.xhtml" media-type="application/xhtml+xml"/>',
        '    <item id="cover-image" href="portada.jpg" media-type="image/jpeg" properties="cover-image"/>',
        '    <item id="nav" href="nav.xhtml" media-type="application/xhtml+xml" properties="nav"/>',
        '    <item id="css" href="styles.css" media-type="text/css"/>',
    ]
    for index, (filename, _, _) in enumerate(chapters, start=1):
        manifest_items.append(
            f'    <item id="chap{index}" href="{filename}" media-type="application/xhtml+xml"/>'
        )

    spine_items = ['    <itemref idref="cover"/>', '    <itemref idref="nav"/>']
    for index in range(1, len(chapters) + 1):
        spine_items.append(f'    <itemref idref="chap{index}"/>')

    opf = f"""<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://www.idpf.org/2007/opf" unique-identifier="bookid" version="3.0">
  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
    <dc:identifier id="bookid">{escape(BOOK_ID)}</dc:identifier>
    <dc:title>{escape(BOOK_TITLE)}</dc:title>
    <dc:language>{BOOK_LANGUAGE}</dc:language>
    <dc:creator>{escape(BOOK_AUTHOR)}</dc:creator>
    <dc:date>{now}</dc:date>
  </metadata>
  <manifest>
{chr(10).join(manifest_items)}
  </manifest>
  <spine>
{chr(10).join(spine_items)}
  </spine>
</package>
"""

    container_xml = """<?xml version="1.0" encoding="utf-8"?>
<container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
  <rootfiles>
    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
  </rootfiles>
</container>
"""

    with zipfile.ZipFile(OUTPUT, "w") as epub:
        epub.writestr(
            "mimetype",
            "application/epub+zip",
            compress_type=zipfile.ZIP_STORED,
        )
        epub.writestr("META-INF/container.xml", container_xml)
        epub.writestr("OEBPS/styles.css", css)
        epub.writestr("OEBPS/portada.jpg", BOOK_COVER.read_bytes())
        epub.writestr("OEBPS/cover.xhtml", cover_xhtml)
        epub.writestr("OEBPS/nav.xhtml", nav_xhtml)
        epub.writestr("OEBPS/content.opf", opf)
        for filename, _, xhtml in chapters:
            epub.writestr(f"OEBPS/{filename}", xhtml)

def markdown_raiz(root: Path) -> list[Path]:
    return sorted( path for path in root.iterdir() if path.is_file() and path.suffix.lower() == ".md" )

def renumerar(root: Path) -> None:
    PATTERN = re.compile(r"^(?P<seccion>\d{2,})\.(?P<orden>\d{2,})-(?P<nombre>.+)\.md$")
    lista = []
    for path in markdown_raiz(root):
        if m := PATTERN.match(path.name):
            lista.append((int(m.group("seccion")), int(m.group("orden")), m.group("nombre"), path))

    lista.sort(key=lambda item: (item[0], item[1], item[3].name.lower()))

    actual = 0
    siguiente_orden = 0
    for (seccion, _, nombre, origen) in lista:
        if seccion != actual:
            actual = seccion
            siguiente_orden = 0
        siguiente_orden += 10
        destino = f"{actual:02d}.{siguiente_orden:03d}-{nombre.capitalize()}.md"

        if origen.name == destino:
            continue

        print(f"     de: {origen.name:60}\n      a: {destino:60}\n")
        origen.rename(origen.parent / destino)

def main() -> int:
    print("\n\nIniciando proceso de publicacion...\n")
    print("- Paso 1: Renumerar archivos Markdown en raiz...")
    renumerar(WORKDIR)
    markdown_files = [path for path in markdown_raiz(WORKDIR) if not is_excluded(path)]
    if not markdown_files:
        print("No se encontraron archivos Markdown para incluir.", file=sys.stderr)
        return 1

    if not BOOK_COVER.exists():
        print(f"No se encontro la portada: {BOOK_COVER.name}", file=sys.stderr)
        return 1

    print("- Paso 2: Construir el archivo EPUB...")
    build_epub(markdown_files)
    print(f"     Salida: {OUTPUT.name}\n")

    print("- Paso 3: Abrir el libro en Apple Books...")
    subprocess.run(["osascript", "-e", 'tell application "Books" to quit'], check=False)
    subprocess.Popen(
        ["open", "-a", "Books", str(OUTPUT)],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        start_new_session=True,
    )
    print("\nProceso de publicacion completado.\n\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
