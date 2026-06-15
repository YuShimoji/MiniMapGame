from __future__ import annotations

import argparse
import sys
from pathlib import Path


EXCLUDED_DIRS = {
    ".git",
    ".venv",
    "__pycache__",
    "build",
    "Build",
    "Builds",
    "dist",
    "Library",
    "Logs",
    "node_modules",
    "Obj",
    "site",
    "Temp",
    "UserSettings",
    "venv",
}

CATEGORY_ORDER = [
    "Overview",
    "Runtime State",
    "Specs",
    "Development Notes",
    "Artifacts",
    "Misc",
]


def is_excluded(path: Path) -> bool:
    return any(part in EXCLUDED_DIRS for part in path.parts)


def first_heading(path: Path) -> str:
    try:
        for line in path.read_text(encoding="utf-8").splitlines():
            stripped = line.strip()
            if stripped.startswith("# "):
                return stripped[2:].strip()
    except UnicodeDecodeError:
        return path.stem
    return path.stem


def yaml_quote(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def classify(path: Path) -> str:
    path_text = path.as_posix().lower()
    name = path.name.lower()
    if name == "runtime-state.md":
        return "Runtime State"
    if path_text.startswith("specs/"):
        return "Specs"
    if path_text.startswith("verification/") or path_text.startswith("archive/"):
        return "Artifacts"
    if name in {"debug-setup.md", "git-auth-troubleshooting.md", "delegation-prompts.md"}:
        return "Development Notes"
    if name in {"index.md", "project-context.md", "invariants.md"} or path_text.startswith("ai/"):
        return "Overview"
    return "Misc"


def markdown_files(root: Path, docs_dir: Path) -> list[Path]:
    files: list[Path] = []
    for path in docs_dir.rglob("*.md"):
        relative_to_root = path.relative_to(root)
        if not is_excluded(relative_to_root):
            files.append(path.relative_to(docs_dir))
    return sorted(files, key=lambda item: item.as_posix().lower())


def outside_docs_markdown(root: Path, docs_dir: Path) -> list[Path]:
    files: list[Path] = []
    for path in root.rglob("*.md"):
        relative = path.relative_to(root)
        if is_excluded(relative):
            continue
        try:
            path.relative_to(docs_dir)
        except ValueError:
            files.append(relative)
    return sorted(files, key=lambda item: item.as_posix().lower())


def nav_candidate(root: Path, docs_dir: Path) -> str:
    grouped = {category: [] for category in CATEGORY_ORDER}
    for relative in markdown_files(root, docs_dir):
        category = classify(relative)
        grouped[category].append(relative)

    lines = [
        "# Nav candidate generated from current Markdown placement.",
        "# Review categories before copying into mkdocs.yml.",
        "nav:",
    ]
    for category in CATEGORY_ORDER:
        entries = grouped[category]
        if not entries:
            continue
        lines.append(f"  - {category}:")
        for relative in entries:
            title = first_heading(docs_dir / relative)
            lines.append(f"      - {yaml_quote(title)}: {relative.as_posix()}")

    outside = outside_docs_markdown(root, docs_dir)
    if outside:
        lines.extend(
            [
                "",
                "# Markdown outside docs_dir is listed for review only.",
                "# MkDocs cannot include these files without copying or moving them.",
            ]
        )
        for relative in outside:
            lines.append(f"# - {relative.as_posix()}")
    return "\n".join(lines) + "\n"


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    parser = argparse.ArgumentParser(
        description="Generate a non-destructive MkDocs nav candidate from Markdown files."
    )
    parser.add_argument("--root", type=Path, default=Path.cwd())
    parser.add_argument("--docs-dir", type=Path, default=Path("docs"))
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    root = args.root.resolve()
    docs_dir = (root / args.docs_dir).resolve()
    result = nav_candidate(root, docs_dir)

    if args.output:
        output = args.output.resolve()
        output.write_text(result, encoding="utf-8")
    else:
        print(result, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
