import re
import struct
import sys
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def parse_color(style: str) -> tuple[int, int, int, int]:
    match = re.search(r"background-color:\s*rgba?\(([^)]+)\)", style)
    if not match:
        return (0, 0, 0, 255)

    parts = [part.strip() for part in match.group(1).split(",")]
    red = int(float(parts[0]))
    green = int(float(parts[1]))
    blue = int(float(parts[2]))

    if len(parts) > 3:
        alpha_value = parts[3]
        alpha = int(float(alpha_value) * 255) if "." in alpha_value else int(alpha_value)
    else:
        alpha = 255

    return (red, green, blue, alpha)


def is_transparent_pixel(red: int, green: int, blue: int, treat_black_as_transparent: bool) -> bool:
    if red >= 240 and green >= 240 and blue >= 240:
        return True

    if treat_black_as_transparent and red <= 5 and green <= 5 and blue <= 5:
        return True

    return False


def to_rgba_pixel(
    red: int,
    green: int,
    blue: int,
    alpha: int,
    treat_black_as_transparent: bool,
) -> tuple[int, int, int, int]:
    if is_transparent_pixel(red, green, blue, treat_black_as_transparent):
        return (0, 0, 0, 0)
    return (red, green, blue, 255)


def convert_html_to_png(html_path: Path, out_path: Path, treat_black_as_transparent: bool = False) -> None:
    html = html_path.read_text(encoding="utf-8")
    rows = re.findall(r"<tr>(.*?)</tr>", html, re.DOTALL)
    pixels: list[tuple[int, int, int, int]] = []

    for row in rows:
        for style in re.findall(r'<td[^>]*style="([^"]+)"[^>]*>', row):
            red, green, blue, alpha = parse_color(style)
            pixels.append(to_rgba_pixel(red, green, blue, alpha, treat_black_as_transparent))

    width = len(re.findall(r"<td", rows[0]))
    height = len(rows)
    print(f"{html_path.name}: {width}x{height}, pixels: {len(pixels)}")

    raw = b""
    for y in range(height):
        raw += b"\x00"
        for x in range(width):
            raw += bytes(pixels[y * width + x])

    def png_chunk(tag: bytes, data: bytes) -> bytes:
        crc = zlib.crc32(tag + data) & 0xFFFFFFFF
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", crc)

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n"
    png += png_chunk(b"IHDR", ihdr)
    png += png_chunk(b"IDAT", zlib.compress(raw, 9))
    png += png_chunk(b"IEND", b"")
    out_path.write_bytes(png)
    print(f"Wrote {out_path}")

    resources_path = ROOT / "Assets/Resources/Sprite" / out_path.name
    if resources_path.parent.exists():
        resources_path.write_bytes(png)
        print(f"Copied to {resources_path}")


def main() -> None:
    presets = {
        "skieur": (
            ROOT / "Assets/Sprite/skieur pixel art.html",
            ROOT / "Assets/Sprite/skieur.png",
            False,
        ),
        "obstacle": (
            ROOT / "Assets/Sprite/roche_obstacle.html",
            ROOT / "Assets/Sprite/roche_obstacle.png",
            False,
        ),
        "sapin": (
            ROOT / "Assets/Sprite/Sapin.html",
            ROOT / "Assets/Sprite/sapin.png",
            False,
        ),
        "piaf": (
            ROOT / "Assets/Sprite/piaf.html",
            ROOT / "Assets/Sprite/piaf.png",
            False,
        ),
    }

    target = sys.argv[1] if len(sys.argv) > 1 else "all"
    if target == "all":
        for _, args in presets.items():
            convert_html_to_png(*args)
        return

    if target not in presets:
        raise SystemExit(f"Unknown target '{target}'. Use: skieur, obstacle, sapin, piaf, all")

    convert_html_to_png(*presets[target])


if __name__ == "__main__":
    main()
