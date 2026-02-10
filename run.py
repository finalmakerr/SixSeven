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

def remove_bg_and_optimize(src: Path, dst: Path):
    tmp_png = TMP_DIR / (src.stem + "_nobg.png")

    print(f"Removing background: {src.name}")
    subprocess.run(
        ["rembg", "i", str(src), str(tmp_png)],
        check=True
    )

    print(f"Optimizing + resizing to 512x512: {src.name}")
    subprocess.run(
        [
            "magick", str(tmp_png),
            "-trim", "+repage",                 # crop to subject
            "-resize", "512x512>",              # fit inside 512
            "-background", "none",
            "-gravity", "center",
            "-extent", "512x512",               # exact canvas
            str(dst)
        ],
        check=True
    )

    tmp_png.unlink()

# =========================
# 1️⃣ ROOT FILES
# =========================
root_out = DONE_DIR / "root"
root_out.mkdir(exist_ok=True)

for file in BASE_DIR.iterdir():
    if not file.is_file():
        continue
    if file.name in {"run.py"}:
        continue

    ext = file.suffix.lower()

    if ext in IMAGE_EXTS:
        remove_bg_and_optimize(file, root_out / file.name)
        file.unlink()

    elif ext in VIDEO_EXTS:
        shutil.move(str(file), root_out / file.name)

# =========================
# 2️⃣ SUBFOLDERS
# =========================
for folder in BASE_DIR.iterdir():
    if not folder.is_dir():
        continue
    if folder.name in {"DONE", "_tmp"}:
        continue

    print(f"\nProcessing folder: {folder.name}")

    out_dir = DONE_DIR / folder.name
    out_dir.mkdir(parents=True, exist_ok=True)

    for file in folder.iterdir():
        if not file.is_file():
            continue

        ext = file.suffix.lower()

        if ext in IMAGE_EXTS:
            remove_bg_and_optimize(
                file,
                out_dir / f"{folder.name}_{file.name}"
            )

        elif ext in VIDEO_EXTS:
            shutil.move(str(file), out_dir / file.name)

    shutil.rmtree(folder)
    print(f"🗑 Deleted original folder: {folder.name}")

shutil.rmtree(TMP_DIR)

print("\nALL DONE ✔")
input("Press Enter to exit...")
