from __future__ import annotations

import os
import re
import subprocess
import sys
from pathlib import Path


OLD_NAME = "Shigure"

SKIP_DIRS = {
    ".git",
    ".vs",
    ".vscode",
    ".agents",
    ".claude",
    "__pycache__",
    "artifacts",
    "bin",
    "cache",
    "obj",
}

TEXT_EXTENSIONS = {
    ".bat",
    ".cmd",
    ".config",
    ".cs",
    ".csproj",
    ".editorconfig",
    ".gitignore",
    ".json",
    ".md",
    ".props",
    ".ps1",
    ".py",
    ".resx",
    ".sln",
    ".slnx",
    ".targets",
    ".txt",
    ".xaml",
    ".xml",
    ".yaml",
    ".yml",
}

NAME_PATTERN = re.compile(r"^[A-Za-z][A-Za-z0-9_]*$")


def configure_console() -> None:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")


def ask_new_name() -> str:
    while True:
        new_name = input("请输入新的项目名称（英文开头，只能包含英文字母/数字/下划线）: ").strip()
        if NAME_PATTERN.fullmatch(new_name):
            return new_name
        print("名称格式不正确：必须以英文字母开头，只能包含英文字母、数字、下划线。")


def is_in_skipped_dir(path: Path, root: Path) -> bool:
    rel_parts = path.relative_to(root).parts
    return any(part in SKIP_DIRS for part in rel_parts)


def iter_text_files(root: Path, script_path: Path):
    for dirpath, dirnames, filenames in os.walk(root):
        current_dir = Path(dirpath)
        dirnames[:] = [name for name in dirnames if name not in SKIP_DIRS]

        for filename in filenames:
            path = current_dir / filename
            if path == script_path:
                continue
            if is_in_skipped_dir(path, root):
                continue
            if path.suffix.lower() not in TEXT_EXTENSIONS and filename.lower() not in TEXT_EXTENSIONS:
                continue
            yield path


def read_text(path: Path) -> tuple[str, str] | None:
    for encoding in ("utf-8", "gbk"):
        try:
            return path.read_text(encoding=encoding), encoding
        except UnicodeDecodeError:
            continue
    print(f"跳过无法识别编码的文件: {path}")
    return None


def collect_replacements(root: Path, script_path: Path) -> dict[Path, tuple[str, str]]:
    backups: dict[Path, tuple[str, str]] = {}

    for path in iter_text_files(root, script_path):
        result = read_text(path)
        if result is None:
            continue

        text, encoding = result
        if OLD_NAME in text:
            backups[path] = (text, encoding)

    return backups


def apply_replacements(backups: dict[Path, tuple[str, str]], new_name: str) -> None:
    for path, (text, encoding) in backups.items():
        path.write_text(text.replace(OLD_NAME, new_name), encoding=encoding, newline="")


def restore_replacements(
    backups: dict[Path, tuple[str, str]],
    restore_paths: dict[Path, Path] | None = None,
) -> None:
    restore_paths = restore_paths or {}
    for path, (text, encoding) in backups.items():
        restore_path = restore_paths.get(path, path)
        if restore_path != path and not restore_path.exists():
            continue
        restore_path.write_text(text, encoding=encoding, newline="")


def rename_required_file(root: Path, old_filename: str, new_filename: str) -> None:
    old_path = root / old_filename
    new_path = root / new_filename

    if not old_path.exists():
        raise FileNotFoundError(f"找不到需要重命名的文件: {old_path}")
    if new_path.exists():
        raise FileExistsError(f"目标文件已存在，已停止避免覆盖: {new_path}")

    old_path.rename(new_path)


def rename_optional_file(root: Path, old_filename: str, new_filename: str) -> bool:
    old_path = root / old_filename
    new_path = root / new_filename

    if not old_path.exists():
        print(f"未找到可重命名的文件，已跳过: {old_path}")
        return False
    if new_path.exists():
        raise FileExistsError(f"目标文件已存在，已停止避免覆盖: {new_path}")

    old_path.rename(new_path)
    return True


def restore_renamed_file(root: Path, old_filename: str, new_filename: str) -> None:
    old_path = root / old_filename
    new_path = root / new_filename

    if not new_path.exists():
        return
    if old_path.exists():
        old_path.unlink()

    new_path.rename(old_path)


def publish(root: Path, new_name: str) -> None:
    command = [
        "dotnet",
        "publish",
        f".\\{new_name}.csproj",
        "-c",
        "Release",
        "-r",
        "win-x64",
        "--self-contained",
        "true",
        "-p:PublishSingleFile=true",
        "-p:EnableCompressionInSingleFile=true",
        "-o",
        ".\\artifacts\\publish\\win-x64",
    ]

    print()
    print("开始执行发布命令:")
    print(subprocess.list2cmdline(command))
    subprocess.run(command, cwd=root, check=True)


def open_publish_folder(root: Path) -> None:
    publish_dir = root / "artifacts" / "publish" / "win-x64"
    if not publish_dir.exists():
        print(f"打包目录不存在，无法打开: {publish_dir}")
        return

    os.startfile(publish_dir)


def main() -> int:
    configure_console()

    root = Path(__file__).resolve().parent
    script_path = Path(__file__).resolve()

    new_name = ask_new_name()
    should_rename = new_name != OLD_NAME

    if should_rename:
        target_csproj = root / f"{new_name}.csproj"
        target_slnx = root / f"{new_name}.slnx"
        if target_csproj.exists() or target_slnx.exists():
            print("目标项目文件已经存在，已停止避免覆盖。")
            print(f"- {target_csproj}")
            print(f"- {target_slnx}")
            return 1

        backups = collect_replacements(root, script_path)
        preview_files = list(backups)

        print()
        print(f"将把文本中的 {OLD_NAME} 区分大小写替换为 {new_name}。")
        print(f"预计临时修改 {len(preview_files)} 个文本文件，打包结束后会恢复。")
        for path in preview_files:
            print(f"- {path.relative_to(root)}")
    else:
        backups = {}
        print()
        print(f"新名称和原名称相同，将直接使用 {OLD_NAME}.csproj 打包。")

    confirm = input("确认继续？输入 Y/y 继续，其它任意内容取消: ").strip()
    if confirm.casefold() != "y":
        print("已取消。")
        return 0

    csproj_renamed = False
    slnx_renamed = False
    error: BaseException | None = None

    try:
        if should_rename:
            apply_replacements(backups, new_name)
            rename_required_file(root, f"{OLD_NAME}.csproj", f"{new_name}.csproj")
            csproj_renamed = True
            slnx_renamed = rename_optional_file(root, f"{OLD_NAME}.slnx", f"{new_name}.slnx")

            print()
            print(f"临时替换完成，已修改 {len(backups)} 个文本文件。")
            print(f"已重命名: {OLD_NAME}.csproj -> {new_name}.csproj")
            if slnx_renamed:
                print(f"已重命名: {OLD_NAME}.slnx -> {new_name}.slnx")

        publish(root, new_name)
    except BaseException as exc:
        error = exc
    finally:
        if should_rename:
            try:
                print()
                print("发布流程已结束，开始恢复项目名称。")
                restore_paths = {}
                if csproj_renamed:
                    restore_paths[root / f"{OLD_NAME}.csproj"] = root / f"{new_name}.csproj"
                if slnx_renamed:
                    restore_paths[root / f"{OLD_NAME}.slnx"] = root / f"{new_name}.slnx"
                restore_replacements(backups, restore_paths)
                if slnx_renamed:
                    restore_renamed_file(root, f"{OLD_NAME}.slnx", f"{new_name}.slnx")
                if csproj_renamed:
                    restore_renamed_file(root, f"{OLD_NAME}.csproj", f"{new_name}.csproj")
                print()
                print(f"已恢复项目名称为 {OLD_NAME}。")
            except BaseException as restore_exc:
                print(f"恢复项目名称失败: {restore_exc}")
                return 1

    if error is not None:
        if isinstance(error, KeyboardInterrupt):
            print("执行已中断。")
        else:
            print(f"执行失败: {error}")
        return 1

    print()
    print("打包完成。")
    open_publish_folder(root)
    return 0


if __name__ == "__main__":
    sys.exit(main())
