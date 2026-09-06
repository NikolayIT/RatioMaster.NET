"""Builds the application icon assets from icon.svg.

Renders the SVG with resvg (run ``npm install`` in build/icons first) and assembles:
  src/RatioMaster.App/Assets/app.ico   multi-size Windows icon (also the tray and window icon)
  src/RatioMaster.App/Assets/app.png   256 px PNG (Linux .desktop icon, About dialog)
  src/RatioMaster.App/Assets/app.icns  macOS bundle icon
  web/public/favicon.ico               website favicon

Run from anywhere: (cd build/icons && npm install) && python build/icons/make-icons.py
"""
import os
import struct
import subprocess
import tempfile

from PIL import Image

root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
assets = os.path.join(root, "src", "RatioMaster.App", "Assets")
os.makedirs(assets, exist_ok=True)

ico_sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
icns_sizes = [16, 32, 64, 128, 256, 512, 1024]
favicon_sizes = [16, 32, 48]
sizes = sorted(set(ico_sizes + icns_sizes + favicon_sizes))


def write_ico(path, images, wanted):
    """Writes a multi-resolution .ico, one PNG-compressed frame per size."""
    frames = []
    for size in wanted:
        with tempfile.TemporaryDirectory() as tmp:
            png_path = os.path.join(tmp, "f.png")
            images[size].save(png_path, format="PNG")
            with open(png_path, "rb") as handle:
                frames.append((size, handle.read()))

    header = struct.pack("<HHH", 0, 1, len(frames))
    offset = len(header) + 16 * len(frames)
    entries = bytearray()
    data = bytearray()
    for size, png in frames:
        entries += struct.pack(
            "<BBBBHHII", size & 0xFF, size & 0xFF, 0, 0, 1, 32, len(png), offset)
        data += png
        offset += len(png)

    with open(path, "wb") as handle:
        handle.write(header)
        handle.write(entries)
        handle.write(data)


with tempfile.TemporaryDirectory() as tmp:
    subprocess.run(
        ["node", os.path.join(root, "build", "icons", "render-svg.mjs"), tmp]
        + [str(s) for s in sizes],
        check=True,
        shell=(os.name == "nt"),
    )
    images = {}
    for size in sizes:
        image = Image.open(os.path.join(tmp, f"icon-{size}.png")).convert("RGBA")
        image.load()
        images[size] = image

    write_ico(os.path.join(assets, "app.ico"), images, ico_sizes)
    write_ico(os.path.join(root, "web", "public", "favicon.ico"), images, favicon_sizes)
    images[256].save(os.path.join(assets, "app.png"), format="PNG")
    icns = [images[s] for s in icns_sizes]
    icns[0].save(os.path.join(assets, "app.icns"), format="ICNS", append_images=icns[1:])

print("icons written to", assets)
