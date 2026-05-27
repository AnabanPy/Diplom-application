"""Генерация app.ico из Resources/AppIconSource.png (запуск из корня WasteAccountingClient.CSharp)."""
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    raise SystemExit("Установите: py -3 -m pip install pillow")

here = Path(__file__).resolve().parent
png = here.parent / "WasteAccountingClient" / "Resources" / "AppIconSource.png"
ico = here.parent / "WasteAccountingClient" / "Resources" / "app.ico"

if not png.is_file():
    raise SystemExit(f"Нет файла: {png}")

im = Image.open(png).convert("RGBA")
# Один кадр с несколькими размерами в ICO (Pillow сам масштабирует)
sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
im.save(ico, format="ICO", sizes=sizes)
print(f"OK: {ico} ({ico.stat().st_size} bytes)")
