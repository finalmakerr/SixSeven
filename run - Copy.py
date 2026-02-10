import subprocess
import shutil
from pathlib import Path

BASE_DIR = Path(__file__).parent.resolve()
DONE_DIR = BASE_DIR / "DONE"
TMP_DIR = BASE_DIR / "_tmp"

IMAGE_EXTS = {".png", ".jpg", ".jpeg", ".webp"}
VIDEO_EXTS = {".mp4", ".mov", ".avi", ".webm"}

DONE_DIR.mkdir(exist_ok=True)
TMP_DIR.mkdir(exist_ok=True)

def remove_bg_and_resize(src: Path, dst: Path):
    tmp_png = TMP_DIR / f"{src.stem}_nobg.png"

    print(f"Removing background: {src.name}")
    subprocess.run(
        ["rembg", "i", str(src), str(tmp_png)],
        check=True
    )

    subprocess.run(
        [
            "magick", str(tmp_png),
            "-resize", "512x512>",
            "-background", "none",
            "-gravity", "center",
            "-extent", "512x512",
            str(dst)
        ],
        check=True
    )

    tmp_png.unlink()

# ROOT FILES
root_out = DONE_DIR / "root"
root_out.mkdir(exist_ok=True)

for file in BASE_DIR.iterdir():
    if file.is_file() and file.name != "run.py":
        ext = file.suffix.lower()
        if ext in IMAGE_EXTS:
            remove_bg_and_resize(file, root_out / file.name)
            file.unlink()
        elif ext in VIDEO_EXTS:
            shutil.move(str(file), root_out / file.name)

# SUBFOLDERS
for folder in BASE_DIR.iterdir():
    if folder.is_dir() and folder.name not in {"DONE", "_tmp"}:
        out_dir = DONE_DIR / folder.name
        out_dir.mkdir(parents=True, exist_ok=True)

        for file in folder.iterdir():
            if file.is_file():
                ext = file.suffix.lower()
                if ext in IMAGE_EXTS:
                    remove_bg_and_resize(
                        file,
                        out_dir / f"{folder.name}_{file.name}"
                    )
                elif ext in VIDEO_EXTS:
                    shutil.move(str(file), out_dir / file.name)

        shutil.rmtree(folder)

shutil.rmtree(TMP_DIR)

print("\nALL DONE ✔")
input("Press Enter to exit...")
