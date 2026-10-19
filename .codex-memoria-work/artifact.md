# Execution contract — Memoria Flow State

## Source authority

- Template: `C:/Users/alons/Downloads/DJD - Plantilla Memoria.docx`
- SHA-256 before editing: `BE8B8A9225062BE01D79AC11CF0511C17A9B9CEE26D2DFE51BABAD547EFF5127`
- The source file is immutable. All authoring happens in a new output file.
- Reference rendering: 5 pages exported from Microsoft Word and rasterized in
  `.codex-memoria-work/template-reference-render/`.

## Page system and visual grammar

- A4 portrait, one section, 1 inch margins, header/footer distance 0.5 inch.
- Default language: Spanish; default family: Arial; body: 11 pt, 1.15 line spacing.
- Cover keeps the institutional logo and the original hierarchy: 26 pt title,
  15 pt gray project title, guide label and lower-right author/date block.
- Heading hierarchy is inherited from the template: Heading 1 at 20 pt,
  Heading 2 at 16 pt, Heading 3 at 14 pt gray.
- Body is black on white, justified, without decorative page furniture.
- Figures are inline, centered, and immediately followed by a concise caption.
- Tables use a restrained grayscale treatment compatible with the academic template.

## Slot map

- Cover placeholder `Nombre del proyecto` -> `Flow State`.
- Cover `Profesor Guía` and `Nombre Estudiantes` remain explicit `[Por completar]`
  fields because no verified names were supplied.
- Cover date -> `6 de octubre de 2026`.
- Manual sample index -> generated Word TOC based on Heading 1–3.
- Every italic instructional paragraph in sections 2–7 is replaced with substantive
  project content; no template instruction is retained as project prose.
- The original seven-section structure is preserved and expanded with numbered
  subsections. Bibliography/ludography and annexes are appended as sections 8 and 9.
- The template logo, styles, page geometry, relationships, theme and section settings
  remain inherited from the original package.

## Content authority and evidence policy

- Project facts come from the current Unity repository, its technical notes, tests,
  scene/build settings, Git history and the updated GDD at the 2026-10-06 cutoff.
- Conceptual and state-of-the-art claims are attributed to bibliography/ludography.
- Prototype, validated, pending and proposed states are kept distinct.
- No professor, student, tester, release, performance or final-build result is invented.

## Fidelity and quality gates

1. Original SHA-256 remains unchanged.
2. Output opens in Word and preserves A4 geometry, margins, logo and heading hierarchy.
3. No template instructional text or unresolved generic title placeholder remains.
4. All required sections, figures, captions, tables and bibliography are present.
5. TOC is updated after pagination.
6. Every page is rendered to PNG and inspected at full resolution for clipping,
   overlap, accidental blank pages, orphan headings and unreadable tables.
7. Final package is opened in Codex after verification.
