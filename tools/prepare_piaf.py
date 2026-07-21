"""Generate a readable pixel-art piaf sprite for the LED ski game."""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets/Sprite/piaf.png"
RES = ROOT / "Assets/Resources/Sprite/piaf.png"
SCALE = 2

CANVAS_W = 22
CANVAS_H = 20
FLAG_W = 22
FLAG_H = 16

# Profil tuto : aile en escalier (haut droite), corps + bec (bas gauche).
BIRD_MASK = [
    "......................",
    "....................KK",
    "...................K#K",
    "..................K##K",
    ".................K###K",
    "................K####K",
    "...............K#####K",
    "..............K######K",
    ".............K#######K",
    "............K########K",
    ".....K############K...",
    "....K#############K....",
    "..KKKKKK##########K....",
    ".K#####K##########K....",
    "..KKKKKK##########K....",
    "......K########K.......",
    ".......K######K........",
    "........K####K.........",
    ".........K##K..........",
    "..........KK...........",
]

COLORS = {
    ".": (0, 0, 0, 0),
    "K": (18, 18, 24, 255),
    "X": (186, 12, 47, 255),
    "F": (255, 255, 255, 255),
    "V": (0, 32, 91, 255),
    "E": (18, 18, 24, 255),
}


def norway_flag_pixel(x: int, y: int) -> str:
    white_arm = 4
    blue_arm = 2
    v_center = 6
    h_center = FLAG_H // 2

    v_white = (v_center - white_arm // 2) <= x <= (v_center + white_arm // 2 - 1)
    h_white = (h_center - white_arm // 2) <= y <= (h_center + white_arm // 2 - 1)
    v_blue = (v_center - blue_arm // 2) <= x <= (v_center + blue_arm // 2 - 1)
    h_blue = (h_center - blue_arm // 2) <= y <= (h_center + blue_arm // 2 - 1)

    if v_blue or h_blue:
        return "V"
    if v_white or h_white:
        return "F"
    return "X"


def flag_at_canvas(x: int, y: int) -> str:
    """Drapeau a orientation fixe sur le canvas (evite l'effet deltaplane)."""
    flag_x = round(x / max(1, CANVAS_W - 1) * (FLAG_W - 1))
    flag_y = round(y / max(1, CANVAS_H - 1) * (FLAG_H - 1))
    return norway_flag_pixel(flag_x, flag_y)


def build_bird_rows() -> list[str]:
    width = max(len(row) for row in BIRD_MASK)
    grid = [list(row.ljust(width, ".")) for row in BIRD_MASK]

    grid[11][8] = "E"

    for y, row in enumerate(grid):
        for x, ch in enumerate(row):
            if ch == "#":
                row[x] = flag_at_canvas(x, y)

    return ["".join(row) for row in grid]


def build_image() -> Image.Image:
    rows = build_bird_rows()
    image = Image.new("RGBA", (len(rows[0]), len(rows)), (0, 0, 0, 0))

    for y, row in enumerate(rows):
        for x, char in enumerate(row):
            if char not in COLORS:
                raise ValueError(f"Unknown color key '{char}' at row {y}, col {x}")
            image.putpixel((x, y), COLORS[char])

    if SCALE > 1:
        image = image.resize((image.width * SCALE, image.height * SCALE), Image.NEAREST)

    return image


def main() -> None:
    image = build_image()
    print(f"Piaf sprite: {image.size[0]}x{image.size[1]}")

    OUT.parent.mkdir(parents=True, exist_ok=True)
    RES.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUT)
    image.save(RES)
    print(f"Wrote {OUT}")
    print(f"Copied to {RES}")


if __name__ == "__main__":
    main()
