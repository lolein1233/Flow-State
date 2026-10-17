# FlowState GDD template contract

## Reference

- Retained reference: `C:\Users\alons\Downloads\FlowState_GDD.docx`
- SHA-256: `9723F1357D296090863CC1F0810ADDD4DCF843CE9A51B3F75B1FF7E8E95D93FA`
- Reference render: `C:\Users\alons\FLOWSTATE\.codex-gdd-work\reference-render`
- Evidence: `template-style-evidence.json` and `reference-inspection.json`
- Pages: 33 in Microsoft Word
- Sections: 1
- Existing body paragraphs: 193
- Existing tables: 30
- Existing images: 30 drawings, including 24 floating and 6 inline

## Page system

- A4 portrait, 8.2681 by 11.6931 inches.
- Margins: left 0.6299, right 0.6299, top 0.5708, bottom 0.5313 inches.
- Header and footer distances: 0.5 inches.
- One section with a different first page.
- First page is a full-bleed dark cover composition.
- All later pages use a white content field framed by the retained black textured border in the existing header.
- Footer on later pages is centered gray text `Flow State - Página` followed by the existing PAGE field.
- New content must remain in the same section so the border and footer continue unchanged.

## Typography

- Major numbered headings use Heading 1 semantics with Arial/Arial Black, dark navy `#111827`, 14 pt, bold visual weight, 24 pt before, keep-with-next and keep-together.
- Subheadings use Heading 2 semantics but direct Courier New treatment, dark charcoal/navy, 13 pt and bold, with 10 pt before and keep-with-next.
- Tertiary headings use the retained Heading 3 hierarchy and Courier New treatment.
- Body copy uses the document default Arial, approximately 11 pt, dark ink `#101725`, with the existing 1.15-line paragraph rhythm and 10 pt paragraph spacing.
- Figure captions are centered, small gray Arial, matching the existing captions on image pages.
- The retained cover, title art, headings, body text, tables, headers, footers and captions are design authority. Do not apply a generic document preset.

## Tables and lists

- Tables occupy approximately the 7.0-inch content width with intentionally unequal columns.
- Header rows use dark navy `#111827` with white bold Arial text.
- Body rows alternate white and very pale gray-blue, with thin black or dark-gray borders as in the reference.
- Cells use internal padding, vertical centering, automatic height and wrapped text.
- Header rows repeat when a table spans pages.
- Existing bullet/list formatting remains untouched. New lists are used sparingly; implementation inventories prefer tables.

## Components and content flow

- Cover page and branding remain unchanged.
- Automatic table of contents on page 2 remains in its content control and is refreshed after new headings are added.
- Sections 1 through 12 and all their text, figures, tables, bookmarks and relationships remain unchanged.
- New material is appended after section 12 as section 13.
- New section follows this sequence: current status, playable flow and architecture, controls, graffiti, movement and parkour, visual rendering, main menu, intro and cinematics, auxiliary systems, technical inventory, validation and update conventions.
- New figures are inline for stability, centered, scaled down from high-resolution project captures and followed immediately by captions.

## Slot map

- Editable slot: the end-of-document insertion point immediately before the final section properties in `word/document.xml`.
- Allowed content: new paragraphs, tables, inline figures, captions and page breaks belonging to section 13.
- Existing body elements: preserve, except the automatic TOC field result may change when refreshed to include section 13.
- Existing header/footer parts, theme, styles, numbering, bookmarks, comments, content controls, drawings and relationships: preserve.
- New image relationships and media parts may be added only for the selected project screenshots.
- The final settings may request field refresh, and Word may update cached TOC/PAGE results.

## Package preservation

- Full baseline part inventory with paths, byte sizes and SHA-256 hashes is stored in `reference-inspection.json`.
- Preserve-only classes: all existing `word/media/*`, `word/header*`, `word/footer*`, `word/theme/*`, `word/numbering.xml`, `word/styles.xml`, `customXml/*`, existing relationship targets and embedded cover art.
- Editable classes: `word/document.xml`, its relationship file, `[Content_Types].xml`, settings needed for field refresh, and new `word/media/*` entries.
- The source reference remains untouched and must still match the recorded SHA-256 after delivery.

## Fidelity gates

- Render the final document through Microsoft Word because the packaged LibreOffice runtime is unavailable and the source relies on 24 floating drawings.
- Inspect every final page at full resolution.
- Confirm no clipping, image overlap, broken table, missing border, font substitution, orphaned caption or unexpected blank page.
- Confirm the black border and centered page footer continue through all appended pages.
- Confirm the existing 193 body paragraph texts and 30 table contents remain unchanged, excluding the expected TOC cache refresh.
- Confirm all original media hashes remain unchanged.
- Confirm the retained reference file remains byte-for-byte unchanged.
