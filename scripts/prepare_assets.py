from pathlib import Path
from PIL import Image
from rembg import new_session, remove

ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "assets"
NAMES = ["idle", "walk", "stand", "sleep", "happy"]
SESSION = new_session("u2netp")


def content_box(image: Image.Image):
    alpha = image.getchannel("A")
    return alpha.getbbox()


def prepare(path: Path):
    image = Image.open(path).convert("RGBA")
    alpha = image.getchannel("A")
    extrema = alpha.getextrema()
    if extrema == (255, 255):
        print(f"removing opaque background from {path.name}")
        image = remove(image, session=SESSION).convert("RGBA")
        alpha = image.getchannel("A")
        extrema = alpha.getextrema()

    if extrema == (255, 255):
        raise RuntimeError(f"{path.name}: background removal did not create transparency")

    box = content_box(image)
    if not box:
        raise RuntimeError(f"{path.name}: image is fully transparent")

    cropped = image.crop(box)
    canvas = Image.new("RGBA", (640, 640), (0, 0, 0, 0))
    cropped.thumbnail((590, 590), Image.Resampling.LANCZOS)
    x = (640 - cropped.width) // 2
    y = 640 - cropped.height - 18
    canvas.alpha_composite(cropped, (x, y))
    canvas.save(path, optimize=True)
    return extrema, box


def create_icons():
    idle = Image.open(ASSET_DIR / "bunny-idle.png").convert("RGBA")
    icon = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    sample = idle.copy()
    sample.thumbnail((238, 238), Image.Resampling.LANCZOS)
    icon.alpha_composite(sample, ((256 - sample.width) // 2, 256 - sample.height))
    icon.save(ASSET_DIR / "icon.png", optimize=True)
    icon.save(
        ASSET_DIR / "icon.ico",
        sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )


if __name__ == "__main__":
    for name in NAMES:
        path = ASSET_DIR / f"bunny-{name}.png"
        extrema, box = prepare(path)
        print(f"prepared {path.name}: alpha={extrema}, source_box={box}, output=640x640")
    create_icons()
    print("created icon.png and icon.ico")
