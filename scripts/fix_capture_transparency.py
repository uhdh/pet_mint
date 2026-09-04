from collections import deque
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SCREENSHOTS = ROOT / "store-assets" / "screenshots"
BACKGROUND = (216, 212, 207)
NEAR_BLACK = 10
MIN_COMPONENT = 400


def is_near_black(pixel):
    return max(pixel[:3]) <= NEAR_BLACK


def clean(path: Path):
    image = Image.open(path).convert("RGB")
    pixels = image.load()
    width, height = image.size
    seen = bytearray(width * height)
    replaced = 0

    for y in range(height):
        for x in range(width):
            index = y * width + x
            if seen[index] or not is_near_black(pixels[x, y]):
                continue

            queue = deque([(x, y)])
            seen[index] = 1
            component = []

            while queue:
                cx, cy = queue.popleft()
                component.append((cx, cy))
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        continue
                    neighbor = ny * width + nx
                    if seen[neighbor] or not is_near_black(pixels[nx, ny]):
                        continue
                    seen[neighbor] = 1
                    queue.append((nx, ny))

            if len(component) >= MIN_COMPONENT:
                for cx, cy in component:
                    pixels[cx, cy] = BACKGROUND
                replaced += len(component)

    image.save(path, optimize=True)
    print(f"{path.name}: replaced {replaced} connected capture-background pixels")


if __name__ == "__main__":
    for screenshot in sorted(SCREENSHOTS.glob("*.png")):
        clean(screenshot)
