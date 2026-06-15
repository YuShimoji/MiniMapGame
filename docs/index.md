# MiniMapGame Local Documentation View

This page is a local browsing entrypoint for reviewing the existing Markdown
documents in a browser tree pane. It is for audit and temporary browser-side
translation only; it is not a translated, summarized, or reorganized source of
truth.

## Purpose

- Browse the existing documents under `docs/` with the MkDocs Material side
  navigation.
- Open `http://127.0.0.1:8000/` locally and use Chrome, Edge, or the DeepL
  extension for temporary page translation while reviewing.
- Keep the original Markdown documents as the authoritative files. Do not treat
  browser translation output as repository content.

## Local Start

From Windows PowerShell at the repository root:

```powershell
python -m pip install mkdocs-material
python -m mkdocs serve
```

Then open:

```text
http://127.0.0.1:8000/
```

## Notes

- `mkdocs.yml` uses `docs/` as the safe documentation boundary. Root-level
  adapter files such as `AGENTS.md`, `CLAUDE.md`, and the legacy aggregate
  `SPEC.md` remain in place and are not copied into this view.
- Navigation categories are practical review buckets, not a new specification
  hierarchy.
- Permanent translation files are intentionally not generated.
