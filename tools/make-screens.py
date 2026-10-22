"""Makes the README's screenshots (docs/media/screens) with Pillow, and FFmpeg for the sample clips.

Usage:
  python3 -I tools/make-screens.py samples     # draws the made-up clips and screenshots in docs/media/screens/samples
  python3 -I tools/make-screens.py webp <dir>  # turns the pages CI drew (<dir>/<lang>/<page>.png) into the README's pictures

The pages are drawn on Windows by Argos itself (.github/workflows/screens.yml runs Argos.exe --render) over these
samples: drawn landscapes with a game-like HUD, so no real game is in the README.
"""
import math
import random
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
SCREENS = ROOT / "docs" / "media" / "screens"
SAMPLES = SCREENS / "samples"
FONT = ROOT / "src" / "Argos" / "Ui" / "Assets" / "Fonts" / "Changa-SemiBold.ttf"
W, H = 1280, 720
PAN = 240  # how far a clip's camera moves sideways, in pixels
README_SIZE = (1320, 840)  # the pages are drawn at 1650x1050 (150%); the README shows them at 120%
LANGUAGES = ["en", "es", "ar", "de", "pt-BR", "ru", "zh-Hans"]
PAGES = ["status", "library", "editor", "share", "smart-save", "accounts"]


def rgb(code):
    return tuple(int(code[i:i + 2], 16) for i in (1, 3, 5))


def mix(a, b, t):
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def sky(width, stops):
    """A vertical gradient through (position, colour) stops, positions from 0 (top) to 1 (bottom)."""
    column = Image.new("RGB", (1, H))
    for y in range(H):
        t = y / (H - 1)
        for (p0, c0), (p1, c1) in zip(stops, stops[1:]):
            if t <= p1:
                column.putpixel((0, y), mix(rgb(c0), rgb(c1), (t - p0) / (p1 - p0)))
                break
    return column.resize((width, H)).convert("RGBA")


def glow(image, centre, radius, colour, strength=1.0):
    """A sun or moon with a soft halo."""
    layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    cx, cy = centre
    for r, alpha in [(radius * 3.4, 40), (radius * 2.1, 80)]:
        d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=rgb(colour) + (round(alpha * strength),))
    layer = layer.filter(ImageFilter.GaussianBlur(radius * 0.9))
    ImageDraw.Draw(layer).ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=rgb(colour) + (255,))
    image.alpha_composite(layer)


def ridge(width, base, height, seed, sharp):
    """A skyline: sharp peaks (mountains) or smooth swells (hills, dunes)."""
    rnd = random.Random(seed)
    waves = [(height * rnd.uniform(0.45, 1.0) / k, rnd.uniform(0.7, 1.3) * k * 2.6 * math.pi / W, rnd.uniform(0, 2 * math.pi))
             for k in range(1, 6)]
    shape = (lambda v: 1 - abs(v)) if sharp else (lambda v: 0.5 + 0.5 * v)
    return [base - sum(a * shape(math.sin(f * x + p)) for a, f, p in waves) for x in range(width + 1)]


def land(image, layers, sharp, edge=None):
    """Layers of land, far to near; `edge` lights the farthest skyline (snow)."""
    d = ImageDraw.Draw(image)
    for i, (base, height, colour) in enumerate(layers):
        ys = ridge(image.width, H * base, height, seed=round(base * 1000) * 7919 + height, sharp=sharp)
        d.polygon([(0, H)] + list(enumerate(ys)) + [(image.width, H)], fill=rgb(colour))
        if edge and i == 0:
            line = Image.new("RGBA", image.size, (0, 0, 0, 0))
            ImageDraw.Draw(line).line(list(enumerate(ys)), fill=rgb(edge) + (200,), width=2)
            image.alpha_composite(line)


def stars(image, count, seed):
    rnd = random.Random(seed)
    layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for _ in range(count):
        x, y, r = rnd.uniform(0, image.width), rnd.uniform(0, H * 0.62), rnd.choice([0.7, 0.9, 1.1, 1.5])
        d.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, rnd.randint(110, 255)))
    image.alpha_composite(layer)


def city(image, base, low, high, colour, density, seed):
    rnd = random.Random(seed)
    d = ImageDraw.Draw(image)
    lit = Image.new("RGBA", image.size, (0, 0, 0, 0))
    windows = ImageDraw.Draw(lit)
    lights = [rgb("#FFD36E"), rgb("#7FE8FF"), rgb("#FF8BD1")]
    x = -20
    while x < image.width:
        w, h = rnd.randint(46, 120), rnd.randint(low, high)
        top = H * base - h
        d.rectangle((x, top, x + w, H), fill=rgb(colour))
        for wy in range(int(top) + 10, int(H * base) - 8, 15):
            for wx in range(x + 7, x + w - 9, 13):
                if rnd.random() < density:
                    windows.rectangle((wx, wy, wx + 5, wy + 8), fill=rnd.choice(lights) + (rnd.randint(150, 240),))
        x += w + rnd.randint(3, 12)
    image.alpha_composite(lit)


def synth_sun(image, centre, radius):
    """A striped retro sun, yellow to pink."""
    layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    cx, cy = centre
    for y in range(cy - radius, cy + radius):
        half = math.sqrt(max(0, radius ** 2 - (y - cy) ** 2))
        stripe = y > cy and (y - cy) % 22 < 4 + (y - cy) // 14
        if not stripe:
            d.line((cx - half, y, cx + half, y), fill=mix(rgb("#FFE36E"), rgb("#FF4FA3"), (y - cy + radius) / (2 * radius)) + (255,))
    image.alpha_composite(layer.filter(ImageFilter.GaussianBlur(radius * 0.25)))
    image.alpha_composite(layer)


def hud(kind):
    """A game's on-screen display: a shooter's (crosshair, health, ammo, minimap) or a racer's (speed, lap, place)."""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    big, small = ImageFont.truetype(str(FONT), 56), ImageFont.truetype(str(FONT), 24)
    panel, white, dim = (12, 10, 16, 130), (255, 255, 255, 240), (255, 255, 255, 170)
    if kind == "shooter":
        cx, cy = W // 2, H // 2
        for x0, y0, x1, y1 in [(cx - 15, cy - 1, cx - 6, cy + 1), (cx + 6, cy - 1, cx + 15, cy + 1),
                               (cx - 1, cy - 15, cx + 1, cy - 6), (cx - 1, cy + 6, cx + 1, cy + 15)]:
            d.rectangle((x0 - 1, y0 - 1, x1 + 1, y1 + 1), fill=(0, 0, 0, 150))
            d.rectangle((x0, y0, x1, y1), fill=white)
        d.rounded_rectangle((36, H - 108, 316, H - 36), radius=12, fill=panel)
        d.text((60, H - 72), "100", font=big, fill=white, anchor="lm")
        d.rounded_rectangle((170, H - 77, 292, H - 67), radius=5, fill=(255, 255, 255, 50))
        d.rounded_rectangle((170, H - 77, 292, H - 67), radius=5, fill=(120, 232, 160, 235))
        d.rounded_rectangle((W - 276, H - 108, W - 36, H - 36), radius=12, fill=panel)
        d.text((W - 252, H - 72), "24", font=big, fill=white, anchor="lm")
        d.text((W - 170, H - 66), "/ 72", font=small, fill=dim, anchor="lm")
        d.ellipse((36, 36, 196, 196), fill=panel, outline=(255, 255, 255, 90), width=2)
        d.polygon([(116, 104), (106, 130), (116, 124), (126, 130)], fill=white)
        d.rounded_rectangle((W // 2 - 60, 26, W // 2 + 60, 74), radius=12, fill=panel)
        d.text((W // 2, 50), "1:42", font=small, fill=white, anchor="mm")
    else:
        d.rounded_rectangle((W - 300, H - 128, W - 36, H - 36), radius=14, fill=panel)
        d.text((W - 276, H - 82), "214", font=ImageFont.truetype(str(FONT), 72), fill=white, anchor="lm")
        d.text((W - 120, H - 66), "km/h", font=small, fill=dim, anchor="lm")
        d.rounded_rectangle((36, 30, 200, 78), radius=12, fill=panel)
        d.text((118, 54), "LAP 2/3", font=small, fill=white, anchor="mm")
        d.rounded_rectangle((W - 150, 30, W - 36, 78), radius=12, fill=panel)
        d.text((W - 93, 54), "1st", font=small, fill=(255, 211, 110, 255), anchor="mm")
    return layer


def ember_peaks(width):
    image = sky(width, [(0, "#261633"), (0.35, "#6A2C5A"), (0.62, "#E0646A"), (0.78, "#FFB27A"), (1, "#FFD9A0")])
    glow(image, (round(W * 0.68), round(H * 0.57)), 60, "#FFE6B0")
    land(image, [(0.72, 150, "#A5527A"), (0.82, 160, "#5E2A4E"), (0.93, 120, "#2E1630"), (1.03, 90, "#160B1A")], sharp=True)
    return image


def frostline(width):
    image = sky(width, [(0, "#070B1F"), (0.45, "#14244F"), (0.75, "#3B5B94"), (1, "#8DB0DA")])
    stars(image, 240, seed=7)
    glow(image, (round(W * 0.8), round(H * 0.18)), 34, "#F4F7FF", strength=0.8)
    land(image, [(0.74, 190, "#6F86B5"), (0.85, 150, "#3E5486"), (0.95, 120, "#1C2A4C"), (1.03, 80, "#0D1428")],
         sharp=True, edge="#DCE8FF")
    return image


def night_runner(width):
    image = sky(width, [(0, "#07061A"), (0.5, "#1B0F3A"), (0.8, "#4A1A5C"), (1, "#B33B78")])
    stars(image, 90, seed=3)
    synth_sun(image, (round(W * 0.55), round(H * 0.6)), 150)
    city(image, 0.86, 120, 300, "#1A1235", 0.16, seed=11)
    city(image, 1.0, 170, 420, "#0C0918", 0.11, seed=12)
    d = ImageDraw.Draw(image)
    d.rectangle((0, H * 0.9, width, H), fill=rgb("#0A0812"))
    road = Image.new("RGBA", image.size, (0, 0, 0, 0))
    ImageDraw.Draw(road).line((0, H * 0.9, width, H * 0.9), fill=rgb("#FF4FA3") + (255,), width=4)
    image.alpha_composite(road.filter(ImageFilter.GaussianBlur(6)))
    image.alpha_composite(road)
    return image


def dune_riders(width):
    image = sky(width, [(0, "#2C7BC9"), (0.55, "#8CC6EE"), (0.8, "#F4E3C3"), (1, "#F8E9CF")])
    glow(image, (round(W * 0.2), round(H * 0.17)), 40, "#FFF7D6", strength=1.3)
    land(image, [(0.72, 70, "#E8C08A"), (0.81, 90, "#D9A15F"), (0.92, 100, "#C0813F"), (1.03, 90, "#9C6430")], sharp=False)
    return image


def greenvale(width):
    image = sky(width, [(0, "#5FB3E8"), (0.6, "#BFE6F2"), (1, "#F2F7E6")])
    glow(image, (round(W * 0.76), round(H * 0.24)), 46, "#FFF8DA", strength=1.2)
    clouds = Image.new("RGBA", image.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(clouds)
    rnd = random.Random(5)
    for _ in range(9):
        x, y, r = rnd.uniform(0, width), rnd.uniform(H * 0.08, H * 0.35), rnd.uniform(30, 70)
        d.ellipse((x - r * 2, y - r * 0.6, x + r * 2, y + r * 0.6), fill=(255, 255, 255, 170))
    image.alpha_composite(clouds.filter(ImageFilter.GaussianBlur(14)))
    land(image, [(0.68, 80, "#9CC9A0"), (0.78, 100, "#6FAE78"), (0.89, 110, "#3F8A55"), (1.02, 90, "#24603A")], sharp=False)
    return image


def framed(scene, overlay, x=0):
    picture = scene.crop((x, 0, x + W, H))
    if overlay is not None:
        picture.alpha_composite(overlay)
    return picture.convert("RGB")


def clip(scene, overlay, out, seconds=8):
    """A clip of the scene, the camera moving slowly sideways under the HUD, with a silent sound track."""
    with tempfile.TemporaryDirectory() as tmp:
        background, top = Path(tmp) / "background.png", Path(tmp) / "hud.png"
        scene.convert("RGB").save(background)
        (overlay or Image.new("RGBA", (W, H), (0, 0, 0, 0))).save(top)
        move = f"[0]crop={W}:{H}:x='min(t*{PAN / seconds:.3f},{PAN})':y=0[v];[v][1]overlay=0:0,format=yuv420p[out]"
        subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
                        "-loop", "1", "-framerate", "30", "-i", str(background),
                        "-loop", "1", "-framerate", "30", "-i", str(top),
                        "-f", "lavfi", "-i", "anullsrc=r=48000:cl=stereo",
                        "-filter_complex", move, "-map", "[out]", "-map", "2:a", "-t", str(seconds), "-r", "30",
                        "-c:v", "libx264", "-preset", "slow", "-crf", "26", "-c:a", "aac", "-b:a", "48k",
                        "-movflags", "+faststart", str(out)], check=True)


def samples():
    (SAMPLES / "posters").mkdir(parents=True, exist_ok=True)
    shooter, racer = hud("shooter"), hud("racer")
    wide = W + PAN
    peaks, frost = ember_peaks(wide), frostline(wide)
    clips = {"ember-peaks": (peaks, shooter), "night-runner": (night_runner(wide), racer), "frostline": (frost, shooter),
             "greenvale": (greenvale(wide), None)}
    for name, (scene, overlay) in clips.items():
        clip(scene, overlay, SAMPLES / f"{name}.mp4")
        # The clip's thumbnail for the renders (a Windows server's shell makes none): its middle frame.
        framed(scene, overlay, x=PAN // 2).save(SAMPLES / "posters" / f"{name}.jpg", quality=88)
    framed(peaks, shooter, x=PAN // 2).save(SAMPLES / "ember-peaks.png", optimize=True)
    framed(frost, shooter, x=PAN).save(SAMPLES / "frostline.png", optimize=True)
    framed(dune_riders(W), None).save(SAMPLES / "dune-riders.png", optimize=True)
    for path in sorted(p for p in SAMPLES.rglob("*") if p.is_file()):
        print(f"{path.relative_to(ROOT)}: {path.stat().st_size // 1024} KB")


def webp(rendered):
    for lang in LANGUAGES:
        for page in PAGES:
            source = Path(rendered) / lang / f"{page}.png"
            target = SCREENS / lang / f"{page}.webp"
            with Image.open(source) as image:
                image.convert("RGB").resize(README_SIZE, Image.LANCZOS).save(target, quality=90, method=6)
            print(f"{target.relative_to(ROOT)}: {target.stat().st_size // 1024} KB")


if __name__ == "__main__":
    if len(sys.argv) >= 2 and sys.argv[1] == "samples":
        samples()
    elif len(sys.argv) >= 3 and sys.argv[1] == "webp":
        webp(sys.argv[2])
    else:
        sys.exit(__doc__)
