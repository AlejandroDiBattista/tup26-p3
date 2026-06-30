#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10,<3.14"
# dependencies = [
#   "pillow>=10.0",
#   "reportlab>=4.2",
# ]
# ///

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import html
import re
import shutil
import subprocess
import sys
import tempfile
import textwrap
from pathlib import Path
from typing import Iterable

try:
    from PIL import Image as PILImage
    from reportlab.lib import colors
    from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT
    from reportlab.lib.pagesizes import A4
    from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
    from reportlab.lib.units import mm
    from reportlab.lib.utils import ImageReader
    from reportlab.pdfbase.pdfmetrics import stringWidth
    from reportlab.platypus import (
        BaseDocTemplate,
        Flowable,
        Frame,
        Image,
        NextPageTemplate,
        PageBreak,
        PageTemplate,
        Paragraph,
        Spacer,
        Table,
        TableStyle,
    )
    from reportlab.platypus.tableofcontents import TableOfContents
except ImportError as exc:
    print(
        "Faltan dependencias para generar el PDF: reportlab y pillow.\n\n"
        "Forma recomendada, sin instalar nada global:\n"
        "  uv run apuntes/publicar_pdf.py\n\n"
        "Alternativa con pip:\n"
        "  python3 -m pip install reportlab pillow\n"
        "  python3 apuntes/publicar_pdf.py",
        file=sys.stderr,
    )
    raise SystemExit(1) from exc

import publicar as epub


SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_OUTPUT = SCRIPT_DIR / "Apuntes-TUP26-P3.pdf"
BOOK_TITLE = epub.BOOK_TITLE
BOOK_SUBTITLE = epub.BOOK_SUBTITLE
BOOK_AUTHOR = epub.BOOK_AUTHOR
BOOK_LANGUAGE = epub.BOOK_LANGUAGE
BOOK_COVER = SCRIPT_DIR / "portada.jpg"
MERMAID_TIMEOUT_SECONDS = epub.MERMAID_TIMEOUT_SECONDS

PAGE_SIZE = A4
PAGE_WIDTH, PAGE_HEIGHT = PAGE_SIZE
BODY_LEFT = 22 * mm
BODY_RIGHT = 19 * mm
BODY_TOP = 24 * mm
BODY_BOTTOM = 23 * mm
ACCENT = colors.HexColor("#0F766E")
INK = colors.HexColor("#1F2937")
MUTED = colors.HexColor("#64748B")
RULE = colors.HexColor("#D6DEE8")
PAPER_TINT = colors.HexColor("#F7FAFC")
CODE_BG = colors.HexColor("#F4F7FB")
CODE_BORDER = colors.HexColor("#D9E2EC")
MONO = "Courier"
MONO_BOLD = "Courier-Bold"
MONO_ITALIC = "Courier-Oblique"
TOKEN_STYLES = {
    "comment": (colors.HexColor("#64748B"), MONO_ITALIC),
    "string": (colors.HexColor("#047857"), MONO),
    "char": (colors.HexColor("#047857"), MONO),
    "number": (colors.HexColor("#7C3AED"), MONO),
    "keyword": (colors.HexColor("#B45309"), MONO_BOLD),
    "directive": (colors.HexColor("#B45309"), MONO_BOLD),
    "razor": (colors.HexColor("#B45309"), MONO_BOLD),
    "type": (colors.HexColor("#0369A1"), MONO),
    "var": (colors.HexColor("#A16207"), MONO),
    "command": (colors.HexColor("#0369A1"), MONO_BOLD),
    "tag": (colors.HexColor("#0369A1"), MONO),
    "attr": (colors.HexColor("#B45309"), MONO),
    "punct": (colors.HexColor("#0369A1"), MONO),
    "doctype": (colors.HexColor("#7C3AED"), MONO_BOLD),
    "json_key": (colors.HexColor("#0369A1"), MONO),
    "literal": (colors.HexColor("#7C3AED"), MONO_BOLD),
}
DEFAULT_CODE_STYLE = (colors.HexColor("#243447"), MONO)


def as_reportlab_markup(text: str) -> str:
    code_spans: list[str] = []
    escaped_chars: list[str] = []
    links: list[tuple[str, str]] = []

    def stash_code(match: re.Match[str]) -> str:
        code_spans.append(html.escape(match.group(1), quote=False))
        return f"@@CODE{len(code_spans) - 1}@@"

    def stash_escaped_char(match: re.Match[str]) -> str:
        escaped_chars.append(html.escape(match.group(1), quote=False))
        return f"@@ESC{len(escaped_chars) - 1}@@"

    def stash_image(match: re.Match[str]) -> str:
        alt = match.group(1).strip() or "imagen"
        return f"[Imagen: {alt}]"

    def stash_link(match: re.Match[str]) -> str:
        links.append((match.group(1), match.group(2)))
        return f"@@LINK{len(links) - 1}@@"

    text = re.sub(r"!\[([^\]]*)\]\(([^)]+)\)", stash_image, text)
    text = re.sub(r"`([^`]+)`", stash_code, text)
    text = re.sub(r"\\([\\`*_{}\[\]()#+\-.!<>|])", stash_escaped_char, text)
    text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", stash_link, text)
    text = html.escape(text, quote=False)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"\*([^*]+)\*", r"<i>\1</i>", text)

    for index, char in enumerate(escaped_chars):
        text = text.replace(f"@@ESC{index}@@", char)
    for index, (label, href) in enumerate(links):
        label_markup = as_reportlab_markup(label)
        safe_href = html.escape(href, quote=True)
        if re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*:", href):
            replacement = (
                f'<link href="{safe_href}">'
                f'<font color="#0F766E"><u>{label_markup}</u></font>'
                f"</link>"
            )
        else:
            replacement = f'<font color="#0F766E"><u>{label_markup}</u></font>'
        text = text.replace(f"@@LINK{index}@@", replacement)
    for index, code in enumerate(code_spans):
        text = text.replace(
            f"@@CODE{index}@@",
            f'<font name="{MONO}" color="#334155">{code}</font>',
        )
    return text


def plain_text_from_markdown(text: str) -> str:
    text = re.sub(r"!\[([^\]]*)\]\([^)]+\)", r"\1", text)
    text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text)
    text = re.sub(r"`([^`]+)`", r"\1", text)
    text = re.sub(r"[*_#]+", "", text)
    return re.sub(r"\s+", " ", text).strip()


def split_table_row(line: str) -> list[str]:
    return epub.split_table_row(line)


def is_table_separator(line: str) -> bool:
    return epub.is_table_separator(line)


def markdown_files(root: Path) -> list[Path]:
    return [path for path in epub.markdown_raiz(root) if not epub.is_excluded(path)]


def mermaid_command() -> list[str]:
    if command := shutil.which("mmdc"):
        return [command]
    if command := shutil.which("npx"):
        return [command, "-y", "@mermaid-js/mermaid-cli"]
    raise RuntimeError(
        "Hay diagramas Mermaid, pero no se encontro Mermaid CLI. "
        "Instale @mermaid-js/mermaid-cli o deje disponible el comando mmdc."
    )


def render_mermaid_png(code: str, asset_dir: Path) -> Path:
    digest = hashlib.sha1(code.encode("utf-8")).hexdigest()[:12]
    output_path = asset_dir / f"mermaid-{digest}.png"
    if output_path.exists():
        return output_path

    input_path = asset_dir / f"mermaid-{digest}.mmd"
    input_path.write_text(code, encoding="utf-8")
    command = [
        *mermaid_command(),
        "-i",
        str(input_path),
        "-o",
        str(output_path),
        "--backgroundColor",
        "transparent",
        "--scale",
        "2",
    ]
    result = subprocess.run(
        command,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        check=False,
        timeout=MERMAID_TIMEOUT_SECONDS,
    )
    if result.returncode != 0 or not output_path.exists():
        details = (result.stderr or result.stdout).strip()
        raise RuntimeError(f"No se pudo renderizar un diagrama Mermaid: {details}")
    return output_path


class CoverPage(Flowable):
    def __init__(self, cover_path: Path):
        super().__init__()
        self.cover_path = cover_path

    def wrap(self, avail_width: float, avail_height: float) -> tuple[float, float]:
        return PAGE_WIDTH, PAGE_HEIGHT

    def draw(self) -> None:
        canvas = self.canv
        if self.cover_path.exists():
            image = ImageReader(str(self.cover_path))
            image_width, image_height = image.getSize()
            scale = max(PAGE_WIDTH / image_width, PAGE_HEIGHT / image_height)
            draw_width = image_width * scale
            draw_height = image_height * scale
            x = (PAGE_WIDTH - draw_width) / 2
            y = (PAGE_HEIGHT - draw_height) / 2
            canvas.drawImage(
                image,
                x,
                y,
                draw_width,
                draw_height,
                preserveAspectRatio=True,
                mask="auto",
            )
        else:
            canvas.setFillColor(colors.white)
            canvas.rect(0, 0, PAGE_WIDTH, PAGE_HEIGHT, fill=1, stroke=0)
            canvas.setFillColor(INK)
            canvas.setFont("Helvetica-Bold", 28)
            canvas.drawCentredString(PAGE_WIDTH / 2, PAGE_HEIGHT / 2 + 18, BOOK_TITLE)
            canvas.setFillColor(ACCENT)
            canvas.setFont("Helvetica", 14)
            canvas.drawCentredString(PAGE_WIDTH / 2, PAGE_HEIGHT / 2 - 12, BOOK_SUBTITLE)


class ChapterParagraph(Paragraph):
    def __init__(self, text: str, style: ParagraphStyle, *, level: int, key: str, plain: str):
        super().__init__(text, style)
        self.toc_level = level
        self.bookmark_key = key
        self.plain_text = plain


class CodeBlock(Flowable):
    def __init__(self, code: str, language: str = "", *, continued: bool = False):
        super().__init__()
        self.code = code.rstrip("\n")
        self.language = epub.code_language_label(language or "texto")
        self.continued = continued
        self.lines: list[str] = []
        self.width = 0.0
        self.height = 0.0
        self.leading = 9.2
        self.font_size = 7.2
        self.padding_x = 8
        self.padding_y = 8
        self.header_height = 10

    def _highlight_patterns(self) -> list[tuple[str, str]]:
        lang = self.language.lower()
        csharp_keywords = (
            "using|namespace|class|record|struct|interface|enum|public|private|protected|internal|static|"
            "void|int|string|bool|var|new|return|if|else|switch|case|default|break|continue|for|foreach|"
            "while|do|try|catch|finally|throw|null|true|false|this|base|out|ref|in|is|as|params|await|"
            "async|get|set|readonly|const|virtual|override|sealed|abstract"
        )

        if lang in {"cs", "csharp", "razor", "cshtml"}:
            return [
                ("comment", r"//.*"),
                ("string", r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"'),
                ("char", r"'(?:\\.|[^'\\])+'"),
                ("number", r"\b\d+(?:\.\d+)?\b"),
                ("directive", r"^\s*@(?:page|model|using|inject|code|functions|implements|inherits|layout|namespace|attribute|typeparam|rendermode|section)\b"),
                ("razor", r"@(?:if|else|switch|for|foreach|while|do|try|catch|finally|lock|using|await)\b|@(?=[{(:])"),
                ("keyword", rf"\b(?:{csharp_keywords})\b"),
                ("type", r"\b(?:Console|List|Dictionary|HashSet|File|Directory|Path|Environment|Exception|DateTime|Task|IEnumerable|IEnumerator|ConsoleKeyInfo|ConsoleKey)\b"),
            ]

        if lang in {"bash", "sh", "zsh", "shell"}:
            shell_keywords = "if|then|else|fi|for|in|do|done|case|esac|while|function"
            return [
                ("comment", r"#.*"),
                ("string", r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''),
                ("var", r"\$[A-Za-z_][A-Za-z0-9_]*|\$\{[^}]+\}"),
                ("number", r"\b\d+\b"),
                ("keyword", rf"\b(?:{shell_keywords})\b"),
                ("command", r"^\s*(?:dotnet|git|cd|ls|cat|rg|sed|python3|python|bash|zsh|mkdir|cp|mv|rm|curl|uv)\b"),
            ]

        if lang in {"htm", "html", "xhtml", "xml"}:
            return [
                ("comment", r"<!--.*?-->"),
                ("doctype", r"<!DOCTYPE(?:\s+[^>]+)?>|<!doctype(?:\s+[^>]+)?>"),
                ("tag", r"</?[A-Za-z][A-Za-z0-9:-]*|<\?[A-Za-z][A-Za-z0-9:-]*"),
                ("attr", r"\b[A-Za-z_:][A-Za-z0-9:._-]*(?=\s*=)"),
                ("string", r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''),
                ("punct", r"\?>|/?>|="),
            ]

        if lang == "json":
            return [
                ("json_key", r'"(?:\\.|[^"\\])*"(?=\s*:)'),
                ("string", r'"(?:\\.|[^"\\])*"'),
                ("number", r"-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b"),
                ("literal", r"\b(?:true|false|null)\b"),
            ]

        return []

    def _highlight_line(self, line: str) -> list[tuple[str, colors.Color, str]]:
        patterns = self._highlight_patterns()
        if not patterns:
            color, font_name = DEFAULT_CODE_STYLE
            return [(line, color, font_name)]

        combined = re.compile("|".join(f"(?P<{name}>{pattern})" for name, pattern in patterns))
        pieces: list[tuple[str, colors.Color, str]] = []
        last = 0
        default_color, default_font = DEFAULT_CODE_STYLE

        for match in combined.finditer(line):
            start, end = match.span()
            if start > last:
                pieces.append((line[last:start], default_color, default_font))
            token_type = match.lastgroup or ""
            color, font_name = TOKEN_STYLES.get(token_type, DEFAULT_CODE_STYLE)
            pieces.append((line[start:end], color, font_name))
            last = end

        if last < len(line):
            pieces.append((line[last:], default_color, default_font))
        return pieces

    def _draw_highlighted_line(self, line: str, x: float, y: float) -> None:
        cursor = x
        for text, color, font_name in self._highlight_line(line):
            if not text:
                continue
            self.canv.setFillColor(color)
            self.canv.setFont(font_name, self.font_size)
            self.canv.drawString(cursor, y, text)
            cursor += stringWidth(text, font_name, self.font_size)

    def _prepare_lines(self, avail_width: float) -> None:
        char_width = max(stringWidth("M", MONO, self.font_size), 1)
        usable = max(avail_width - 2 * self.padding_x, 40)
        max_chars = max(24, int(usable / char_width))
        prepared: list[str] = []
        for raw_line in self.code.splitlines() or [""]:
            expanded = raw_line.expandtabs(4)
            wrapped = textwrap.wrap(
                expanded,
                width=max_chars,
                replace_whitespace=False,
                drop_whitespace=False,
                break_long_words=True,
                break_on_hyphens=False,
            )
            prepared.extend(wrapped or [""])
        self.lines = prepared

    def wrap(self, avail_width: float, avail_height: float) -> tuple[float, float]:
        self.width = avail_width
        self._prepare_lines(avail_width)
        self.height = (
            2 * self.padding_y
            + self.header_height
            + max(1, len(self.lines)) * self.leading
        )
        return self.width, self.height

    def split(self, avail_width: float, avail_height: float) -> list[Flowable]:
        self.wrap(avail_width, avail_height)
        max_lines = int(
            (avail_height - 2 * self.padding_y - self.header_height) / self.leading
        )
        if len(self.lines) <= max_lines or max_lines < 6:
            return []

        first_lines = self.lines[:max_lines]
        rest_lines = self.lines[max_lines:]
        first = CodeBlock("\n".join(first_lines), self.language, continued=self.continued)
        rest = CodeBlock("\n".join(rest_lines), self.language, continued=True)
        return [first, rest]

    def draw(self) -> None:
        canvas = self.canv
        canvas.saveState()
        canvas.setFillColor(CODE_BG)
        canvas.setStrokeColor(CODE_BORDER)
        canvas.setLineWidth(0.6)
        canvas.roundRect(0, 0, self.width, self.height, 5, fill=1, stroke=1)
        canvas.setFillColor(colors.HexColor("#94A3B8"))
        canvas.setFont("Helvetica", 5.6)
        label = self.language.lower()
        if self.continued:
            label = f"{label} - continuacion"
        canvas.drawString(self.padding_x, self.height - self.padding_y - 5.4, label)
        y = self.height - self.padding_y - self.header_height - self.font_size
        for line in self.lines:
            self._draw_highlighted_line(line, self.padding_x, y)
            y -= self.leading
        canvas.restoreState()


class Rule(Flowable):
    def __init__(self, color: colors.Color = RULE, thickness: float = 0.7):
        super().__init__()
        self.color = color
        self.thickness = thickness

    def wrap(self, avail_width: float, avail_height: float) -> tuple[float, float]:
        self.width = avail_width
        return avail_width, self.thickness + 8

    def draw(self) -> None:
        self.canv.saveState()
        self.canv.setStrokeColor(self.color)
        self.canv.setLineWidth(self.thickness)
        self.canv.line(0, 4, self.width, 4)
        self.canv.restoreState()


class BookDocTemplate(BaseDocTemplate):
    def afterFlowable(self, flowable: Flowable) -> None:
        if not isinstance(flowable, ChapterParagraph):
            return

        key = flowable.bookmark_key
        text = flowable.plain_text
        level = flowable.toc_level
        if level != 0:
            return

        previous_outline_level = getattr(self, "_last_outline_level", -1)
        outline_level = min(level, previous_outline_level + 1)
        self.canv.bookmarkPage(key)
        self.canv.addOutlineEntry(text, key, level=outline_level, closed=outline_level > 0)
        self._last_outline_level = outline_level
        self.notify("TOCEntry", (level, html.escape(text, quote=False), self.page, key))


def build_styles() -> dict[str, ParagraphStyle]:
    base = getSampleStyleSheet()
    return {
        "title": ParagraphStyle(
            "BookTitle",
            parent=base["Title"],
            fontName="Helvetica-Bold",
            fontSize=30,
            leading=34,
            textColor=INK,
            spaceAfter=7 * mm,
            alignment=TA_LEFT,
        ),
        "subtitle": ParagraphStyle(
            "BookSubtitle",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=12.5,
            leading=17,
            textColor=colors.HexColor("#334155"),
            spaceAfter=7 * mm,
        ),
        "meta": ParagraphStyle(
            "BookMeta",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=9.3,
            leading=13,
            textColor=MUTED,
            spaceAfter=2 * mm,
        ),
        "toc_title": ParagraphStyle(
            "TocTitle",
            parent=base["Heading1"],
            fontName="Helvetica-Bold",
            fontSize=21,
            leading=25,
            textColor=INK,
            spaceAfter=8 * mm,
        ),
        "disclaimer_title": ParagraphStyle(
            "DisclaimerTitle",
            parent=base["BodyText"],
            fontName="Helvetica-Bold",
            fontSize=9.4,
            leading=12,
            textColor=INK,
            spaceAfter=1.5 * mm,
        ),
        "disclaimer_body": ParagraphStyle(
            "DisclaimerBody",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=8.4,
            leading=12,
            textColor=colors.HexColor("#475569"),
        ),
        "chapter_kicker": ParagraphStyle(
            "ChapterKicker",
            parent=base["BodyText"],
            fontName="Helvetica-Bold",
            fontSize=8,
            leading=10,
            textColor=ACCENT,
            uppercase=True,
            spaceAfter=2 * mm,
        ),
        "chapter_title": ParagraphStyle(
            "ChapterTitle",
            parent=base["Heading1"],
            fontName="Helvetica-Bold",
            fontSize=24,
            leading=28,
            textColor=INK,
            spaceAfter=5 * mm,
            keepWithNext=True,
        ),
        "h2": ParagraphStyle(
            "Heading2",
            parent=base["Heading2"],
            fontName="Helvetica-Bold",
            fontSize=15.5,
            leading=19,
            textColor=INK,
            spaceBefore=9 * mm,
            spaceAfter=3 * mm,
            keepWithNext=True,
        ),
        "h3": ParagraphStyle(
            "Heading3",
            parent=base["Heading3"],
            fontName="Helvetica-Bold",
            fontSize=12.6,
            leading=15.5,
            textColor=colors.HexColor("#334155"),
            spaceBefore=6 * mm,
            spaceAfter=2.2 * mm,
            keepWithNext=True,
        ),
        "h4": ParagraphStyle(
            "Heading4",
            parent=base["Heading4"],
            fontName="Helvetica-Bold",
            fontSize=10.8,
            leading=13.5,
            textColor=colors.HexColor("#475569"),
            spaceBefore=4.5 * mm,
            spaceAfter=1.7 * mm,
            keepWithNext=True,
        ),
        "body": ParagraphStyle(
            "Body",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=9.6,
            leading=14.3,
            textColor=INK,
            spaceAfter=3.2 * mm,
        ),
        "small": ParagraphStyle(
            "Small",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=8.4,
            leading=11.4,
            textColor=colors.HexColor("#475569"),
        ),
        "quote": ParagraphStyle(
            "QuoteText",
            parent=base["BodyText"],
            fontName="Helvetica-Oblique",
            fontSize=9,
            leading=13.2,
            textColor=colors.HexColor("#334155"),
            leftIndent=0,
            rightIndent=0,
        ),
        "table_cell": ParagraphStyle(
            "TableCell",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=8.0,
            leading=10.5,
            textColor=INK,
        ),
        "table_head": ParagraphStyle(
            "TableHead",
            parent=base["BodyText"],
            fontName="Helvetica-Bold",
            fontSize=8.0,
            leading=10.5,
            textColor=colors.white,
        ),
        "list": ParagraphStyle(
            "List",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=9.4,
            leading=13.8,
            textColor=INK,
            spaceAfter=1.8 * mm,
        ),
        "diagram_caption": ParagraphStyle(
            "DiagramCaption",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=7.7,
            leading=10,
            textColor=MUTED,
            alignment=TA_CENTER,
            spaceBefore=1.2 * mm,
            spaceAfter=4 * mm,
        ),
    }


def make_paragraph(text: str, style: ParagraphStyle) -> Paragraph:
    return Paragraph(as_reportlab_markup(text), style)


def make_heading(
    text: str,
    level: int,
    styles: dict[str, ParagraphStyle],
    key: str,
) -> ChapterParagraph:
    style = styles["chapter_title"] if level == 0 else styles.get(f"h{min(level + 1, 4)}", styles["h4"])
    return ChapterParagraph(
        as_reportlab_markup(text),
        style,
        level=level,
        key=key,
        plain=plain_text_from_markdown(text),
    )


def make_quote(lines: list[str], styles: dict[str, ParagraphStyle]) -> Table:
    body = " ".join(line.strip().lstrip(">").strip() for line in lines)
    paragraph = Paragraph(as_reportlab_markup(body), styles["quote"])
    table = Table([[paragraph]], colWidths=["100%"], hAlign="LEFT")
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#F8FAFC")),
                ("BOX", (0, 0), (-1, -1), 0.6, colors.HexColor("#E2E8F0")),
                ("LINEBEFORE", (0, 0), (0, -1), 2.0, ACCENT),
                ("LEFTPADDING", (0, 0), (-1, -1), 9),
                ("RIGHTPADDING", (0, 0), (-1, -1), 9),
                ("TOPPADDING", (0, 0), (-1, -1), 7),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
            ]
        )
    )
    return table


def make_table(
    header_line: str,
    separator_line: str,
    row_lines: list[str],
    styles: dict[str, ParagraphStyle],
) -> Table:
    headers = split_table_row(header_line)
    alignments = []
    for cell in split_table_row(separator_line):
        left = cell.startswith(":")
        right = cell.endswith(":")
        if left and right:
            alignments.append(TA_CENTER)
        elif right:
            alignments.append(TA_RIGHT)
        else:
            alignments.append(TA_LEFT)

    rows = [split_table_row(line) for line in row_lines]
    column_count = max([len(headers), len(alignments), *(len(row) for row in rows)] or [0])
    headers = headers[:column_count] + [""] * max(0, column_count - len(headers))
    alignments = alignments[:column_count] + [TA_LEFT] * max(0, column_count - len(alignments))

    def cell_style(base: ParagraphStyle, alignment: int) -> ParagraphStyle:
        return ParagraphStyle(f"{base.name}-{alignment}", parent=base, alignment=alignment)

    table_data = [
        [
            Paragraph(as_reportlab_markup(headers[index]), cell_style(styles["table_head"], alignments[index]))
            for index in range(column_count)
        ]
    ]
    for row in rows:
        row = row[:column_count] + [""] * max(0, column_count - len(row))
        table_data.append(
            [
                Paragraph(as_reportlab_markup(row[index]), cell_style(styles["table_cell"], alignments[index]))
                for index in range(column_count)
            ]
        )

    table = Table(table_data, repeatRows=1, hAlign="LEFT")
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), ACCENT),
                ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#D7DEE8")),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F8FAFC")]),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 5),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
            ]
        )
    )
    return table


def make_diagram(path: Path, styles: dict[str, ParagraphStyle]) -> list[Flowable]:
    with PILImage.open(path) as image:
        width_px, height_px = image.size
    max_width = PAGE_WIDTH - BODY_LEFT - BODY_RIGHT
    max_height = 120 * mm
    ratio = min(max_width / width_px, max_height / height_px, 1.0)
    drawing = Image(str(path), width_px * ratio, height_px * ratio)
    drawing.hAlign = "CENTER"
    return [
        Spacer(1, 1.5 * mm),
        drawing,
        Paragraph("Diagrama", styles["diagram_caption"]),
    ]


def make_disclaimer(styles: dict[str, ParagraphStyle]) -> Table:
    title = Paragraph("Nota sobre este material", styles["disclaimer_title"])
    body = Paragraph(
        "Este material fue realizado como soporte al dictado de clase. "
        "Para su elaboración se empleó inteligencia artificial como ayuda "
        "en tareas de redacción y corrección; la selección, organización "
        "y revisión pedagógica corresponden al docente.",
        styles["disclaimer_body"],
    )
    table = Table([[title], [body]], colWidths=["100%"], hAlign="LEFT")
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#F8FAFC")),
                ("BOX", (0, 0), (-1, -1), 0.6, colors.HexColor("#D7DEE8")),
                ("LINEBEFORE", (0, 0), (0, -1), 2.0, ACCENT),
                ("LEFTPADDING", (0, 0), (-1, -1), 9),
                ("RIGHTPADDING", (0, 0), (-1, -1), 9),
                ("TOPPADDING", (0, 0), (-1, 0), 7),
                ("BOTTOMPADDING", (0, 0), (-1, 0), 0),
                ("TOPPADDING", (0, 1), (-1, 1), 0),
                ("BOTTOMPADDING", (0, 1), (-1, 1), 7),
            ]
        )
    )
    return table


def flush_list(
    story: list[Flowable],
    list_items: list[tuple[str, str]],
    styles: dict[str, ParagraphStyle],
) -> None:
    if not list_items:
        return

    first_kind = list_items[0][0]
    list_style = ParagraphStyle(
        f"List-{first_kind}",
        parent=styles["list"],
        leftIndent=14,
        firstLineIndent=0,
        bulletIndent=0,
        bulletFontName="Helvetica-Bold",
        bulletFontSize=8.3,
        bulletColor=ACCENT,
    )
    for index, (_, item) in enumerate(list_items, start=1):
        marker = f"{index}." if first_kind == "ol" else "•"
        story.append(
            Paragraph(
                as_reportlab_markup(item),
                list_style,
                bulletText=marker,
            )
        )
    story.append(Spacer(1, 1.5 * mm))
    list_items.clear()


def parse_markdown(
    markdown_text: str,
    chapter_title: str,
    chapter_index: int,
    styles: dict[str, ParagraphStyle],
    asset_dir: Path,
) -> list[Flowable]:
    markdown_text = epub.strip_leading_title(markdown_text, chapter_title)
    lines = markdown_text.splitlines()
    story: list[Flowable] = []
    paragraph: list[str] = []
    list_items: list[tuple[str, str]] = []
    skipped_first_h1 = False
    heading_counter = 0

    def flush_paragraph() -> None:
        if paragraph:
            joined = " ".join(line.strip() for line in paragraph).strip()
            if joined:
                story.append(make_paragraph(joined, styles["body"]))
            paragraph.clear()

    i = 0
    while i < len(lines):
        line = lines[i].rstrip("\n")
        stripped = line.strip()

        if stripped.startswith("```"):
            flush_paragraph()
            flush_list(story, list_items, styles)
            language = stripped[3:].strip().lower()
            code_lines: list[str] = []
            i += 1
            while i < len(lines) and not lines[i].strip().startswith("```"):
                code_lines.append(lines[i].rstrip("\n"))
                i += 1
            if i < len(lines):
                i += 1
            code = "\n".join(code_lines)
            if language == "mermaid":
                diagram_path = render_mermaid_png(code, asset_dir)
                story.extend(make_diagram(diagram_path, styles))
            else:
                story.append(CodeBlock(code, language))
                story.append(Spacer(1, 2.2 * mm))
            continue

        if not stripped:
            flush_paragraph()
            flush_list(story, list_items, styles)
            i += 1
            continue

        if stripped == "---":
            flush_paragraph()
            flush_list(story, list_items, styles)
            story.append(Rule())
            i += 1
            continue

        if stripped.startswith(">"):
            flush_paragraph()
            flush_list(story, list_items, styles)
            quote_lines: list[str] = []
            while i < len(lines) and lines[i].strip().startswith(">"):
                quote_lines.append(lines[i].strip())
                i += 1
            story.append(make_quote(quote_lines, styles))
            story.append(Spacer(1, 3 * mm))
            continue

        if "|" in line and i + 1 < len(lines) and is_table_separator(lines[i + 1]):
            flush_paragraph()
            flush_list(story, list_items, styles)
            row_lines: list[str] = []
            cursor = i + 2
            while cursor < len(lines):
                candidate = lines[cursor]
                if not candidate.strip() or "|" not in candidate:
                    break
                row_lines.append(candidate)
                cursor += 1
            story.append(make_table(line, lines[i + 1], row_lines, styles))
            story.append(Spacer(1, 3 * mm))
            i = cursor
            continue

        heading_match = re.match(r"^(#{1,6})\s+(.*)$", stripped)
        if heading_match:
            flush_paragraph()
            flush_list(story, list_items, styles)
            level = len(heading_match.group(1))
            title = epub.normalize_heading_text(heading_match.group(2))
            if level == 1 and not skipped_first_h1:
                skipped_first_h1 = True
                i += 1
                continue
            heading_counter += 1
            toc_level = min(max(level - 1, 1), 3)
            key = f"ch{chapter_index:02d}-h{heading_counter:03d}-{epub.slugify(title)}"
            story.append(make_heading(title, toc_level, styles, key))
            i += 1
            continue

        ordered_match = re.match(r"^(\d+)\.\s+(.*)$", stripped)
        unordered_match = re.match(r"^[-*]\s+(.*)$", stripped)
        if ordered_match or unordered_match:
            flush_paragraph()
            kind = "ol" if ordered_match else "ul"
            content = ordered_match.group(2) if ordered_match else unordered_match.group(1)
            if list_items and list_items[0][0] != kind:
                flush_list(story, list_items, styles)
            list_items.append((kind, content.strip()))
            i += 1
            continue

        paragraph.append(line)
        i += 1

    flush_paragraph()
    flush_list(story, list_items, styles)
    return story


def body_page(canvas, doc) -> None:
    canvas.saveState()
    canvas.setFillColor(colors.white)
    canvas.rect(0, 0, PAGE_WIDTH, PAGE_HEIGHT, fill=1, stroke=0)
    canvas.setStrokeColor(RULE)
    canvas.setLineWidth(0.5)
    canvas.line(BODY_LEFT, PAGE_HEIGHT - 15 * mm, PAGE_WIDTH - BODY_RIGHT, PAGE_HEIGHT - 15 * mm)
    canvas.setFillColor(MUTED)
    canvas.setFont("Helvetica", 7.5)
    canvas.drawString(BODY_LEFT, PAGE_HEIGHT - 11 * mm, BOOK_TITLE)
    canvas.drawRightString(PAGE_WIDTH - BODY_RIGHT, PAGE_HEIGHT - 11 * mm, "UTN / FRT / TUP 26")
    canvas.setStrokeColor(RULE)
    canvas.line(BODY_LEFT, 15 * mm, PAGE_WIDTH - BODY_RIGHT, 15 * mm)
    canvas.setFillColor(MUTED)
    canvas.setFont("Helvetica", 8)
    canvas.drawRightString(PAGE_WIDTH - BODY_RIGHT, 9.5 * mm, f"Página {doc.page}")
    canvas.restoreState()


def front_page(canvas, doc) -> None:
    canvas.saveState()
    canvas.setFillColor(colors.white)
    canvas.rect(0, 0, PAGE_WIDTH, PAGE_HEIGHT, fill=1, stroke=0)
    canvas.restoreState()


def build_pdf(root: Path, output: Path, *, should_renumerar: bool = True) -> None:
    if should_renumerar:
        epub.renumerar(root)

    files = markdown_files(root)
    if not files:
        raise RuntimeError(f"No se encontraron Markdown para incluir en {root}")

    output.parent.mkdir(parents=True, exist_ok=True)
    tmp_root = root / "tmp" / "pdfs"
    tmp_root.mkdir(parents=True, exist_ok=True)

    styles = build_styles()
    doc = BookDocTemplate(
        str(output),
        pagesize=PAGE_SIZE,
        leftMargin=BODY_LEFT,
        rightMargin=BODY_RIGHT,
        topMargin=BODY_TOP,
        bottomMargin=BODY_BOTTOM,
        title=BOOK_TITLE,
        author=BOOK_AUTHOR,
        subject=BOOK_SUBTITLE,
        creator="publicar_pdf.py",
        lang=BOOK_LANGUAGE,
    )

    cover_frame = Frame(
        0,
        0,
        PAGE_WIDTH,
        PAGE_HEIGHT,
        id="cover-frame",
        leftPadding=0,
        rightPadding=0,
        topPadding=0,
        bottomPadding=0,
        showBoundary=0,
    )
    front_frame = Frame(
        BODY_LEFT,
        BODY_BOTTOM,
        PAGE_WIDTH - BODY_LEFT - BODY_RIGHT,
        PAGE_HEIGHT - BODY_TOP - BODY_BOTTOM,
        id="front-frame",
        showBoundary=0,
    )
    body_frame = Frame(
        BODY_LEFT,
        BODY_BOTTOM,
        PAGE_WIDTH - BODY_LEFT - BODY_RIGHT,
        PAGE_HEIGHT - BODY_TOP - BODY_BOTTOM,
        id="body-frame",
        showBoundary=0,
    )
    doc.addPageTemplates(
        [
            PageTemplate(id="Cover", frames=[cover_frame], onPage=front_page),
            PageTemplate(id="Front", frames=[front_frame], onPage=front_page),
            PageTemplate(id="Body", frames=[body_frame], onPage=body_page),
        ]
    )

    toc = TableOfContents()
    toc.levelStyles = [
        ParagraphStyle("TOC0", fontName="Helvetica-Bold", fontSize=9.8, leading=13.2, leftIndent=0, firstLineIndent=0, spaceBefore=1.8 * mm, textColor=INK),
        ParagraphStyle("TOC1", fontName="Helvetica", fontSize=8.8, leading=11.6, leftIndent=9 * mm, firstLineIndent=0, textColor=colors.HexColor("#334155")),
        ParagraphStyle("TOC2", fontName="Helvetica", fontSize=8.2, leading=10.8, leftIndent=17 * mm, firstLineIndent=0, textColor=colors.HexColor("#475569")),
        ParagraphStyle("TOC3", fontName="Helvetica", fontSize=8.0, leading=10.5, leftIndent=24 * mm, firstLineIndent=0, textColor=colors.HexColor("#64748B")),
    ]

    story: list[Flowable] = [
        CoverPage(BOOK_COVER),
        NextPageTemplate("Front"),
        PageBreak(),
        Spacer(1, 28 * mm),
        Paragraph("PROGRAMACIÓN III", styles["meta"]),
        Rule(ACCENT, 1.3),
        Spacer(1, 8 * mm),
        Paragraph(BOOK_TITLE, styles["title"]),
        Paragraph(BOOK_SUBTITLE, styles["subtitle"]),
        Spacer(1, 58 * mm),
        Paragraph(BOOK_AUTHOR, styles["meta"]),
        Paragraph(dt.date.today().strftime("%d/%m/%Y"), styles["meta"]),
        Spacer(1, 14 * mm),
        make_disclaimer(styles),
        PageBreak(),
        Paragraph("Índice", styles["toc_title"]),
        toc,
        NextPageTemplate("Body"),
        PageBreak(),
    ]

    with tempfile.TemporaryDirectory(prefix="apuntes-pdf-", dir=tmp_root) as tmpdir:
        asset_dir = Path(tmpdir)
        for chapter_index, path in enumerate(files, start=1):
            source = path.read_text(encoding="utf-8")
            title = epub.first_heading(source, path.stem)
            if chapter_index > 1:
                story.append(PageBreak())
            story.append(Paragraph(f"CAPITULO {chapter_index:02d}", styles["chapter_kicker"]))
            story.append(
                make_heading(
                    title,
                    0,
                    styles,
                    f"ch{chapter_index:02d}-{epub.slugify(title)}",
                )
            )
            story.append(Rule())
            story.append(Spacer(1, 5 * mm))
            story.extend(parse_markdown(source, title, chapter_index, styles, asset_dir))

        doc.multiBuild(story)


def open_pdf(path: Path) -> None:
    if sys.platform == "darwin":
        subprocess.Popen(
            ["open", str(path)],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            start_new_session=True,
        )


def parse_args(argv: Iterable[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Genera el libro de apuntes en PDF.")
    parser.add_argument(
        "--root",
        type=Path,
        default=SCRIPT_DIR,
        help="Directorio donde estan los Markdown de apuntes.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_OUTPUT,
        help="Archivo PDF de salida.",
    )
    parser.add_argument(
        "--no-renumerar",
        action="store_true",
        help="No renumerar archivos Markdown antes de publicar.",
    )
    parser.add_argument(
        "--no-open",
        action="store_true",
        help="No abrir el PDF al terminar.",
    )
    return parser.parse_args(list(argv))


def main(argv: Iterable[str] = sys.argv[1:]) -> int:
    args = parse_args(argv)
    root = args.root.resolve()
    output = args.output.resolve()

    print("\n\nIniciando proceso de publicacion PDF...\n")
    print(f"- Directorio de apuntes: {root}")
    print("- Construir el libro PDF...")
    build_pdf(root, output, should_renumerar=not args.no_renumerar)
    print(f"     Salida: {output}\n")

    if not args.no_open:
        print("- Abrir el PDF...")
        open_pdf(output)

    print("\nProceso de publicacion PDF completado.\n\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
