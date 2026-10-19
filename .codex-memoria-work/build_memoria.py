from __future__ import annotations

import hashlib
import os
import shutil
import textwrap
from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK, WD_LINE_SPACING
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Inches, Pt, RGBColor
from PIL import Image


SOURCE = Path(r"C:\Users\alons\Downloads\DJD - Plantilla Memoria.docx")
OUTPUT = Path(r"C:\Users\alons\FLOWSTATE\Deliverables\FlowState_Memoria_Proyecto_Titulo_2026-10-06.docx")
EXPECTED_SOURCE_SHA256 = "BE8B8A9225062BE01D79AC11CF0511C17A9B9CEE26D2DFE51BABAD547EFF5127"

IMAGES = {
    "menu": Path(r"C:\Users\alons\FLOWSTATE\Docs\AI\MainMenuPreview.png"),
    "graffiti_menu": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\GraffitiMenu_Refined_Close.png"),
    "nozzles": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\graffiti_five_nozzle_profiles.png"),
    "parkour": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\parkour_course_final.png"),
    "camera": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\third_person_camera_final.png"),
    "hud": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\spray_hud_full_final.png"),
    "fssrs_flow": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\fssrs-v3-state-flow-max-rainbow-a.png"),
    "fssrs_mono": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\fssrs-v3-state-monochrome-final.png"),
    "intro": Path(r"C:\Users\alons\FLOWSTATE\Captures\Phase3ABReview\IntroPanels_02_SixComplete.png"),
    "contact": Path(r"C:\Users\alons\FLOWSTATE\Captures\FlowState_v6_FSSRS_contact.png"),
    "music": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\music_device_prototype_final.png"),
    "combat": Path(r"C:\Users\alons\FLOWSTATE\Assets\Screenshots\combat_lockon_thick_silhouette_vignette_final.png"),
}


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def remove_element(element) -> None:
    parent = element.getparent()
    if parent is not None:
        parent.remove(element)


def replace_paragraph_text(paragraph, text: str) -> None:
    if paragraph.runs:
        paragraph.runs[0].text = text
        for run in paragraph.runs[1:]:
            run._element.getparent().remove(run._element)
    else:
        paragraph.add_run(text)


def set_run_font(run, size: float | None = None, bold: bool | None = None,
                 italic: bool | None = None, color: str | None = None) -> None:
    run.font.name = "Arial"
    run._element.get_or_add_rPr().rFonts.set(qn("w:eastAsia"), "Arial")
    if size is not None:
        run.font.size = Pt(size)
    if bold is not None:
        run.bold = bold
    if italic is not None:
        run.italic = italic
    if color is not None:
        run.font.color.rgb = RGBColor.from_string(color)
    lang = run._element.get_or_add_rPr().find(qn("w:lang"))
    if lang is None:
        lang = OxmlElement("w:lang")
        run._element.get_or_add_rPr().append(lang)
    lang.set(qn("w:val"), "es-CL")
    lang.set(qn("w:eastAsia"), "es-CL")


def add_widow_control(paragraph) -> None:
    ppr = paragraph._p.get_or_add_pPr()
    if ppr.find(qn("w:widowControl")) is None:
        ppr.append(OxmlElement("w:widowControl"))


def body_paragraph(doc: Document, text: str, *, italic: bool = False,
                   bold_lead: str | None = None, align=WD_ALIGN_PARAGRAPH.JUSTIFY,
                   after: float = 6.0, first_line: float = 0.0):
    p = doc.add_paragraph(style="Normal")
    p.alignment = align
    pf = p.paragraph_format
    pf.space_after = Pt(after)
    pf.line_spacing = 1.15
    if first_line:
        pf.first_line_indent = Cm(first_line)
    add_widow_control(p)
    if bold_lead and text.startswith(bold_lead):
        r1 = p.add_run(bold_lead)
        set_run_font(r1, 11, bold=True)
        r2 = p.add_run(text[len(bold_lead):])
        set_run_font(r2, 11, italic=italic)
    else:
        r = p.add_run(text)
        set_run_font(r, 11, italic=italic)
    return p


def add_paragraphs(doc: Document, block: str) -> None:
    clean = textwrap.dedent(block).strip()
    for part in clean.split("\n\n"):
        text = " ".join(line.strip() for line in part.splitlines()).strip()
        if text:
            body_paragraph(doc, text)


def add_heading(doc: Document, text: str, level: int, *, page_break_before: bool = False):
    p = doc.add_paragraph(style=f"Heading {level}")
    p.paragraph_format.keep_with_next = True
    p.paragraph_format.space_before = Pt(12 if level == 1 else 8)
    p.paragraph_format.space_after = Pt(6)
    p.paragraph_format.page_break_before = page_break_before
    r = p.add_run(text)
    sizes = {1: 20, 2: 16, 3: 14}
    set_run_font(r, sizes[level], bold=True, color="000000" if level < 3 else "434343")
    return p


def add_bullet(doc: Document, text: str) -> None:
    p = doc.add_paragraph(style="Normal")
    p.paragraph_format.left_indent = Cm(0.65)
    p.paragraph_format.first_line_indent = Cm(-0.35)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.line_spacing = 1.15
    add_widow_control(p)
    r = p.add_run("•  " + text)
    set_run_font(r, 11)


def set_cell_shading(cell, fill: str) -> None:
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = tc_pr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        tc_pr.append(shd)
    shd.set(qn("w:fill"), fill)


def set_cell_margins(cell, top=70, start=90, bottom=70, end=90) -> None:
    tc = cell._tc
    tc_pr = tc.get_or_add_tcPr()
    tc_mar = tc_pr.first_child_found_in("w:tcMar")
    if tc_mar is None:
        tc_mar = OxmlElement("w:tcMar")
        tc_pr.append(tc_mar)
    for m, v in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = tc_mar.find(qn(f"w:{m}"))
        if node is None:
            node = OxmlElement(f"w:{m}")
            tc_mar.append(node)
        node.set(qn("w:w"), str(v))
        node.set(qn("w:type"), "dxa")


def prevent_row_split(row) -> None:
    tr_pr = row._tr.get_or_add_trPr()
    cant_split = OxmlElement("w:cantSplit")
    tr_pr.append(cant_split)


def repeat_header(row) -> None:
    tr_pr = row._tr.get_or_add_trPr()
    tbl_header = OxmlElement("w:tblHeader")
    tbl_header.set(qn("w:val"), "true")
    tr_pr.append(tbl_header)


def set_table_borders(table, color: str = "B7B7B7", size: str = "6") -> None:
    tbl_pr = table._tbl.tblPr
    borders = tbl_pr.find(qn("w:tblBorders"))
    if borders is None:
        borders = OxmlElement("w:tblBorders")
        tbl_pr.append(borders)
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        node = borders.find(qn(f"w:{edge}"))
        if node is None:
            node = OxmlElement(f"w:{edge}")
            borders.append(node)
        node.set(qn("w:val"), "single")
        node.set(qn("w:sz"), size)
        node.set(qn("w:space"), "0")
        node.set(qn("w:color"), color)


def add_table(doc: Document, headers: list[str], rows: list[list[str]], widths: list[float] | None = None,
              font_size: float = 9.3):
    table = doc.add_table(rows=1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = True
    set_table_borders(table)
    table.rows[0].height = None
    repeat_header(table.rows[0])
    for i, header in enumerate(headers):
        cell = table.rows[0].cells[i]
        cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
        set_cell_shading(cell, "333333")
        set_cell_margins(cell)
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        p.paragraph_format.space_after = Pt(0)
        r = p.add_run(header)
        set_run_font(r, font_size, bold=True, color="FFFFFF")
    prevent_row_split(table.rows[0])

    for r_idx, row_data in enumerate(rows, start=1):
        row = table.add_row()
        prevent_row_split(row)
        for c_idx, value in enumerate(row_data):
            cell = row.cells[c_idx]
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.TOP
            set_cell_margins(cell)
            if r_idx % 2 == 0:
                set_cell_shading(cell, "F2F2F2")
            p = cell.paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            p.paragraph_format.line_spacing = 1.0
            r = p.add_run(str(value))
            set_run_font(r, font_size)
    if widths:
        for row in table.rows:
            for i, width in enumerate(widths):
                row.cells[i].width = Cm(width)
    spacer = doc.add_paragraph()
    spacer.paragraph_format.space_after = Pt(2)
    return table


def set_alt_text(shape, title: str, description: str) -> None:
    doc_prs = shape._inline.xpath("./wp:docPr")
    if doc_prs:
        doc_pr = doc_prs[0]
        doc_pr.set("title", title)
        doc_pr.set("descr", description)


def add_figure(doc: Document, image_path: Path, caption: str, alt_text: str,
               max_width_cm: float = 15.6, max_height_cm: float = 9.1) -> None:
    if not image_path.exists():
        raise FileNotFoundError(image_path)
    with Image.open(image_path) as im:
        width_px, height_px = im.size
    ratio = width_px / max(height_px, 1)
    width_cm = max_width_cm
    height_cm = width_cm / ratio
    if height_cm > max_height_cm:
        height_cm = max_height_cm
        width_cm = height_cm * ratio

    p = doc.add_paragraph(style="Normal")
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(6)
    p.paragraph_format.space_after = Pt(3)
    p.paragraph_format.keep_with_next = True
    run = p.add_run()
    shape = run.add_picture(str(image_path), width=Cm(width_cm), height=Cm(height_cm))
    set_alt_text(shape, caption.split(".")[0], alt_text)

    cp = doc.add_paragraph(style="Normal")
    cp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    cp.paragraph_format.space_after = Pt(8)
    cp.paragraph_format.keep_together = True
    r = cp.add_run(caption)
    set_run_font(r, 9, italic=True, color="555555")


def add_callout(doc: Document, title: str, text: str) -> None:
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = True
    cell = table.cell(0, 0)
    set_cell_shading(cell, "F0F0F0")
    set_cell_margins(cell, 120, 140, 120, 140)
    p = cell.paragraphs[0]
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.line_spacing = 1.1
    r1 = p.add_run(title + ": ")
    set_run_font(r1, 10, bold=True)
    r2 = p.add_run(text)
    set_run_font(r2, 10)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)


def add_toc_field(doc: Document) -> None:
    p = doc.add_paragraph(style="Normal")
    p.paragraph_format.space_after = Pt(6)
    run = p.add_run()
    fld_char = OxmlElement("w:fldChar")
    fld_char.set(qn("w:fldCharType"), "begin")
    instr = OxmlElement("w:instrText")
    instr.set(qn("xml:space"), "preserve")
    instr.text = ' TOC \\o "1-3" \\h \\z \\u '
    separate = OxmlElement("w:fldChar")
    separate.set(qn("w:fldCharType"), "separate")
    placeholder = OxmlElement("w:t")
    placeholder.text = "Actualice este campo para generar el índice."
    end = OxmlElement("w:fldChar")
    end.set(qn("w:fldCharType"), "end")
    run._r.append(fld_char)
    run._r.append(instr)
    run._r.append(separate)
    run._r.append(placeholder)
    run._r.append(end)


def mark_update_fields(doc: Document) -> None:
    settings = doc.settings._element
    update = settings.find(qn("w:updateFields"))
    if update is None:
        update = OxmlElement("w:updateFields")
        settings.append(update)
    update.set(qn("w:val"), "true")


def add_page_break(doc: Document) -> None:
    p = doc.add_paragraph(style="Normal")
    p.paragraph_format.space_after = Pt(0)
    p.add_run().add_break(WD_BREAK.PAGE)


def build() -> None:
    actual_hash = sha256(SOURCE)
    if actual_hash != EXPECTED_SOURCE_SHA256:
        raise RuntimeError(f"Source template hash changed: {actual_hash}")
    for key, path in IMAGES.items():
        if not path.exists():
            raise FileNotFoundError(f"Missing figure {key}: {path}")

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(SOURCE, OUTPUT)
    doc = Document(OUTPUT)

    # Preserve the cover and all package-level design assets; replace only its text slots.
    replace_paragraph_text(doc.paragraphs[12], "Flow State")
    replace_paragraph_text(doc.paragraphs[13], "Profesor guía: [Por completar]")
    replace_paragraph_text(doc.paragraphs[23], "Estudiante(s): [Por completar]")
    replace_paragraph_text(doc.paragraphs[24], "6 de octubre de 2026")

    # Remove template body after the cover, leaving section properties untouched.
    for p in list(doc.paragraphs[25:]):
        remove_element(p._element)

    # Strengthen the inherited Normal style without changing the visual language.
    normal = doc.styles["Normal"]
    normal.font.name = "Arial"
    normal._element.get_or_add_rPr().rFonts.set(qn("w:eastAsia"), "Arial")
    normal.font.size = Pt(11)

    add_page_break(doc)
    add_heading(doc, "1.- Índice", 1)
    add_toc_field(doc)
    add_callout(
        doc,
        "Nota de edición",
        "El índice es un campo automático de Word. Si la paginación cambia después de futuras ediciones, seleccione el índice y use “Actualizar campo” para recalcular títulos y páginas.",
    )

    add_heading(doc, "2.- Agradecimientos", 1, page_break_before=True)
    add_paragraphs(doc, """
        Este proyecto reconoce el acompañamiento de docentes, compañeros, personas que participaron en revisiones y quienes aportaron observaciones durante las pruebas del prototipo. Sus preguntas ayudaron a convertir una idea inicialmente centrada en la estética del graffiti en una propuesta de diseño más precisa, capaz de relacionar expresión, riesgo, desplazamiento y consecuencias dentro de una ciudad ficticia.

        También se agradece a las comunidades artísticas y urbanas cuya producción visual mantiene abierta la discusión sobre memoria, pertenencia y derecho a la ciudad. Flow State no pretende hablar en nombre de ellas: utiliza la investigación y el diseño de sistemas para aproximarse a esas tensiones con respeto, reconociendo que el graffiti posee historias, códigos y contextos que exceden el videojuego.

        Esta sección queda deliberadamente redactada de forma institucional hasta completar los nombres de estudiante(s), profesor guía, colaboradores y participantes autorizados de la evaluación final. La versión entregada no atribuye aportes individuales que no hayan sido verificados.
    """)

    add_heading(doc, "3.- Introducción", 1, page_break_before=True)
    add_paragraphs(doc, """
        Flow State es un videojuego de aventura urbana narrativa que integra graffiti, sigilo, parkour y exploración. Su propuesta sitúa al jugador en una ciudad latinoamericana ficticia inspirada en Santiago de Chile, en un período visual y tecnológico cercano al cambio de milenio. El protagonista, Maikol “Maik” Hernández, regresa al país después de haber vivido parte de su juventud en Nueva York, donde encontró en el graffiti una forma de lectura del espacio y de construcción de identidad. Al volver, descubre una ciudad sometida a la campaña municipal Ciudad Clara: una política que borra murales, vigila agrupaciones juveniles y busca transformar la imagen pública mediante control, limpieza y relato mediático.

        El proyecto parte de una pregunta de diseño: ¿cómo puede un videojuego hacer que pintar una superficie sea una decisión expresiva, espacial y social, en lugar de una acción automática que solo entrega puntos? La respuesta se desarrolla a través de un ciclo jugable que exige observar el barrio, escoger una intención, planificar una ruta, infiltrarse, pintar manualmente, escapar y reconocer las consecuencias. El graffiti no funciona únicamente como decoración; modifica la percepción del territorio, la reputación de Maik y la relación entre agrupaciones. El movimiento tampoco se entiende como una secuencia aislada de acrobacias, sino como el lenguaje corporal que conecta preparación, riesgo y autoría.

        El título alude al concepto de flujo formulado por Mihaly Csikszentmihalyi: un estado de concentración intensa en que desafío y habilidad se equilibran, la retroalimentación es inmediata y la acción adquiere continuidad. Flow State traduce esa idea a un sistema donde desplazamiento, lectura ambiental, ritmo, control del aerosol, cámara y sonido intentan sostener una experiencia coherente. Cuando el jugador domina el recorrido y la ejecución, la interfaz cede protagonismo y la ciudad se convierte en una secuencia de decisiones legibles. Cuando aumenta la presión, el mismo sistema introduce interrupciones, escasez y exposición.

        La historia propone un conflicto entre expresión y responsabilidad. Maik debe reconstruir su vínculo con la ciudad mientras conoce posturas distintas dentro de la escena local. Nadir y Vela tensionan su forma de entender el arte, la pertenencia y el impacto de intervenir espacios compartidos. Ciudad Clara actúa como antagonista institucional, pero el relato evita reducir el problema a una oposición simple entre jóvenes y autoridad: las decisiones del jugador pueden fortalecer redes, producir rechazo, desplazar voces o convertir una acción significativa en mera firma personal. El propósito es que la libertad artística tenga consecuencias observables y que el jugador deba interpretar a quién beneficia cada intervención.

        La memoria documenta el proyecto como obra en desarrollo al 6 de octubre de 2026. Describe su marco conceptual, referentes, decisiones de preproducción, implementación en Unity, dirección visual, narrativa, sonido, pruebas y limitaciones. La evidencia proviene del repositorio, escenas, scripts, materiales, capturas, informes de fases y resultados de pruebas disponibles. Por esa razón se distingue entre características implementadas, prototipos validados, aspiraciones de diseño y tareas pendientes. No se presenta como final aquello que aún no cuenta con build independiente, prueba física o medición formal.

        El alcance actual permite demostrar el núcleo de interacción y la identidad del proyecto: locomoción en tercera persona, transición a pintura en primera persona, sistema de aerosol con recursos, herramientas de color y boquillas, movimientos de parkour, menú diegético, estados visuales, reputación persistente y una pieza cinematográfica de presentación. Quedan fuera de la validación final una campaña completa, la integración definitiva de audio, el empaquetado para Windows, pruebas prolongadas con mando y un perfil de rendimiento en hardware objetivo. Estas ausencias forman parte de la conclusión y del plan de continuidad, no se ocultan como detalles menores.
    """)

    add_heading(doc, "3.1.- Ficha técnica del proyecto", 2)
    add_table(
        doc,
        ["Campo", "Definición"],
        [
            ["Título", "Flow State"],
            ["Formato", "Videojuego digital; prototipo jugable de proyecto de título"],
            ["Género", "Aventura urbana narrativa con graffiti, sigilo, parkour y exploración"],
            ["Perspectiva", "Tercera persona para desplazamiento; primera persona para graffiti"],
            ["Ambientación", "Ciudad latinoamericana ficticia inspirada en Santiago, entre fines de los años noventa e inicios de los dos mil"],
            ["Motor", "Unity 6000.0.41f1, Universal Render Pipeline 17.0.4"],
            ["Plataforma objetivo", "PC/Windows; build independiente pendiente de cierre"],
            ["Público de referencia", "Personas interesadas en exploración urbana, expresión creativa, narrativa ambiental y cultura visual"],
            ["Estado al corte", "Vertical slice/prototipo avanzado con sistemas centrales implementados y validación parcial"],
            ["Fecha de corte", "6 de octubre de 2026"],
        ],
        widths=[4.0, 11.5],
        font_size=9.5,
    )
    add_figure(
        doc, IMAGES["menu"],
        "Figura 1. Menú principal de Flow State integrado al espacio tridimensional del proyecto. Fuente: captura del repositorio, 2026.",
        "Menú principal del videojuego Flow State con cuatro opciones y estética de graffiti sobre un escenario tridimensional oscuro.",
    )

    add_heading(doc, "4.- Marco Investigativo–Conceptual", 1, page_break_before=True)
    add_heading(doc, "4.1.- Problema de diseño: expresión, espacio público y consecuencia", 2)
    add_paragraphs(doc, """
        El punto de partida de Flow State no es reproducir el graffiti como tema superficial, sino comprenderlo como una práctica situada. Una marca en un muro implica selección de lugar, tiempo, visibilidad, técnica, riesgo y audiencia. Su significado cambia según el barrio, la propiedad del soporte, la historia del espacio, la relación con otras piezas y la respuesta de quienes transitan por allí. En términos de diseño, esto exige abandonar la lógica de “presionar un botón para completar un mural” y construir un sistema donde el proceso de intervenir sea legible.

        Jeff Ferrell, Nancy Macdonald y Joe Austin han estudiado el graffiti como cultura, identidad y disputa por la ciudad. Sus aproximaciones permiten observar que la práctica no se limita al resultado visual: se organiza mediante reputaciones, aprendizaje, estilos, recorridos y redes. Flow State recoge esa dimensión sin intentar convertirla en simulación documental. La ficción traduce el conflicto a variables jugables —reconocimiento, respeto de crew e impacto urbano— y las utiliza como señales de relaciones distintas. Ser reconocido no equivale necesariamente a ser respetado; producir impacto tampoco implica que ese impacto sea positivo.

        La campaña Ciudad Clara representa una forma institucional de ordenar lo visible. Su objetivo declarado es limpiar y recuperar, pero sus procedimientos borran memoria, simplifican identidades barriales y convierten la vigilancia en paisaje. El sistema narrativo busca que el jugador comprenda esa tensión a través del espacio: muros repintados, zonas controladas, rumores, accesos restringidos y reacciones de personajes. La ciudad funciona así como interfaz social. No explica todo mediante diálogos; modifica rutas, superficies, presencias y oportunidades.

        Esta posición conceptual también obliga a establecer límites. El juego no romantiza toda intervención ni supone que cualquier superficie debe ser ocupada. La decisión de pintar se vincula con contexto e intención. Una obra puede recuperar una historia borrada, apoyar a una comunidad, desafiar propaganda o ignorar las voces del lugar. La memoria entiende la responsabilidad como parte de la agencia: si el jugador puede expresarse, también debe poder observar el efecto de su expresión.
    """)

    add_heading(doc, "4.2.- Estado de flujo y diseño de la experiencia", 2)
    add_paragraphs(doc, """
        Csikszentmihalyi describe el flujo como una experiencia de concentración profunda asociada a metas claras, retroalimentación inmediata y equilibrio entre desafío y habilidad. En un videojuego, ese equilibrio no depende solo de la dificultad numérica. También surge de la continuidad entre percepción y acción: el jugador reconoce una oportunidad, ejecuta una secuencia y recibe información suficiente para corregirse sin abandonar la tarea. Flow State adopta el concepto como principio de coherencia más que como promesa psicológica universal.

        La locomoción aporta el primer nivel de continuidad. Correr, saltar, aprovechar un breve margen de salto, superar obstáculos, trepar, hacer top-out, ejecutar un wall jump o corregir la caída mediante un roll deben formar parte de un mismo vocabulario. Si cada acción exige una lógica totalmente distinta, el recorrido se fragmenta. Por eso el prototipo incorpora tolerancias como coyote time y buffer de salto, además de respuestas de cámara que anticipan velocidad y colisión. Esas ayudas no eliminan el desafío; reducen errores que el jugador percibiría como ajenos a su intención.

        El graffiti aporta un segundo nivel. El trazo manual entrega retroalimentación continua mediante pigmento, sonido, consumo de pintura, mezcla y comportamiento de la boquilla. La herramienta modifica su huella, pero mantiene una relación estable entre movimiento de entrada y marca visible. El sistema HSV permite explorar color sin depender de una lista cerrada, mientras que las cinco boquillas amplían densidad, ancho y textura. El resultado busca ser legible como acción propia incluso cuando la dirección artística aplica estilización posterior.

        La tensión aparece cuando el entorno interrumpe esa continuidad. Vigilancia, rutas inciertas, recursos que descienden y la necesidad de escapar obligan a decidir cuándo mantener el trazo y cuándo abandonar. El flujo no se trata entonces de ausencia de fricción, sino de una fricción comprensible. Una falla útil comunica su causa y permite aprender; una falla opaca rompe la relación entre habilidad y resultado. Esta distinción orienta las pruebas de control, cámara, interfaz y consumo.
    """)

    add_heading(doc, "4.3.- MDA: de reglas a significado", 2)
    add_paragraphs(doc, """
        El marco MDA de Hunicke, LeBlanc y Zubek distingue mecánicas, dinámicas y estéticas. Las mecánicas son reglas y componentes; las dinámicas emergen durante la interacción; las estéticas corresponden a las respuestas experienciales que el sistema intenta producir. La utilidad del modelo para Flow State consiste en evitar que una característica sea defendida únicamente porque “se siente temática”. Cada decisión debe conectar una regla observable con un comportamiento del jugador y con una intención emocional.

        Por ejemplo, el consumo de pintura y mezcla son mecánicas. Al pintar, la pintura desciende a una tasa mayor y la mezcla se agota tanto durante el uso como lentamente en reposo. Esas reglas generan la dinámica de preparar la lata, cambiarla, agitar y escoger cuánto tiempo permanecer expuesto. La estética buscada combina concentración, urgencia y apropiación material del instrumento. Si la tasa es demasiado alta, la experiencia se vuelve administración; si es irrelevante, desaparece la tensión que justifica el dispositivo.

        El parkour sigue la misma relación. Sus mecánicas incluyen saltos tolerantes, vaults, trepa, wall jump y roll. La dinámica deseada es leer la arquitectura como una red de posibilidades, encadenar movimientos y ajustar el recorrido bajo presión. La estética asociada es libertad corporal, dominio progresivo y ritmo. Una ciudad visualmente compleja pero sin rutas legibles no produce esa estética; del mismo modo, un conjunto amplio de movimientos sin riesgos o decisiones termina siendo exhibición sin propósito.

        La reputación evita condensar todos los resultados en una sola puntuación. Reconocimiento registra visibilidad; Crew Respect representa legitimidad entre pares; City Impact señala la huella sobre el territorio y sus habitantes. La separación permite resultados contradictorios: una acción muy visible puede dañar vínculos, y una intervención discreta puede tener alto valor comunitario. Esta estructura deja espacio para decisiones narrativas posteriores sin convertirlas en un medidor moral binario.
    """)
    add_table(
        doc,
        ["Sistema", "Mecánica", "Dinámica esperada", "Experiencia buscada"],
        [
            ["Graffiti", "Trazo manual, HSV, cinco boquillas, consumo", "Planificar el gesto y adaptarse a recursos", "Autoría, concentración, riesgo"],
            ["Parkour", "Vault, trepa, top-out, wall jump, roll", "Encadenar rutas y corregir bajo presión", "Fluidez, dominio, libertad"],
            ["Sigilo", "Lectura de vigilancia, posición y exposición", "Elegir momento, ruta y retirada", "Tensión legible"],
            ["Reputación", "Tres ejes persistentes", "Aceptar resultados no equivalentes", "Responsabilidad y pertenencia"],
            ["Dirección visual", "Estados emocionales del render", "Cambios perceptivos ligados al contexto", "Identidad, intensidad, memoria"],
        ],
        widths=[2.6, 4.3, 4.4, 4.2],
        font_size=8.7,
    )

    add_heading(doc, "4.4.- Legibilidad urbana, navegación y memoria", 2)
    add_paragraphs(doc, """
        Kevin Lynch plantea que la imagen de la ciudad se construye mediante sendas, bordes, barrios, nodos e hitos. Estas categorías son útiles para el diseño de niveles porque permiten organizar orientación sin depender por completo de marcadores superpuestos. Flow State utiliza la infraestructura urbana —pasajes, techos, cierres, estaciones, muros, equipamiento y publicidad— para producir rutas reconocibles. Un obstáculo puede ser simultáneamente borde visual, oportunidad de parkour y señal de control territorial.

        La propuesta de distritos semiabiertos busca equilibrar libertad y densidad. Un mundo demasiado amplio diluiría la relación entre piezas, personajes y consecuencias; un corredor cerrado impediría experimentar con rutas. Cada distrito debe poder leerse en capas: circulación cotidiana, trayecto rápido, acceso oculto y superficie significativa. La repetición de un lugar después de una intervención permite comprobar cambios, reforzando la memoria espacial.

        La cámara y el HUD apoyan esa lectura, pero no deben reemplazarla. La cámara en tercera persona utiliza colisión y variación de campo visual para acompañar velocidad y evitar obstrucciones. Durante la pintura se adopta primera persona para aumentar precisión y proximidad material. El HUD muestra pintura y mezcla, especialmente en estados críticos, mientras la mayor parte de la orientación queda en el entorno. Esta distribución intenta mantener información suficiente sin convertir la pantalla en un panel de administración.

        La teletransportación por alcantarillas, presente como prototipo, demuestra una transición entre puntos urbanos con fundidos temporizados. Su existencia no reemplaza el recorrido principal; funciona como infraestructura potencial para desplazamiento entre zonas ya conocidas. La regla de diseño es que un atajo debe responder a aprendizaje y progresión, no eliminar la experiencia de recorrer la ciudad antes de comprenderla.
    """)

    add_heading(doc, "4.5.- Autoría, herramienta y materialidad digital", 2)
    add_paragraphs(doc, """
        En muchos videojuegos, el graffiti aparece como textura precompuesta que se activa al alcanzar un punto. Esa solución garantiza calidad visual, pero limita autoría. Flow State combina dos modos: pintura libre y decals. La pintura libre registra el gesto del jugador sobre superficies; los decals permiten incorporar piezas diseñadas con mayor control. La coexistencia evita una falsa oposición entre libertad total y composición dirigida.

        La interfaz Neo Spray Lab hace visible la herramienta. Color, boquilla, pintura y mezcla no se configuran en un menú externo completamente abstracto, sino en un dispositivo coherente con el universo visual. Esta decisión fortalece la relación entre sistema y ficción. También obliga a mantener claridad: una interfaz diegética puede ser atractiva y aun así fracasar si sus estados no se comprenden. Las pruebas se concentran por ello en navegación, persistencia y lectura de niveles críticos.

        El límite de doce mil estampas por superficie establece un presupuesto explícito para el trazo. No define por sí solo el rendimiento final, pero evita crecimiento ilimitado de datos durante sesiones prolongadas. El diseño de recursos también reconoce su condición provisional: el suministro de latas es infinito en el estado actual y el audio del spray está silenciado por defecto. En consecuencia, la economía completa todavía no puede evaluarse como sistema de progresión.
    """)
    add_figure(
        doc, IMAGES["graffiti_menu"],
        "Figura 2. Neo Spray Lab: interfaz diegética de configuración de aerosol, color y recursos. Fuente: captura del repositorio, 2026.",
        "Interfaz de aerosol con lata, selector cromático y medidores de recursos dentro de la estética industrial de Flow State.",
    )
    add_figure(
        doc, IMAGES["nozzles"],
        "Figura 3. Comparación de las cinco boquillas implementadas y sus huellas. Fuente: captura del repositorio, 2026.",
        "Cinco perfiles de boquilla de aerosol comparados mediante trazos de diferente ancho, densidad y dispersión.",
        max_height_cm=7.7,
    )

    add_heading(doc, "4.6.- Dirección artística y estados emocionales", 2)
    add_paragraphs(doc, """
        La dirección High-Fi Low Poly combina geometría controlada con superficies, iluminación y posprocesado de alta intención gráfica. La meta no es imitar una fotografía, sino construir una ciudad táctil y reconocible mediante siluetas claras, materiales legibles y capas de tinta. La base cromática utiliza grises, azules e industriales; el graffiti introduce saturación y contraste. Esta jerarquía hace que las intervenciones destaquen sin separar el arte del entorno.

        El sistema Flow State Stylized Rendering System (FSSRS) organiza contornos, posterización, tinta, tramado, hatching, paleta y elevación de papel en una sola composición de pantalla completa. La UI se procesa fuera del pase para mantener legibilidad. La arquitectura del efecto importa tanto como su apariencia: consolidar operaciones evita cadenas innecesarias de blits y permite que los estados emocionales compartan una base técnica.

        Neutral, Doubt, Anger, Creative Flow y Clarity son estados visuales que modifican la lectura de la escena. No deben funcionar como filtros decorativos arbitrarios. Cada uno necesita una motivación narrativa o jugable: la duda puede reducir contraste y color; la ira intensificar placas y bordes; el flujo creativo amplificar separación cromática y energía; la claridad estabilizar valores. La revisión artística actual desactiva motas y puntos de semitono cuando compiten con la composición, demostrando que la identidad no depende de activar todos los efectos disponibles.

        La separación entre LookDev y producción protege materiales originales. La escena de exploración visual utiliza un renderer diferente y duplicados controlados, mientras los materiales fuente permanecen disponibles. Esta práctica permite experimentar con estados extremos sin convertir una prueba de arte en un cambio irreversible para el juego.
    """)
    add_figure(
        doc, IMAGES["fssrs_flow"],
        "Figura 4. Estado Creative Flow del sistema FSSRS, con separación cromática y energía gráfica ampliadas. Fuente: captura del repositorio, 2026.",
        "Escena urbana estilizada por el estado Creative Flow con contornos, tinta y colores intensos.",
    )

    add_heading(doc, "4.7.- Criterios de responsabilidad y representación", 2)
    add_paragraphs(doc, """
        La inspiración en Santiago y en culturas reales introduce una responsabilidad de representación. La ciudad del juego es ficticia para evitar atribuir hechos inventados a instituciones o comunidades específicas, pero conserva referencias materiales reconocibles: densidad, infraestructura, contraste entre planificación y apropiación, capas de propaganda y muralismo. La ficción debe servir para examinar relaciones, no para usar identidades locales como decoración exótica.

        El Museo a Cielo Abierto de San Miguel funciona como antecedente significativo porque evidencia cómo el mural puede articular patrimonio, participación vecinal y recorrido urbano. No es tratado como equivalente al graffiti clandestino; su inclusión permite contrastar formas de producción, autorización, memoria y cuidado. Esa diferencia alimenta misiones posibles donde la legalidad, la legitimidad comunitaria y el riesgo no coinciden automáticamente.

        Para una versión posterior, la evaluación debe incorporar personas vinculadas a arte urbano y comunidades representadas, con consentimiento sobre citas e imágenes. La memoria actual se limita a fuentes publicadas y evidencia interna del desarrollo. No inventa testimonios ni presenta pruebas de usuario que no hayan ocurrido.
    """)

    add_heading(doc, "4.8.- Marco técnico y trazabilidad", 2)
    add_paragraphs(doc, """
        El proyecto se desarrolla en Unity 6000.0.41f1 con Universal Render Pipeline 17.0.4, Input System 1.13.1, Cinemachine 3.1.7, Timeline 1.8.7 y Test Framework 1.4.6. La elección responde a la necesidad de integrar control en tercera persona, shaders personalizados, interfaces, secuencias y pruebas automatizadas dentro de un mismo entorno. La versión exacta se registra para reducir ambigüedad al reproducir resultados.

        La arquitectura separa sistemas principales por responsabilidad. Movimiento, cámara, graffiti, menú, reputación, música y render estilizado tienen componentes específicos. Esta separación no significa independencia absoluta, pero reduce la posibilidad de que un efecto visual controle una transición o que una interfaz escriba directamente la pose de cámara. Los informes de fase documentan, por ejemplo, que el controlador central es el único escritor de la cámara del menú y que Timeline solicita una pose sin manipular directamente el transform principal.

        La trazabilidad se sostiene con control de versiones, documentación en el repositorio, capturas de hitos y resultados XML de pruebas. La memoria utiliza ese material para reconstruir el proceso. Cuando una cifra proviene del Editor —como draw calls de una escena de prueba— se identifica como observación local y no como rendimiento de build. Esta diferencia es esencial: precisión documental no equivale a acumular números, sino a expresar qué midió cada número y bajo qué condiciones.
    """)

    add_heading(doc, "5.- Estado del Arte", 1, page_break_before=True)
    add_heading(doc, "5.1.- Graffiti, movimiento y ciudad como espectáculo", 2)
    add_paragraphs(doc, """
        Jet Set Radio estableció una relación influyente entre desplazamiento, música, estilo gráfico y ocupación de la ciudad. El jugador recorre Tokio-to sobre patines, marca espacios y evade autoridades dentro de una identidad audiovisual fuertemente cohesionada. Su aporte a Flow State no es una mecánica aislada, sino la demostración de que movilidad, sonido y gráfica pueden construir una fantasía urbana unificada. La diferencia principal es el énfasis de Flow State en el trazo manual, el contexto de cada superficie y consecuencias sociales diferenciadas.

        Bomb Rush Cyberfunk retoma esa herencia con una ciudad tridimensional, crews, reputación y múltiples modos de desplazamiento. Su escala, legibilidad y energía ofrecen referencias útiles para rutas y para la forma en que un lenguaje visual puede guiar al jugador. Flow State se distancia al reducir el carácter puramente celebratorio de la conquista territorial y al situar la intervención dentro de un conflicto latinoamericano específico.
    """)

    add_heading(doc, "5.2.- Graffiti como narrativa y confrontación", 2)
    add_paragraphs(doc, """
        Marc Ecko’s Getting Up: Contents Under Pressure convierte el ascenso de un escritor de graffiti en una campaña de acción, infiltración y confrontación política. Su relevancia está en vincular piezas, riesgo y progresión narrativa. También expone una tensión del género: cuando pintar se resuelve mediante eventos simplificados o coleccionables, el resultado visual puede dominar sobre el proceso expresivo. Flow State responde manteniendo pintura libre como sistema central y reservando composiciones más controladas para decals o momentos narrativos.

        La obra sirve además como advertencia sobre el tono. Una narrativa de resistencia puede perder matices si todas las instituciones son intercambiables y toda intervención se considera automáticamente liberadora. Ciudad Clara está diseñada como fuerza antagonista, pero las consecuencias de Maik y las diferencias entre Nadir, Vela y otras agrupaciones impiden que la legitimidad se determine solo por oposición.
    """)

    add_heading(doc, "5.3.- Fotografía, crítica y lectura del entorno", 2)
    add_paragraphs(doc, """
        Umurangi Generation utiliza la fotografía para enseñar a observar una ciudad saturada por crisis, publicidad, vigilancia y presencia militar. Su sistema demuestra que una herramienta creativa puede ser simultáneamente mecánica de puntuación, forma de encuadre y vehículo crítico. La acción de mirar no es neutral: escoger qué queda dentro de la imagen produce una postura. Flow State traslada ese principio a la pintura. La superficie elegida, el contenido y la ruta de acceso deben revelar cómo Maik lee el barrio.

        La comparación resulta especialmente útil porque ambos proyectos trabajan con instrumentos expresivos. La cámara de Umurangi limita y orienta la percepción; el aerosol de Flow State materializa una marca persistente. En ambos casos, la interfaz debe apoyar autoría sin ocultar el mundo detrás de paneles. El Neo Spray Lab y el HUD de recursos intentan sostener esa relación material.
    """)

    add_heading(doc, "5.4.- Sigilo, sistemas y rutas", 2)
    add_paragraphs(doc, """
        Thief: The Dark Project es un referente para el sigilo basado en información espacial. Luz, sonido, superficies y rutas producen decisiones antes del encuentro directo. Flow State no replica su simulación, pero recoge la idea de que el sigilo se comprende a través del ambiente y no únicamente mediante conos visibles. La lectura de vigilancia, cobertura y salida debe comenzar antes de pintar.

        Esta influencia afecta el diseño del fracaso. Si el jugador es detectado, la respuesta debe reconocer posición y conducta, no parecer aleatoria. La combinación con parkour permite que la retirada sea una fase expresiva del ciclo y no un corte automático. En el prototipo todavía falta una IA de persecución completa; por eso el referente se emplea como criterio para continuar, no como equivalencia ya alcanzada.
    """)

    add_heading(doc, "5.5.- Vida cotidiana, vínculos y construcción de identidad", 2)
    add_paragraphs(doc, """
        Persona 5 ofrece una referencia de identidad gráfica, ritmo de interfaz y relación entre vida cotidiana y conflicto extraordinario. Night in the Woods muestra cómo conversaciones, repetición de lugares y cambios pequeños pueden construir una comunidad compleja. Flow State toma de estas obras la importancia del intervalo: no todo momento debe ser persecución o pintura. La pertenencia se construye al volver a espacios, hablar con personas y reconocer cómo una acción altera la convivencia.

        La propuesta evita copiar estructuras de calendario o combate por turnos. Lo relevante es la coordinación entre tono, interfaz y arco personal. Maik no es una figura sin pasado; su retorno desde Nueva York, la presión familiar y el reencuentro con una ciudad transformada condicionan sus decisiones. La progresión debe mostrar cambios en relaciones, no solo aumentar capacidades.
    """)

    add_heading(doc, "5.6.- Referente territorial: Museo a Cielo Abierto de San Miguel", 2)
    add_paragraphs(doc, """
        El Museo a Cielo Abierto de San Miguel constituye un referente territorial para comprender el mural como recorrido, memoria barrial y obra situada. Su registro institucional y difusión turística describen un conjunto de murales articulado con el territorio y la comunidad. Para Flow State, su valor no está en reproducir piezas específicas, sino en recordar que la imagen urbana puede ser parte de un tejido social y no un fondo intercambiable.

        Este antecedente ayuda a diseñar contrastes entre mural autorizado, encargo comunitario, firma individual, propaganda y borrado. Cada categoría produce condiciones distintas de acceso, audiencia y cuidado. La ciudad ficticia puede explorar esas diferencias sin utilizar imágenes reales sin autorización.
    """)

    add_heading(doc, "5.7.- Síntesis comparativa y oportunidad de diseño", 2)
    add_table(
        doc,
        ["Referente", "Aporte observado", "Decisión propia de Flow State"],
        [
            ["Jet Set Radio", "Unidad entre movimiento, música, ciudad y gráfica", "Desplazamiento sin patines y pintura manual con consecuencias"],
            ["Bomb Rush Cyberfunk", "Rutas urbanas, crews, energía visual", "Escala más contenida y conflicto situado en una ciudad latinoamericana ficticia"],
            ["Getting Up", "Graffiti ligado a campaña y confrontación", "Mayor autoría del trazo y legitimidades no binarias"],
            ["Umurangi Generation", "Herramienta creativa como forma de lectura crítica", "El aerosol deja una marca persistente y social"],
            ["Thief", "Sigilo informado por ambiente y rutas", "Preparación, pintura y escape como un ciclo continuo"],
            ["Persona 5 / Night in the Woods", "Interfaz con identidad y vínculos cotidianos", "Reputación distribuida y retorno a barrios transformados"],
            ["Museo a Cielo Abierto", "Mural, memoria y territorio", "Contraste entre encargo, comunidad, propaganda y práctica no autorizada"],
        ],
        widths=[3.2, 6.1, 6.2],
        font_size=8.7,
    )
    add_paragraphs(doc, """
        El espacio de oportunidad aparece en la intersección de estos referentes. Existen juegos centrados en movilidad urbana, otros en graffiti, otros en sigilo y otros en herramientas creativas; Flow State busca que esas dimensiones dependan unas de otras. La ruta importa porque conduce a una superficie; la superficie importa porque contiene historia; pintar importa porque produce una reacción; escapar importa porque confirma el riesgo asumido.

        La diferenciación no se declara únicamente por ambientación. Debe probarse mediante sistemas y contenido. El prototipo ya valida control, herramienta, dirección visual e interfaces, pero necesita una misión completa en un distrito coherente para demostrar la promesa en conjunto. Esa misión constituye el siguiente hito crítico de producción.

        Desde la perspectiva de producción, el análisis comparativo también define qué no debe priorizarse. Competir por tamaño de mundo, cantidad de trucos o número de piezas diluiría los recursos disponibles. El valor diferencial depende de la calidad de la relación entre superficie, recorrido y respuesta. Por eso el referente no funciona como una lista de funciones a reproducir, sino como un conjunto de preguntas: qué información entrega el entorno, cuánto control tiene el jugador sobre la marca, cómo cambia el barrio y qué parte de la experiencia puede verificarse en una misión breve. Esta lectura transforma el estado del arte en criterio de alcance y no solo en justificación estética.
    """)
    add_figure(
        doc, IMAGES["contact"],
        "Figura 5. Hoja de contacto de la pieza cinematográfica V6, utilizada para evaluar continuidad visual y estados FSSRS. Fuente: captura del repositorio, 2026.",
        "Hoja de contacto con distintos encuadres de una pieza cinematográfica urbana de Flow State.",
    )

    add_heading(doc, "6.- Bitácora de Proyecto", 1, page_break_before=True)
    add_paragraphs(doc, """
        La bitácora organiza el desarrollo en preproducción, producción y postproducción. Las fechas se reconstruyen a partir del historial de Git, documentos de fase y evidencia del repositorio. No representan dedicación diaria completa, sino hitos verificables. La clasificación por etapas es analítica: varias actividades se solaparon, especialmente programación, arte y pruebas.
    """)

    add_heading(doc, "6.1.- Pre-Producción", 2)
    add_heading(doc, "6.1.1.- Conceptualización de la Propuesta", 3)
    add_paragraphs(doc, """
        La propuesta inicial se definió alrededor de tres verbos: recorrer, pintar y responder. Recorrer debía convertir la ciudad en un espacio aprendido; pintar debía permitir una huella propia; responder debía reconocer consecuencias narrativas y sistémicas. Esta formulación evitó tratar el graffiti como un minijuego desconectado y permitió evaluar nuevas ideas según su aporte al ciclo central.

        Maik fue concebido como un protagonista entre contextos. Haber crecido parcialmente en Estados Unidos le entrega experiencia y distancia, pero no autoridad automática sobre la ciudad a la que regresa. Su historia permite explorar traducción cultural, nostalgia, pertenencia incompleta y responsabilidad. La campaña Ciudad Clara concentra el conflicto visible, mientras Nadir y Vela representan posiciones que impiden una lectura única de la escena.

        El alcance se redujo a un vertical slice capaz de demostrar identidad, control, pintura, presentación y base narrativa. Se descartó la idea de construir de inmediato una ciudad completa. El criterio fue que un distrito denso con una misión significativa ofrece mejor evidencia que múltiples sectores sin consecuencias ni acabado.
    """)
    add_callout(
        doc,
        "Pregunta rectora",
        "¿Cómo convertir el acto de pintar la ciudad en una decisión expresiva, corporal y social cuya ejecución y consecuencias sean legibles para el jugador?",
    )

    add_heading(doc, "6.1.2.- Referentes", 3)
    add_paragraphs(doc, """
        La investigación se dividió en referentes lúdicos, visuales, territoriales y teóricos. Los juegos aportaron soluciones de movimiento, lectura espacial, interfaz y narrativa. Los referentes urbanos permitieron discutir memoria, muralismo, señalética, infraestructura y vigilancia. Csikszentmihalyi, MDA y Lynch entregaron conceptos operativos para evaluar continuidad, relación entre reglas y experiencia, y legibilidad de la ciudad.

        El uso de referentes siguió una regla de transformación: identificar qué problema resuelve cada obra antes de adoptar su forma. Por ejemplo, la energía visual de Jet Set Radio no se tradujo en copiar cel shading; se convirtió en la necesidad de una relación coherente entre movimiento, música y color. El Museo a Cielo Abierto no se convirtió en un escenario literal; ayudó a distinguir pieza, territorio y comunidad.
    """)

    add_heading(doc, "6.1.3.- Experiencia Mecánica", 3)
    add_paragraphs(doc, """
        La experiencia mecánica se organizó en capas. La locomoción básica incluye caminar, correr, sprint y salto con tolerancias. El parkour agrega vault bajo y medio, trepa continua, top-out, wall jump y roll aéreo. La cámara acompaña velocidad, evita colisiones y cambia de modalidad durante la pintura. El combate cuenta con un prototipo de fijación de objetivo hasta dieciocho metros, mantenido hasta veinticuatro; queda bloqueado mientras se usa graffiti o el dispositivo musical para evitar conflictos de entrada.

        El graffiti registra trazos libres y decals. El color se selecciona en HSV y cinco boquillas modifican el resultado. Pintar consume aproximadamente cuatro unidades de pintura por segundo y 7,5 de mezcla; la mezcla también disminuye lentamente en reposo. Agitar con R y cambiar lata con F completan la interacción provisional. El HUD comunica ambos recursos y sus estados críticos.

        Otros prototipos apoyan el recorrido: teletransporte por alcantarillas con fundido, dispositivo musical diegético con tempo inicial de 92 BPM y reputación persistente en tres ejes. Estas piezas no tienen el mismo nivel de cierre que pintura y movimiento, pero prueban cómo el sistema central puede conectarse con navegación, ritmo y consecuencia.
    """)
    add_figure(
        doc, IMAGES["parkour"],
        "Figura 6. Curso de validación de parkour utilizado para revisar vault, trepa y continuidad del movimiento. Fuente: captura del repositorio, 2026.",
        "Personaje de Flow State dentro de un curso gris de obstáculos utilizado para probar movimientos de parkour.",
    )
    add_figure(
        doc, IMAGES["camera"],
        "Figura 7. Validación de cámara y locomoción en tercera persona. Fuente: captura del repositorio, 2026.",
        "Vista en tercera persona del personaje en un entorno urbano de prueba.",
    )

    add_heading(doc, "6.1.4.- Experiencia Dinámica", 3)
    add_paragraphs(doc, """
        La dinámica central comienza antes del trazo. El jugador observa patrones, identifica accesos y compara superficies. La ruta más rápida puede ser más visible; una zona segura puede limitar el tamaño de la pieza; una intervención de alto impacto puede exigir volver por un trayecto difícil. Al unir estas variables, el recorrido deja de ser traslado y se convierte en planificación.

        Durante la pintura, la atención se estrecha. La primera persona, el consumo y la huella inmediata favorecen concentración, mientras el contexto mantiene presión. Después aparece el escape, que reutiliza el conocimiento adquirido durante la entrada. Finalmente, el retorno al barrio permite leer reacción y persistencia. La dinámica completa depende de que cada fase informe la siguiente.

        Los tres ejes de reputación permiten que el sistema responda sin una evaluación moral única. Una pieza grande en un lugar visible puede aumentar reconocimiento, molestar a una crew o tener poco efecto comunitario. La implementación actual persiste los valores, pero la red completa de misiones y reacciones todavía pertenece al diseño de continuidad.
    """)

    add_heading(doc, "6.1.5.- Experiencia Estética", 3)
    add_paragraphs(doc, """
        La experiencia estética busca tensión entre ciudad contenida y color insurgente. Volúmenes low poly, materiales industriales y una paleta fría forman la base. El graffiti, los elementos de interfaz y los estados emocionales introducen rojo, cian, magenta, amarillo ácido y coral. Las siluetas se mantienen legibles antes de aplicar textura, de modo que el estilo no dependa exclusivamente del posprocesado.

        El sonido se concibe como parte del gesto: ataque de lata, aerosol continuo, superficie, pasos, impacto y capas musicales reactivas. En el estado actual existen placeholders y un dispositivo musical, pero el audio de spray está silenciado por defecto y no puede considerarse mezcla final. La memoria conserva la intención estética y registra la diferencia con el estado implementado.
    """)
    add_figure(
        doc, IMAGES["fssrs_mono"],
        "Figura 8. Prueba monocromática del sistema FSSRS para revisar jerarquía, contornos y lectura sin depender del color. Fuente: captura del repositorio, 2026.",
        "Escena urbana de Flow State renderizada en blanco, negro y grises para evaluar jerarquía visual.",
    )

    add_heading(doc, "6.2.- Producción", 2)
    add_heading(doc, "6.2.1.- Desarrollo de la Propuesta", 3)
    add_paragraphs(doc, """
        La producción comenzó con estructura de proyecto, assets y nivel, y avanzó mediante prototipos sucesivos. Cada hito agregó una capacidad o consolidó una anterior. La tabla resume los puntos visibles en el historial, sin atribuir a cada fecha todo el trabajo intermedio.
    """)
    add_table(
        doc,
        ["Fecha", "Hito verificable", "Aprendizaje o decisión"],
        [
            ["17 ago. 2026", "Inicio del repositorio, assets y nivel", "Establecer estructura antes de ampliar contenido"],
            ["18–19 ago.", "Primer sistema de graffiti y UI", "El trazo debía ser una herramienta, no un trigger"],
            ["27–28 ago.", "Blocking y escala", "Probar distancias y lectura antes del detalle"],
            ["31 ago.", "Trabajo de shader", "La identidad visual requería una arquitectura propia"],
            ["1–2 sep.", "Parkour y revisión de shader", "Movimiento y arte debían evaluarse en conjunto"],
            ["8 sep.", "Primer menú principal", "La presentación debía compartir el lenguaje del juego"],
            ["15–16 sep.", "Blocking y revisión de calidad", "Reducir dispersión y ordenar rutas"],
            ["21 sep.", "Fundaciones y prototipo de intro del menú", "Separar coordinación, cámara, pintura y contenido"],
            ["23 sep.", "Mejoras de UI / Neo Spray Lab", "Hacer legible la herramienta y sus estados"],
            ["27 sep.", "Escenario y cajas", "Aumentar soporte material para navegación"],
            ["30 sep.", "Reputación y trabajo de entorno", "Conectar acciones con persistencia"],
            ["1 oct.", "Animación de tráiler", "Consolidar una pieza de presentación verificable"],
            ["2–4 oct.", "UI, entorno y blocking actualizado", "Cerrar coherencia antes del build"],
            ["5 oct.", "Preparación de build", "Detectar que el empaquetado y smoke test aún faltaban"],
        ],
        widths=[2.7, 5.5, 7.3],
        font_size=8.4,
    )

    add_heading(doc, "6.2.2.- Programación", 3)
    add_paragraphs(doc, """
        La base técnica reúne 83 archivos C# y aproximadamente 13.065 líneas, de las cuales 39 scripts se ubican en la carpeta principal de runtime, 26 pertenecen al menú y 12 al sistema FSSRS. La cifra describe escala, no calidad por sí sola. Su utilidad está en mostrar que el prototipo dejó de ser una prueba monolítica y requiere documentación, pruebas y límites de responsabilidad.

        En movimiento se implementaron locomoción, salto, tolerancias y acciones de parkour. La cámara resuelve seguimiento, campo visual y colisión. En graffiti se separan entrada, pintura, herramientas, recursos y HUD. La superficie limita estampas para impedir acumulación indefinida. El cambio a primera persona se bloquea frente a modos incompatibles, y el sistema de lock-on también respeta graffiti y música.

        El menú principal se construyó como escena viva con JUGAR, GALERÍA, CONFIG y SALIR. Acepta teclado, mouse y gamepad; incluye opción de movimiento reducido y una transición de 1,05 segundos hacia Gameplay. La cámara reactiva centraliza la escritura de pose. Una regresión reveló que una preferencia antigua con valor cero anulaba el movimiento aunque la UI indicaba REDUCIDO; la migración corrigió el valor a una respuesta reducida real y agregó pruebas específicas.

        La intro visual utiliza seis ventanas verticales alimentadas por una sola cámara de contenido, una textura compartida y dos máscaras ping-pong. Timeline entrega progreso y el director coordina recursos, pero no escribe directamente la cámara principal. La secuencia Full dura 5,10 segundos en la revisión actual y permanece desactivada por defecto. Esta decisión permite conservar el menú estable mientras audio y handoff final siguen pendientes.

        La persistencia de reputación utiliza PlayerPrefs para Recognition, CrewRespect y CityImpact. Es adecuada para el prototipo, aunque una producción con múltiples partidas requeriría versionado, migración y pruebas entre procesos. El sistema de teletransporte usa fundidos de 0,3 segundos, una pausa negra de 0,12 y retorno de 0,4, demostrando coordinación de estado antes de ampliar la red de destinos.
    """)
    add_figure(
        doc, IMAGES["intro"],
        "Figura 9. Prototipo de intro: seis ventanas verticales revelan una única fuente tridimensional mediante máscaras compartidas. Fuente: captura del repositorio, 2026.",
        "Seis paneles verticales irregulares muestran fragmentos de un diorama oscuro dentro de la intro de Flow State.",
    )

    add_heading(doc, "6.2.3.- Arte", 3)
    add_paragraphs(doc, """
        La producción visual combina blocking, materiales, shaders, animación y composición. El repositorio contiene siete escenas, cinco prefabs, 156 materiales, 29 shaders, cuatro Shader Graphs, 27 animaciones y tres controladores. La variedad exige distinguir recursos de exploración, producción y presentación. Las pruebas FSSRS se mantienen en un renderer separado cuando corresponde, y los materiales originales se preservan.

        FSSRS ejecuta contornos, posterización, tinta, hatching, paleta y papel dentro de una composición de pantalla completa. Esta organización mantiene la UI fuera del tratamiento para evitar pérdida de lectura. Los estados emocionales se probaron en imágenes extremas y monocromáticas; después se redujeron flecks y puntos de semitono cuando distraían. La iteración demuestra que una dirección estilizada se beneficia de eliminar elementos tanto como de agregarlos.

        El menú y la intro adoptan el mismo lenguaje de tinta, recortes y graffiti. La intro no muestra el logo en su revisión actual: revela seis ventanas, sostiene la composición y realiza un pequeño empuje de cámara. Esta ausencia es deliberada y está documentada, evitando confundir un asset referenciado con un elemento efectivamente presentado.
    """)

    add_heading(doc, "6.2.4.- Narrativa", 3)
    add_paragraphs(doc, """
        La narrativa se articula alrededor del retorno de Maik y su lectura de una ciudad transformada. El protagonista debe reconstruir lazos y decidir qué significa dejar una marca. Nadir y Vela introducen perspectivas distintas sobre visibilidad, comunidad y confrontación. Ciudad Clara controla imagen, vigilancia y discurso, convirtiendo el borrado en una acción narrativa visible.

        El diseño de misiones proyectado vincula intención y espacio. Antes de intervenir, el jugador recibe información parcial mediante conversaciones, rumores y observación. La elección de superficie y contenido produce reacciones en los tres ejes de reputación. La historia no está implementada como campaña completa; existen premisa, arco, facciones y sistemas de soporte. El siguiente paso narrativo verificable es una misión vertical que incluya preparación, pintura, escape y retorno.
    """)

    add_heading(doc, "6.2.5.- Sonido", 3)
    add_paragraphs(doc, """
        El sonido se diseñó en tres capas. La primera comunica acción: salto, aterrizaje, vault, spray, agitación y cambio de lata. La segunda informa riesgo y espacio: vigilancia, distancia, materiales y presencia urbana. La tercera organiza tono mediante música y estados emocionales. El dispositivo diegético de música inicia en 92 BPM y admite control mediante M, P, teclas 1–4 y flechas verticales en la versión de desarrollo.

        El menú reutiliza clips placeholder para ataque y loop de aerosol durante la intro. La conducta visual no depende del volumen, lo que permite probar la secuencia aun con audio desactivado. Sin embargo, el spray general permanece silenciado por defecto y no existe una mezcla final. El hito de sonido debe incluir biblioteca autorizada, niveles, transiciones, opciones de accesibilidad y prueba en build.
    """)
    add_figure(
        doc, IMAGES["music"],
        "Figura 10. Prototipo de dispositivo musical diegético y control de tempo. Fuente: captura del repositorio, 2026.",
        "Interfaz de dispositivo musical dentro del mundo de Flow State con controles de reproducción y tempo.",
    )

    add_heading(doc, "6.2.6.- Build", 3)
    add_paragraphs(doc, """
        Build Settings contiene dos escenas principales: MainMenu_FlowState y Gameplay, en ese orden. La transición desde JUGAR carga Gameplay durante las pruebas. Esta configuración demuestra el flujo interno del proyecto, pero no equivale a un ejecutable distribuible.

        Al corte de esta memoria no existe evidencia de un build Windows empaquetado y sometido a smoke test. Tampoco se ha verificado persistencia entre procesos, instalación en un equipo limpio, comportamiento con control físico durante una sesión prolongada ni rendimiento fuera del Editor. La preparación de build del 5 de octubre debe entenderse como hito de organización, no como certificación final.
    """)
    add_callout(
        doc,
        "Estado verificable",
        "El prototipo carga sus dos escenas dentro del entorno de desarrollo. La memoria no declara un build final hasta generar un ejecutable, ejecutarlo en hardware objetivo y registrar el smoke test.",
    )

    add_heading(doc, "6.3.- Post Producción", 2)
    add_heading(doc, "6.3.1.- VFX", 3)
    add_paragraphs(doc, """
        Los efectos visuales se concentran en aerosol, tinta, bordes, estados FSSRS, transiciones y presentación cinematográfica. El sistema de spray debe comunicar densidad y contacto sin ocultar el trazo. FSSRS se aplica después de la geometría y antes de UI, mientras graffiti y transparencias mantienen un orden definido. Esta secuencia evita que el tratamiento de pantalla reduzca la legibilidad de controles.

        La pieza cinematográfica FlowState_Trailer_25s_98s.playable produjo una versión V6 de 73 segundos, H.264, 1920 × 1080, 30 fps y audio AAC, validada el 1 de octubre. Funciona como material de prototipo y presentación; no es un máster final. Su principal valor para producción es comprobar encuadres, pose, estados del render y continuidad visual.
    """)

    add_heading(doc, "6.3.2.- Depurado y Optimización", 3)
    add_paragraphs(doc, """
        El depurado se apoyó en Play Mode, Edit Mode, capturas y revisión de referencias. Se corrigieron problemas de cursor, fuentes de entrada, preferencias antiguas y escritores de cámara. La intro libera texturas temporales al finalizar o saltarse. Sus recursos propios activos corresponden a dos máscaras de 1024 × 576 y una textura de contenido de la misma resolución; durante la fase documentada se estimó un presupuesto aproximado de 8,44 MiB incluyendo profundidad. Esa cifra no representa memoria total del juego.

        El menú utiliza dos RenderTextures de 2048 × 1024, con un coste de color aproximado de 16 MiB sin considerar sobrecarga. El número de objetos se mantiene fijo y el máximo de partículas está acotado. La superficie de graffiti limita estampas a doce mil. Estas decisiones previenen crecimiento evidente, pero todavía falta perfilar CPU, GPU, memoria y tiempos de frame en build.

        Una lectura del Editor durante la intro Full mostró 95 draw calls, 95 batches, 18.389 triángulos, 14.859 vértices y 48 RenderTextures globales. Debido a que incluye Editor y URP completo, se conserva solo como referencia local. La optimización final necesita un escenario objetivo, hardware definido, captura reproducible y presupuesto por sistema.
    """)

    add_heading(doc, "6.3.3.- Test Q&A", 3)
    add_paragraphs(doc, """
        Las pruebas automatizadas se concentraron en los sistemas de mayor riesgo de regresión. El 21 de septiembre, la base de menú e intro registró 19/19 pruebas Play Mode y 7/7 Edit Mode en su revisión conjunta. La intro tuvo 4/4 pruebas específicas y la regresión de movimiento reducido, mouse y stick derecho obtuvo 3/3. Las pruebas cubrieron entrada única, skip, navegación, retorno de cámara, separación de stick derecho, determinismo de máscaras, liberación de recursos y carga de Gameplay.

        La revisión de Neo Spray Lab del 23 de septiembre registró siete pruebas Edit Mode y diecinueve Play Mode consideradas en conjunto con el menú. Una prueba de migración mostró dependencia del orden dentro de la corrida combinada, aunque pasó de forma aislada. Este resultado no debe ocultarse: señala aislamiento incompleto y requiere corregir setup/teardown antes de interpretar la suite combinada como totalmente estable.

        No existe una suite dedicada de regresión para parkour, prueba prolongada con mando físico, benchmark formal ni integración continua. La cobertura automatizada demuestra comportamiento de componentes y flujos concretos; no reemplaza evaluación de sensación, accesibilidad, comprensión narrativa ni estabilidad de una sesión completa.
    """)
    add_table(
        doc,
        ["Área", "Evidencia disponible", "Resultado", "Pendiente"],
        [
            ["Menú / intro", "Play Mode y Edit Mode, 21 sep.", "19/19 y 7/7 en revisión conjunta", "Prueba física prolongada"],
            ["Intro específica", "4 pruebas Play Mode", "Determinismo, recursos y handoff validados", "Audio y activación final"],
            ["Movimiento reducido", "3 pruebas de regresión", "Preferencia, mouse y stick corregidos", "Accesibilidad con usuarios"],
            ["Neo Spray Lab", "7 Edit Mode; corrida combinada de Play Mode", "Funciones centrales verificadas; una migración depende del orden", "Aislar estado entre pruebas"],
            ["Parkour", "Revisión manual y capturas", "Movimientos implementados", "Suite dedicada y métricas"],
            ["Build", "Carga interna de escenas", "Flujo Editor operativo", "Ejecutable, smoke test y perfil"],
        ],
        widths=[2.5, 4.4, 5.0, 3.6],
        font_size=8.3,
    )
    add_figure(
        doc, IMAGES["hud"],
        "Figura 11. HUD de aerosol con lectura de pintura, mezcla y estados críticos. Fuente: captura del repositorio, 2026.",
        "Interfaz de juego que muestra recursos de pintura y mezcla durante el uso del aerosol.",
    )
    add_figure(
        doc, IMAGES["combat"],
        "Figura 12. Prototipo de lock-on y tratamiento de silueta; sistema secundario aún sujeto a integración narrativa. Fuente: captura del repositorio, 2026.",
        "Personaje fijando un objetivo con contorno estilizado y viñeta en un entorno urbano de prueba.",
    )

    add_heading(doc, "7.- Conclusiones del Desarrollo del Proyecto", 1, page_break_before=True)
    add_heading(doc, "7.1.- Resultado alcanzado", 2)
    add_paragraphs(doc, """
        Flow State alcanza un prototipo avanzado capaz de expresar su identidad central. La combinación de movimiento, pintura manual, recursos, interfaz diegética y render estilizado existe en el proyecto y puede evaluarse de manera concreta. El menú, la intro, la reputación, el dispositivo musical y la pieza cinematográfica amplían la presentación sin sustituir el núcleo. La principal fortaleza es la coherencia: el graffiti no aparece solo en la historia o en el arte, sino que organiza control, riesgo, UI y consecuencia.

        El trabajo técnico muestra una evolución desde prototipos aislados hacia sistemas con responsabilidades más claras. La corrección del movimiento reducido ilustra el valor de esa arquitectura: fue posible identificar la preferencia persistida, observar la cadena de entrada y añadir pruebas específicas sin reescribir el menú completo. Del mismo modo, la intro separa coordinación, máscara, contenido y cámara, lo que permite desactivarla sin romper el acceso al menú.

        La dirección visual consigue una voz reconocible a través de FSSRS, la paleta industrial y el contraste del graffiti. Las pruebas monocromáticas y la reducción de efectos superfluos ayudaron a priorizar lectura. El proyecto demuestra que el estilo puede ser un sistema regulado por estados, no una capa colocada al final.
    """)

    add_heading(doc, "7.2.- Dificultades y límites", 2)
    add_paragraphs(doc, """
        La mayor dificultad fue coordinar ambición narrativa, herramienta creativa y alcance técnico. Cada sistema central abre necesidades adicionales: la pintura requiere superficies, persistencia y economía; el parkour exige niveles y cámara; la reputación necesita contenido reactivo; los estados visuales requieren criterios narrativos. El aprendizaje fue que la integración debe validarse con una misión completa antes de sumar nuevas funciones.

        Las limitaciones actuales son claras. No hay build independiente validado, campaña completa, audio final, prueba física prolongada ni benchmark en hardware objetivo. La intro permanece desactivada por defecto y el audio de spray está silenciado. La persistencia entre procesos no cuenta con prueba separada, parkour no posee suite específica y no existe CI. Estas brechas impiden declarar el proyecto terminado, pero definen un plan de cierre verificable.

        La documentación también reveló la importancia de separar intención y evidencia. Un sistema puede estar diseñado en detalle y no estar implementado; una captura puede demostrar apariencia, pero no estabilidad; una suite puede pasar y aun contener una prueba dependiente del orden. La memoria adopta esa distinción como criterio metodológico.
    """)

    add_heading(doc, "7.3.- Próximos pasos priorizados", 2)
    add_paragraphs(doc, """
        El siguiente hito debe ser una misión vertical en un distrito acotado. Debe incluir conversación o descubrimiento de intención, lectura del lugar, selección de ruta, infiltración, pintura, escape y regreso para observar consecuencias. Solo esa secuencia permitirá evaluar si la unión de sistemas produce la experiencia declarada.
    """)
    for item in [
        "Generar un build Windows limpio, ejecutar un smoke test y registrar versión, hardware, duración y defectos.",
        "Diseñar una misión vertical con objetivos, rutas alternativas y reacciones conectadas a los tres ejes de reputación.",
        "Completar audio de acción y ambiente, activar el spray bajo opciones de volumen y validar transiciones.",
        "Crear una suite de regresión para parkour, aislar la prueba de migración dependiente del orden y configurar integración continua.",
        "Perfilar CPU, GPU, memoria y tiempos de frame en build con una ruta reproducible y presupuestos por sistema.",
        "Realizar pruebas con mando físico, movimiento reducido y participantes externos; documentar consentimiento, tareas, observaciones y cambios.",
        "Revisar representación y contenido con personas vinculadas al arte urbano y al contexto territorial antes de cerrar narrativa y piezas.",
    ]:
        add_bullet(doc, item)

    add_heading(doc, "7.4.- Aprendizaje proyectual", 2)
    add_paragraphs(doc, """
        El desarrollo muestra que una idea fuerte necesita criterios de descarte. La pregunta rectora permitió decidir qué prototipos acercaban al juego a su promesa y cuáles eran secundarios. También mostró que la identidad no proviene de acumular sistemas, sino de conectar unos pocos con claridad. Movimiento, pintura y consecuencia forman esa columna vertebral.

        A nivel de producción, la evidencia se volvió parte del diseño. Capturas, resultados de pruebas, documentos de fase y control de versiones permitieron revisar decisiones con precisión. El objetivo futuro es extender esa práctica a pruebas de usuario, rendimiento y build. La memoria no cierra Flow State como obra definitiva; cierra una etapa con una base argumentada, visible y técnicamente trazable.
    """)

    add_heading(doc, "8.- Bibliografía y Ludografía", 1, page_break_before=True)
    add_heading(doc, "8.1.- Bibliografía", 2)
    bibliography = [
        "Austin, J. (2001). Taking the Train: How Graffiti Art Became an Urban Crisis in New York City. Columbia University Press.",
        "Csikszentmihalyi, M. (1990). Flow: The Psychology of Optimal Experience. Harper & Row.",
        "Ferrell, J. (1996). Crimes of Style: Urban Graffiti and the Politics of Criminality. Northeastern University Press.",
        "Hunicke, R., LeBlanc, M., & Zubek, R. (2004). MDA: A Formal Approach to Game Design and Game Research. Proceedings of the AAAI Workshop on Challenges in Game AI. https://users.cs.northwestern.edu/~hunicke/MDA.pdf",
        "Lynch, K. (1960). The Image of the City. MIT Press.",
        "Macdonald, N. (2001). The Graffiti Subculture: Youth, Masculinity and Identity in London and New York. Palgrave Macmillan.",
        "Norman, D. A. (2013). The Design of Everyday Things (rev. ed.). Basic Books.",
        "Salen, K., & Zimmerman, E. (2004). Rules of Play: Game Design Fundamentals. MIT Press.",
        "Schell, J. (2008). The Art of Game Design: A Book of Lenses. Morgan Kaufmann.",
        "Registro de Museos de Chile. (s. f.). Museo a Cielo Abierto en San Miguel. https://www.registromuseoschile.cl/663/w3-article-50831.html",
        "Visit Santiago. (s. f.). Museo a Cielo Abierto San Miguel. https://site.visitsantiago.org/que-hacer/museo-inglesias-y-sitios-de-memoria/museo-cielo-abierto-san-miguel/",
        "Unity Technologies. (2026). Universal Render Pipeline, Unity 6 Manual. https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.render-pipelines.universal.html",
    ]
    for entry in bibliography:
        p = body_paragraph(doc, entry, align=WD_ALIGN_PARAGRAPH.LEFT, after=5)
        p.paragraph_format.left_indent = Cm(0.75)
        p.paragraph_format.first_line_indent = Cm(-0.75)

    add_heading(doc, "8.2.- Ludografía", 2)
    ludography = [
        "Atlus. (2016). Persona 5 [Videojuego]. Atlus.",
        "Atari / The Collective. (2006). Marc Ecko’s Getting Up: Contents Under Pressure [Videojuego]. Atari.",
        "Dreamcast / Smilebit. (2000). Jet Set Radio [Videojuego]. Sega.",
        "Infinite Fall. (2017). Night in the Woods [Videojuego]. Finji.",
        "Origame Digital. (2020). Umurangi Generation [Videojuego]. PLAYISM.",
        "Looking Glass Studios. (1998). Thief: The Dark Project [Videojuego]. Eidos Interactive.",
        "Team Reptile. (2023). Bomb Rush Cyberfunk [Videojuego]. Team Reptile.",
    ]
    for entry in ludography:
        p = body_paragraph(doc, entry, align=WD_ALIGN_PARAGRAPH.LEFT, after=5)
        p.paragraph_format.left_indent = Cm(0.75)
        p.paragraph_format.first_line_indent = Cm(-0.75)

    add_heading(doc, "8.3.- Fuentes internas del proyecto", 2)
    add_paragraphs(doc, """
        Flow State. (2026). Repositorio del proyecto Unity: escenas, scripts, materiales, shaders, pruebas, capturas y control de versiones. Corte documental: 6 de octubre de 2026.

        Flow State. (2026). Documentación técnica interna: UnityProjectContext, MainMenu, MainMenu-Phase3A, MainMenu-Phase3B, MainMenu-Phase3AB-Review, GraffitiMenuNeoSprayLab, RENDER_NOTES y FLOW_STATE_STYLIZED_RENDERING.

        Flow State. (2026). Game Design Document actualizado. Documento interno de diseño, versión 6 de octubre de 2026.
    """)

    add_heading(doc, "9.- Anexos y Estado de Cierre", 1, page_break_before=True)
    add_heading(doc, "9.1.- Inventario técnico resumido", 2)
    add_table(
        doc,
        ["Elemento", "Cantidad / versión", "Observación"],
        [
            ["Unity", "6000.0.41f1", "Motor registrado para reproducibilidad"],
            ["URP", "17.0.4", "Pipeline de render"],
            ["Input System", "1.13.1", "Teclado, mouse y gamepad"],
            ["Cinemachine / Timeline", "3.1.7 / 1.8.7", "Cámara y secuencias"],
            ["Escenas", "7; 2 en Build Settings", "MainMenu_FlowState y Gameplay forman el flujo principal"],
            ["C#", "83 archivos; aprox. 13.065 líneas", "Incluye runtime, menú, render y pruebas"],
            ["Prefabs", "5", "Inventario del corte"],
            ["Materiales / shaders", "156 / 29", "Más 4 Shader Graphs"],
            ["Animaciones / controllers", "27 / 3", "Inventario del corte"],
            ["Pruebas", "6 archivos", "Cobertura concentrada en menú, intro y UI"],
        ],
        widths=[3.6, 4.0, 7.9],
        font_size=8.8,
    )

    add_heading(doc, "9.2.- Controles registrados en el prototipo", 2)
    add_table(
        doc,
        ["Contexto", "Entrada", "Acción"],
        [
            ["Movimiento", "Teclado / stick izquierdo", "Desplazamiento y navegación"],
            ["Cámara de menú", "Mouse / stick derecho", "Respuesta ambiental sin navegar opciones"],
            ["Graffiti", "Entrada de puntero", "Pintura manual en primera persona"],
            ["Aerosol", "R", "Agitar lata"],
            ["Aerosol", "F", "Cambiar lata"],
            ["Música", "M / P", "Abrir o controlar dispositivo según modo"],
            ["Música", "1–4", "Selección de capas o pistas en prototipo"],
            ["Música", "Flechas arriba/abajo", "Ajuste de tempo"],
            ["Menú", "WASD, flechas, D-Pad, stick izquierdo", "Navegar"],
            ["Menú", "Submit / Cancel", "Confirmar o volver"],
        ],
        widths=[3.4, 4.3, 7.8],
        font_size=8.8,
    )

    add_heading(doc, "9.3.- Matriz de estado", 2)
    add_table(
        doc,
        ["Componente", "Estado al 06-10-2026", "Criterio para cierre"],
        [
            ["Locomoción y parkour", "Implementado; revisión manual", "Suite de regresión y prueba de sensación"],
            ["Graffiti libre y decals", "Implementado; cinco boquillas y HSV", "Persistencia, audio y economía final"],
            ["Neo Spray Lab / HUD", "Implementado y probado parcialmente", "Aislar prueba de migración y evaluar accesibilidad"],
            ["Menú principal", "Implementado; 19/19 Play Mode en revisión", "Prueba física y build"],
            ["Intro", "Prototipo de 5,10 s; desactivado por defecto", "Audio, handoff y decisión de activación"],
            ["FSSRS", "Implementado; estados visuales disponibles", "Perfil en hardware y calibración por escena"],
            ["Reputación", "Tres ejes persistentes", "Misiones y reacciones que los utilicen"],
            ["Narrativa", "Premisa, arco y facciones definidos", "Misión vertical y guion de implementación"],
            ["Audio", "Placeholders y dispositivo musical", "Biblioteca autorizada, mezcla y opciones"],
            ["Build Windows", "No validado", "Empaquetar, instalar y ejecutar smoke test"],
            ["Rendimiento", "Sin benchmark formal", "Perfil reproducible fuera del Editor"],
            ["CI", "No configurada", "Automatizar suites estables"],
        ],
        widths=[3.5, 5.7, 6.3],
        font_size=8.5,
    )

    add_heading(doc, "9.4.- Registro de figuras", 2)
    add_paragraphs(doc, """
        Las doce figuras de esta memoria provienen del repositorio local de Flow State y corresponden a capturas de desarrollo realizadas durante 2026. Se incluyen como evidencia de proceso y no como imágenes de una versión comercial final. Antes de una publicación pública deben revisarse licencias de assets visibles, créditos y autorizaciones correspondientes.
    """)

    add_heading(doc, "9.5.- Campos administrativos pendientes", 2)
    add_paragraphs(doc, """
        La portada mantiene los campos Profesor guía y Estudiante(s) como “[Por completar]” porque sus nombres no fueron entregados junto con la plantilla. Antes de la entrega institucional se deben completar esos datos, revisar la denominación oficial de carrera y sede si corresponde, actualizar el índice y exportar una versión PDF desde el mismo archivo DOCX.
    """)

    mark_update_fields(doc)
    doc.core_properties.title = "Flow State — Memoria de Proyecto de Título"
    doc.core_properties.subject = "Memoria académica y bitácora de desarrollo del videojuego Flow State"
    doc.core_properties.keywords = "Flow State, videojuego, graffiti, parkour, Unity, memoria de proyecto"
    doc.core_properties.comments = "Construido sobre la plantilla DJD; corte documental 6 de octubre de 2026."
    doc.save(OUTPUT)

    # Re-check immutable source after authoring.
    if sha256(SOURCE) != EXPECTED_SOURCE_SHA256:
        raise RuntimeError("Source template was modified unexpectedly")
    print(OUTPUT)


if __name__ == "__main__":
    build()
