#!/usr/bin/env python3
import argparse
import sys
import zipfile
from pathlib import Path


BASE_DIRS = ("Runtime", "Editor")
DOTWEEN_DIR = "DOTween"
ROOT_FILES = ("README.md", "CHANGELOG.md")
EXCLUDED_SUFFIXES = (".meta", ".asmdef")
WITH_DOTWEEN_ZIP = "UIController_WithDoTween.zip"
WITHOUT_DOTWEEN_ZIP = "UIController_WithoutDoTween.zip"


def parse_args():
    parser = argparse.ArgumentParser(
        description="Create UIController release zip packages with and without DOTween."
    )
    parser.add_argument(
        "--root",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help="UIController package root. Defaults to this script's parent package root.",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=None,
        help="Output directory for zip files. Defaults to the package root.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the files that would be added without creating zip files.",
    )
    return parser.parse_args()


def collect_files(root, dir_names):
    files = []

    for dir_name in dir_names:
        target_dir = root / dir_name
        if not target_dir.is_dir():
            raise FileNotFoundError(f"Required directory not found: {target_dir}")

        for file_path in sorted(target_dir.rglob("*")):
            if file_path.is_file() and file_path.suffix not in EXCLUDED_SUFFIXES:
                files.append(file_path)

    for file_name in ROOT_FILES:
        file_path = root / file_name
        if not file_path.is_file():
            raise FileNotFoundError(f"Required file not found: {file_path}")

        files.append(file_path)

    return files


def create_zip(root, zip_path, dir_names, dry_run):
    files = collect_files(root, dir_names)

    if dry_run:
        print(f"[dry-run] {zip_path}")
        for file_path in files:
            print(f"  {file_path.relative_to(root).as_posix()}")
        return

    zip_path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for file_path in files:
            archive.write(file_path, file_path.relative_to(root).as_posix())

    print(f"Created: {zip_path}")


def main():
    args = parse_args()
    root = args.root.resolve()
    output_dir = (args.output_dir or root).resolve()

    create_zip(
        root,
        output_dir / WITHOUT_DOTWEEN_ZIP,
        BASE_DIRS,
        args.dry_run,
    )
    create_zip(
        root,
        output_dir / WITH_DOTWEEN_ZIP,
        (*BASE_DIRS, DOTWEEN_DIR),
        args.dry_run,
    )


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(exc, file=sys.stderr)
        sys.exit(1)
