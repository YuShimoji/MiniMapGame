# MiniMapGame Project Hub

This hub renders the existing Markdown documents for browser navigation. It is
a derived view of repository content, not another project-state source of
truth. The same structure can be served locally or published without copying
status into a separately edited Wiki.

## 現在地への入口

- [Project Context](project-context.md): active lane、active slice、現在有効な判断
- [Runtime State](runtime-state.md): 次セッション用の短い継続状態と最新の検証
- [Specification Index](spec-index.json): 各 spec の lifecycle、進捗、参照先
- [Agent Rules](ai/AGENT_RULES.md): 監修AI・開発AIが共有する作業ルール

active lane や進捗率をこのページへ再記載しない。上記の正本を更新すれば、
この hub の表示も同じ commit から更新される。

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
- `README`, GitHub Pages, Wiki, Issues, and Projects must link to or derive from
  the canonical documents rather than own another copy of current status.
- Permanent translation files are intentionally not generated.
