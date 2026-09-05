"""
Create high-resolution, anti-aliased item sprites for My Bunny Desktop Pet.
Items:
1. item-hay.png (건초 더미)
2. item-doll.png (토끼 인형 - "주인님,,그녀석은 가짜에요")
3. item-bag.png (여행 가방 / 당근 가방)
4. item-house.png (아늑한 토끼 원목 하우스)
"""

import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "assets"
SITE_ASSET_DIR = ROOT / "site" / "assets"
NATIVE_ASSET_DIR = ROOT / "native" / "Assets"

CANVAS_SIZE = 512
SCALE = 2  # Draw at 1024x1024 for supersampling


def create_supersampled_image():
    return Image.new("RGBA", (CANVAS_SIZE * SCALE, CANVAS_SIZE * SCALE), (0, 0, 0, 0))


def downsample(img: Image.Image):
    return img.resize((CANVAS_SIZE, CANVAS_SIZE), Image.Resampling.LANCZOS)


# ---------------------------------------------------------------------------
# 1. 건초 (Hay Bundle)
# ---------------------------------------------------------------------------
def draw_hay():
    s = SCALE
    img = create_supersampled_image()
    draw = ImageDraw.Draw(img)

    cx, cy = 256 * s, 290 * s

    # 그림자 (Shadow)
    shadow_box = [cx - 160 * s, cy + 90 * s, cx + 160 * s, cy + 140 * s]
    draw.ellipse(shadow_box, fill=(40, 30, 20, 50))

    # 개별 건초 줄기들 (Hay stalks) - 여러 각도로 겹쳐서 자연스러운 건초더미 형성
    stalk_colors = [
        (238, 206, 128),  # 황금빛 건초
        (225, 192, 110),
        (210, 175, 95),
        (245, 218, 145),
        (185, 190, 115),  # 마른 초록빛 풀
        (230, 195, 118),
        (200, 160, 85),
    ]

    # 기본 더미 몸체 (Hay bundle base bulk)
    for r in range(120 * s, 40 * s, -10 * s):
        col = (220, 185, 105, 255)
        draw.ellipse([cx - r * 1.3, cy - r * 0.9, cx + r * 1.3, cy + r * 0.9], fill=col)

    # 흩날리는 줄기들 (Stalks)
    import random
    rng = random.Random(42)

    for i in range(160):
        col = rng.choice(stalk_colors)
        angle = rng.uniform(-math.pi * 0.85, -math.pi * 0.15)
        length = rng.uniform(100 * s, 180 * s)
        width = rng.randint(4 * s, 8 * s)
        curve = rng.uniform(-30 * s, 30 * s)

        x0 = cx + rng.uniform(-90 * s, 90 * s)
        y0 = cy + rng.uniform(-40 * s, 60 * s)
        x1 = x0 + math.cos(angle) * length + curve
        y1 = y0 + math.sin(angle) * length

        # 선 그리기
        draw.line([(x0, y0), (x1, y1)], fill=col, width=width)
        # 줄기 끝 뾰족하게
        draw.ellipse([x1 - width // 2, y1 - width // 2, x1 + width // 2, y1 + width // 2], fill=col)

    # 줄기들 아래쪽 삐져나온 부분들
    for i in range(70):
        col = rng.choice(stalk_colors)
        angle = rng.uniform(math.pi * 0.1, math.pi * 0.9)
        length = rng.uniform(40 * s, 90 * s)
        width = rng.randint(4 * s, 7 * s)
        x0 = cx + rng.uniform(-110 * s, 110 * s)
        y0 = cy + rng.uniform(10 * s, 70 * s)
        x1 = x0 + math.cos(angle) * length
        y1 = y0 + math.sin(angle) * length
        draw.line([(x0, y0), (x1, y1)], fill=col, width=width)

    # 건초를 묶는 리본 끈 (Ribbon rope)
    ribbon_y = cy + 15 * s
    ribbon_w = 115 * s
    ribbon_h = 24 * s
    # 끈 본체
    draw.rounded_rectangle(
        [cx - ribbon_w, ribbon_y - ribbon_h // 2, cx + ribbon_w, ribbon_y + ribbon_h // 2],
        radius=8 * s,
        fill=(88, 140, 75),  # 상큼한 녹색 리본
        outline=(55, 95, 45),
        width=3 * s,
    )
    # 리본 매듭 (Knot & Bow)
    draw.ellipse([cx - 18 * s, ribbon_y - 18 * s, cx + 18 * s, ribbon_y + 18 * s], fill=(105, 160, 90), outline=(55, 95, 45), width=3 * s)
    # 리본 날개
    draw.polygon([(cx - 15 * s, ribbon_y), (cx - 45 * s, ribbon_y - 20 * s), (cx - 40 * s, ribbon_y + 15 * s)], fill=(105, 160, 90), outline=(55, 95, 45))
    draw.polygon([(cx + 15 * s, ribbon_y), (cx + 45 * s, ribbon_y - 20 * s), (cx + 40 * s, ribbon_y + 15 * s)], fill=(105, 160, 90), outline=(55, 95, 45))
    # 리본 꼬리
    draw.line([(cx - 10 * s, ribbon_y + 10 * s), (cx - 28 * s, ribbon_y + 42 * s)], fill=(88, 140, 75), width=6 * s)
    draw.line([(cx + 10 * s, ribbon_y + 10 * s), (cx + 28 * s, ribbon_y + 42 * s)], fill=(88, 140, 75), width=6 * s)

    # 귀여운 꽃 한 송이 꽂아두기 (Little Daisy on the hay)
    fx, fy = cx + 65 * s, cy - 45 * s
    for a in range(6):
        ang = a * math.pi / 3
        px = fx + math.cos(ang) * 16 * s
        py = fy + math.sin(ang) * 16 * s
        draw.ellipse([px - 9 * s, py - 9 * s, px + 9 * s, py + 9 * s], fill=(255, 255, 255), outline=(220, 215, 205), width=2 * s)
    draw.ellipse([fx - 8 * s, fy - 8 * s, fx + 8 * s, fy + 8 * s], fill=(255, 200, 50))

    return downsample(img)


# ---------------------------------------------------------------------------
# 2. 토끼 인형 (item-doll.png)
# "주인님,,그녀석은 가짜에요"
# ---------------------------------------------------------------------------
def draw_doll():
    s = SCALE
    img = create_supersampled_image()
    draw = ImageDraw.Draw(img)

    cx, cy = 256 * s, 280 * s

    # 바닥 그림자
    draw.ellipse([cx - 120 * s, cy + 90 * s, cx + 120 * s, cy + 135 * s], fill=(40, 30, 20, 55))

    # 몸통 색상 (봉제인형 느낌의 부드러운 양모 크림색)
    body_fill = (235, 228, 218)
    body_shade = (210, 200, 188)
    ear_fill = (145, 138, 132)  # 회색 롭이어 귀
    ear_inner = (220, 185, 185)
    stitch_col = (90, 75, 68)

    # 1. 늘어진 롭이어 귀 (뒤쪽/양옆)
    # 왼쪽 귀
    draw.ellipse([cx - 155 * s, cy - 90 * s, cx - 75 * s, cy + 80 * s], fill=ear_fill, outline=stitch_col, width=3 * s)
    draw.ellipse([cx - 142 * s, cy - 40 * s, cx - 88 * s, cy + 60 * s], fill=ear_inner)
    # 귀 봉제선 스티치 (Stitch marks)
    for sy in range(int(cy - 60 * s), int(cy + 60 * s), int(20 * s)):
        draw.line([(cx - 155 * s, sy), (cx - 145 * s, sy + 6 * s)], fill=stitch_col, width=3 * s)

    # 오른쪽 귀
    draw.ellipse([cx + 75 * s, cy - 90 * s, cx + 155 * s, cy + 80 * s], fill=ear_fill, outline=stitch_col, width=3 * s)
    draw.ellipse([cx + 88 * s, cy - 40 * s, cx + 142 * s, cy + 60 * s], fill=ear_inner)
    for sy in range(int(cy - 60 * s), int(cy + 60 * s), int(20 * s)):
        draw.line([(cx + 155 * s, sy), (cx + 145 * s, sy + 6 * s)], fill=stitch_col, width=3 * s)

    # 2. 둥근 뚱뚱한 몸통 (Round plush body)
    draw.ellipse([cx - 105 * s, cy - 10 * s, cx + 105 * s, cy + 115 * s], fill=body_fill, outline=stitch_col, width=4 * s)

    # 배 패치 (Tummy patch with stitches)
    patch_box = [cx - 50 * s, cy + 15 * s, cx + 50 * s, cy + 85 * s]
    draw.ellipse(patch_box, fill=(248, 244, 238), outline=stitch_col, width=2 * s)
    # 패치 십자 스티치들
    for ang in range(0, 360, 45):
        rad = math.radians(ang)
        px = cx + math.cos(rad) * 44 * s
        py = cy + 50 * s + math.sin(rad) * 30 * s
        draw.line([(px - 4 * s, py - 4 * s), (px + 4 * s, py + 4 * s)], fill=stitch_col, width=2 * s)
        draw.line([(px - 4 * s, py + 4 * s), (px + 4 * s, py - 4 * s)], fill=stitch_col, width=2 * s)

    # 3. 둥글넓적한 머리 (Head)
    draw.ellipse([cx - 95 * s, cy - 120 * s, cx + 95 * s, cy + 15 * s], fill=body_fill, outline=stitch_col, width=4 * s)

    # 머리 윗부분 봉제 꿰맨 자국 (Top seam stitches)
    draw.line([(cx, cy - 120 * s), (cx, cy - 80 * s)], fill=stitch_col, width=3 * s)
    draw.line([(cx - 8 * s, cy - 105 * s), (cx + 8 * s, cy - 105 * s)], fill=stitch_col, width=2 * s)
    draw.line([(cx - 8 * s, cy - 90 * s), (cx + 8 * s, cy - 90 * s)], fill=stitch_col, width=2 * s)

    # 4. 단추 눈 (Mismatched Button Eyes - 인형 느낌 극대화!)
    # 왼쪽 눈: 동그란 검정 단추 + 십자 구멍
    bx1, by1 = cx - 42 * s, cy - 50 * s
    draw.ellipse([bx1 - 16 * s, by1 - 16 * s, bx1 + 16 * s, by1 + 16 * s], fill=(30, 25, 25), outline=(15, 10, 10), width=2 * s)
    draw.ellipse([bx1 - 12 * s, by1 - 12 * s, bx1 + 12 * s, by1 + 12 * s], fill=(45, 40, 40))
    # 단추 실구멍
    draw.ellipse([bx1 - 5 * s, by1 - 5 * s, bx1 + 5 * s, by1 + 5 * s], fill=(235, 228, 218))
    draw.line([(bx1 - 6 * s, by1 - 6 * s), (bx1 + 6 * s, by1 + 6 * s)], fill=(200, 60, 60), width=3 * s)
    draw.line([(bx1 - 6 * s, by1 + 6 * s), (bx1 + 6 * s, by1 - 6 * s)], fill=(200, 60, 60), width=3 * s)

    # 오른쪽 눈: 단추 대신 실로 'X' 자만 대충 꿰맨 눈 (가짜 인형 느낌 팍팍!)
    bx2, by2 = cx + 42 * s, cy - 50 * s
    draw.ellipse([bx2 - 16 * s, by2 - 16 * s, bx2 + 16 * s, by2 + 16 * s], fill=(50, 40, 35), outline=(20, 15, 12), width=2 * s)
    draw.line([(bx2 - 10 * s, by2 - 10 * s), (bx2 + 10 * s, by2 + 10 * s)], fill=(230, 220, 190), width=4 * s)
    draw.line([(bx2 - 10 * s, by2 + 10 * s), (bx2 + 10 * s, by2 - 10 * s)], fill=(230, 220, 190), width=4 * s)

    # 5. 분홍 코와 봉제 'ㅅ' 입
    nx, ny = cx, cy - 30 * s
    draw.polygon([(nx - 8 * s, ny - 6 * s), (nx + 8 * s, ny - 6 * s), (nx, ny + 4 * s)], fill=(225, 140, 150))
    # 멍한 'ㅅ' 입
    draw.line([(nx, ny + 4 * s), (nx - 12 * s, ny + 15 * s)], fill=stitch_col, width=3 * s)
    draw.line([(nx, ny + 4 * s), (nx + 12 * s, ny + 15 * s)], fill=stitch_col, width=3 * s)

    # 6. 볼터치 (Blush)
    draw.ellipse([cx - 65 * s, cy - 35 * s, cx - 35 * s, cy - 15 * s], fill=(245, 180, 185, 120))
    draw.ellipse([cx + 35 * s, cy - 35 * s, cx + 65 * s, cy - 15 * s], fill=(245, 180, 185, 120))

    # 7. 짤막한 인형 앞발 2개
    draw.ellipse([cx - 70 * s, cy + 25 * s, cx - 25 * s, cy + 65 * s], fill=body_fill, outline=stitch_col, width=3 * s)
    draw.ellipse([cx + 25 * s, cy + 25 * s, cx + 70 * s, cy + 65 * s], fill=body_fill, outline=stitch_col, width=3 * s)

    # 8. 바닥 발 2개
    draw.ellipse([cx - 95 * s, cy + 85 * s, cx - 40 * s, cy + 120 * s], fill=body_fill, outline=stitch_col, width=3 * s)
    draw.ellipse([cx + 40 * s, cy + 85 * s, cx + 95 * s, cy + 120 * s], fill=body_fill, outline=stitch_col, width=3 * s)

    # 9. 인형 꼬리표 (Price tag / Fabric label "FAKE" or 바코드 텍)
    tag_x, tag_y = cx + 85 * s, cy + 40 * s
    draw.polygon(
        [(tag_x, tag_y), (tag_x + 38 * s, tag_y - 12 * s), (tag_x + 50 * s, tag_y + 12 * s), (tag_x + 12 * s, tag_y + 24 * s)],
        fill=(250, 248, 240),
        outline=(160, 150, 140),
    )
    draw.line([(tag_x + 15 * s, tag_y), (tag_x + 35 * s, tag_y - 6 * s)], fill=(180, 50, 50), width=2 * s)

    return downsample(img)


# ---------------------------------------------------------------------------
# 3. 여행 가방 / 당근 가방 (item-bag.png)
# ---------------------------------------------------------------------------
def draw_bag():
    s = SCALE
    img = create_supersampled_image()
    draw = ImageDraw.Draw(img)

    cx, cy = 256 * s, 290 * s

    # 바닥 그림자
    draw.ellipse([cx - 120 * s, cy + 90 * s, cx + 120 * s, cy + 130 * s], fill=(40, 30, 20, 50))

    # 가방 가죽 색상 (따뜻한 캐러멜 브라운 가죽)
    leather_dark = (145, 82, 38)
    leather_mid = (188, 114, 56)
    leather_light = (215, 142, 82)
    buckle_gold = (245, 195, 65)
    border_col = (95, 50, 20)

    # 1. 가방 본체 (Backpack body)
    draw.rounded_rectangle(
        [cx - 95 * s, cy - 80 * s, cx + 95 * s, cy + 95 * s],
        radius=28 * s,
        fill=leather_mid,
        outline=border_col,
        width=4 * s,
    )
    # 가방 하단 보강 가죽 (Bottom leather patch)
    draw.rounded_rectangle(
        [cx - 93 * s, cy + 30 * s, cx + 93 * s, cy + 93 * s],
        radius=24 * s,
        fill=leather_dark,
        outline=border_col,
        width=3 * s,
    )

    # 2. 당근 주머니 (Carrot side pocket on left)
    # 삐져나온 당근 (Carrot peeking out!)
    car_x, car_y = cx - 55 * s, cy - 100 * s
    # 당근 잎 (Green leaves)
    draw.polygon([(car_x, car_y - 15 * s), (car_x - 18 * s, car_y - 55 * s), (car_x - 5 * s, car_y - 30 * s)], fill=(65, 155, 60))
    draw.polygon([(car_x, car_y - 15 * s), (car_x, car_y - 65 * s), (car_x + 8 * s, car_y - 30 * s)], fill=(85, 185, 75))
    draw.polygon([(car_x, car_y - 15 * s), (car_x + 22 * s, car_y - 50 * s), (car_x + 10 * s, car_y - 25 * s)], fill=(65, 155, 60))
    # 주황 당근 몸통
    draw.polygon([(car_x - 18 * s, car_y - 15 * s), (car_x + 18 * s, car_y - 15 * s), (car_x + 5 * s, car_y + 35 * s), (car_x - 10 * s, car_y + 35 * s)], fill=(245, 120, 25), outline=(180, 70, 10), width=2 * s)
    # 당근 가로 주름선
    draw.line([(car_x - 10 * s, car_y), (car_x + 6 * s, car_y)], fill=(210, 90, 15), width=2 * s)
    draw.line([(car_x - 6 * s, car_y + 15 * s), (car_x + 8 * s, car_y + 15 * s)], fill=(210, 90, 15), width=2 * s)

    # 3. 가방 덮개 플랩 (Front Flap)
    draw.rounded_rectangle(
        [cx - 98 * s, cy - 85 * s, cx + 98 * s, cy + 10 * s],
        radius=26 * s,
        fill=leather_light,
        outline=border_col,
        width=4 * s,
    )

    # 4. 가죽 스트랩 2줄 (Two vertical straps)
    for sx in (cx - 48 * s, cx + 48 * s):
        draw.rounded_rectangle(
            [sx - 10 * s, cy - 85 * s, sx + 10 * s, cy + 70 * s],
            radius=6 * s,
            fill=leather_dark,
            outline=border_col,
            width=2 * s,
        )
        # 금색 버클 (Gold buckles)
        draw.rounded_rectangle(
            [sx - 14 * s, cy + 5 * s, sx + 14 * s, cy + 32 * s],
            radius=4 * s,
            fill=buckle_gold,
            outline=border_col,
            width=2 * s,
        )
        draw.rectangle([sx - 7 * s, cy + 12 * s, sx + 7 * s, cy + 25 * s], fill=(30, 20, 15))
        draw.line([(sx, cy + 6 * s), (sx, cy + 30 * s)], fill=buckle_gold, width=3 * s)

    # 5. 가방 손잡이 (Top handle)
    draw.arc([cx - 40 * s, cy - 125 * s, cx + 40 * s, cy - 65 * s], start=180, end=360, fill=leather_dark, width=10 * s)
    draw.arc([cx - 40 * s, cy - 125 * s, cx + 40 * s, cy - 65 * s], start=180, end=360, fill=border_col, width=2 * s)

    # 6. 앞주머니 (Front small pouch)
    draw.rounded_rectangle(
        [cx - 40 * s, cy + 32 * s, cx + 40 * s, cy + 85 * s],
        radius=10 * s,
        fill=leather_light,
        outline=border_col,
        width=3 * s,
    )
    # 주머니 단추
    draw.ellipse([cx - 7 * s, cy + 45 * s, cx + 7 * s, cy + 59 * s], fill=buckle_gold, outline=border_col, width=2 * s)

    return downsample(img)


# ---------------------------------------------------------------------------
# 4. 토끼 집 (item-house.png)
# ---------------------------------------------------------------------------
def draw_house():
    s = SCALE
    img = create_supersampled_image()
    draw = ImageDraw.Draw(img)

    cx, cy = 256 * s, 275 * s

    # 바닥 그림자
    draw.ellipse([cx - 170 * s, cy + 115 * s, cx + 170 * s, cy + 165 * s], fill=(40, 30, 20, 60))

    # 원목 색상 (Cozy warm cedar & birch)
    wood_wall = (235, 195, 145)
    wood_shadow = (195, 155, 110)
    wood_line = (150, 105, 65)
    roof_red = (198, 78, 65)  # 따뜻한 테라코타 빨강 지붕
    roof_dark = (155, 55, 45)
    border_col = (85, 50, 28)

    # 1. 집 본체 사각 벽면 (House base)
    hw, hh = 145 * s, 115 * s
    draw.rounded_rectangle(
        [cx - hw, cy - 20 * s, cx + hw, cy + 125 * s],
        radius=16 * s,
        fill=wood_wall,
        outline=border_col,
        width=4 * s,
    )

    # 벽면 통나무 판자 줄무늬 (Wood plank vertical lines)
    for px in range(int(cx - hw + 36 * s), int(cx + hw), int(36 * s)):
        draw.line([(px, cy - 18 * s), (px, cy + 123 * s)], fill=wood_line, width=2 * s)

    # 2. 아늑한 아치형 문 (Cozy arch entryway where bunny sits)
    door_w, door_h = 75 * s, 95 * s
    # 문 안쪽 어두운 공간
    draw.rounded_rectangle(
        [cx - door_w, cy + 30 * s - door_h // 2, cx + door_w, cy + 125 * s],
        radius=40 * s,
        fill=(45, 30, 22),
        outline=border_col,
        width=4 * s,
    )
    # 문 안쪽 따스한 전등 빛 (Warm glow inside house)
    draw.ellipse([cx - 50 * s, cy + 15 * s, cx + 50 * s, cy + 90 * s], fill=(120, 80, 45, 160))

    # 문 바닥 짚 풀 (Straw at doorway)
    for i in range(25):
        import random
        r = random.Random(i + 10)
        sx = cx + r.uniform(-65 * s, 65 * s)
        sy = cy + 120 * s + r.uniform(-5 * s, 5 * s)
        draw.line([(sx, sy), (sx + r.uniform(-15 * s, 15 * s), sy - r.uniform(8 * s, 18 * s))], fill=(240, 210, 120), width=3 * s)

    # 3. 삼각 박공 벽면 (Triangle Gable wall)
    draw.polygon([(cx - hw - 5 * s, cy - 20 * s), (cx, cy - 130 * s), (cx + hw + 5 * s, cy - 20 * s)], fill=wood_wall, outline=border_col)
    # 다락방 작은 동그란 창문 (Attic round window)
    win_y = cy - 65 * s
    draw.ellipse([cx - 24 * s, win_y - 24 * s, cx + 24 * s, win_y + 24 * s], fill=(255, 245, 210), outline=border_col, width=3 * s)
    draw.line([(cx - 24 * s, win_y), (cx + 24 * s, win_y)], fill=border_col, width=3 * s)
    draw.line([(cx, win_y - 24 * s), (cx, win_y + 24 * s)], fill=border_col, width=3 * s)

    # 4. 귀여운 세모 지붕 (Cozy roof with overhang)
    roof_thick = 28 * s
    # 왼쪽 지붕 날개
    draw.polygon(
        [(cx, cy - 145 * s), (cx - hw - 30 * s, cy - 10 * s), (cx - hw - 30 * s, cy + 10 * s), (cx, cy - 125 * s)],
        fill=roof_red,
        outline=border_col,
    )
    # 오른쪽 지붕 날개
    draw.polygon(
        [(cx, cy - 145 * s), (cx + hw + 30 * s, cy - 10 * s), (cx + hw + 30 * s, cy + 10 * s), (cx, cy - 125 * s)],
        fill=roof_dark,
        outline=border_col,
    )
    # 지붕 처마 끝 둥근 마감
    draw.line([(cx, cy - 145 * s), (cx - hw - 30 * s, cy - 10 * s)], fill=(255, 140, 130), width=6 * s)
    draw.line([(cx, cy - 145 * s), (cx + hw + 30 * s, cy - 10 * s)], fill=(225, 100, 90), width=6 * s)

    # 5. 굴뚝 (Tiny Chimney with smoke)
    chim_x = cx + 80 * s
    draw.rectangle([chim_x - 16 * s, cy - 130 * s, chim_x + 16 * s, cy - 70 * s], fill=(160, 60, 50), outline=border_col, width=3 * s)
    draw.rounded_rectangle([chim_x - 20 * s, cy - 138 * s, chim_x + 20 * s, cy - 124 * s], radius=3 * s, fill=(130, 45, 38), outline=border_col, width=2 * s)
    # 연기 방울 (Smoke puffs)
    draw.ellipse([chim_x - 8 * s, cy - 155 * s, chim_x + 8 * s, cy - 141 * s], fill=(240, 240, 245, 180))
    draw.ellipse([chim_x + 2 * s, cy - 175 * s, chim_x + 22 * s, cy - 157 * s], fill=(240, 240, 245, 140))
    draw.ellipse([chim_x + 10 * s, cy - 198 * s, chim_x + 36 * s, cy - 176 * s], fill=(240, 240, 245, 100))

    # 6. 문 위 당근 간판 (Carrot nameplate sign)
    plate_y = cy + 18 * s
    draw.rounded_rectangle([cx - 36 * s, plate_y - 12 * s, cx + 36 * s, plate_y + 12 * s], radius=5 * s, fill=(245, 235, 215), outline=border_col, width=2 * s)
    # 간판 속 미니 당근 아이콘
    draw.polygon([(cx - 12 * s, plate_y), (cx + 8 * s, plate_y - 7 * s), (cx + 8 * s, plate_y + 7 * s)], fill=(245, 120, 25))
    draw.line([(cx + 8 * s, plate_y), (cx + 16 * s, plate_y - 4 * s)], fill=(65, 160, 55), width=2 * s)
    draw.line([(cx + 8 * s, plate_y), (cx + 16 * s, plate_y + 4 * s)], fill=(65, 160, 55), width=2 * s)

    return downsample(img)


def main():
    print("Generating item sprites...")
    hay = draw_hay()
    doll = draw_doll()
    bag = draw_bag()
    house = draw_house()

    items = {
        "item-hay.png": hay,
        "item-doll.png": doll,
        "item-bag.png": bag,
        "item-house.png": house,
    }

    targets = [ASSET_DIR, SITE_ASSET_DIR, NATIVE_ASSET_DIR]

    for dir_path in targets:
        dir_path.mkdir(parents=True, exist_ok=True)
        for name, img in items.items():
            out_file = dir_path / name
            img.save(out_file, "PNG", optimize=True)
            print(f"Saved {name} -> {out_file} ({img.width}x{img.height})")

    print("All item assets generated successfully!")


if __name__ == "__main__":
    main()
