from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Image.open(ROOT / "assets" / "bunny-idle.png").convert("RGBA")
LISTING = ROOT / "store-assets" / "logos"
PACKAGE = ROOT / "store-package" / "Assets"
BACKGROUND = (241, 237, 231, 255)


def fitted_bunny(width: int, height: int, scale: float = 0.84) -> Image.Image:
    subject = SOURCE.copy()
    max_size = (max(1, int(width * scale)), max(1, int(height * scale)))
    subject.thumbnail(max_size, Image.Resampling.LANCZOS)
    return subject


def make_square(path: Path, size: int, scale: float = 0.86) -> None:
    canvas = Image.new("RGBA", (size, size), BACKGROUND)
    subject = fitted_bunny(size, size, scale)
    x = (size - subject.width) // 2
    y = size - subject.height
    canvas.alpha_composite(subject, (x, y))
    canvas.convert("RGB").save(path, optimize=True)


def make_wide(path: Path, width: int, height: int) -> None:
    canvas = Image.new("RGBA", (width, height), BACKGROUND)
    subject = fitted_bunny(int(width * 0.48), height, 0.88)
    x = (width - subject.width) // 2
    y = height - subject.height
    canvas.alpha_composite(subject, (x, y))
    canvas.convert("RGB").save(path, optimize=True)


if __name__ == "__main__":
    LISTING.mkdir(parents=True, exist_ok=True)
    PACKAGE.mkdir(parents=True, exist_ok=True)

    make_square(LISTING / "StoreTile-300x300.png", 300)
    make_square(PACKAGE / "StoreLogo.png", 50)
    make_square(PACKAGE / "Square44x44Logo.png", 44)
    make_square(PACKAGE / "Square150x150Logo.png", 150)
    make_square(PACKAGE / "Square310x310Logo.png", 310)
    make_wide(PACKAGE / "Wide310x150Logo.png", 310, 150)
    make_wide(PACKAGE / "SplashScreen.png", 620, 300)

    for path in [*sorted(LISTING.glob("*.png")), *sorted(PACKAGE.glob("*.png"))]:
        with Image.open(path) as image:
            print(f"{path}: {image.size} {image.mode}")
