"""Makes every logo picture Argos ships from the two source pictures in docs/media/brand (needs Pillow).

Usage: python3 -I tools/make-brand.py

- argos-mark.png, the mark alone in flat colours (it reads at 16 px): app.ico (the program, window, taskbar and tray
  icon) and the installer's small picture.
- argos-logo.png, the mark and the name: the logo in the window's sidebar (logo.png), the README's logo for dark and
  light pages, the installer's side picture and the repository's social preview.
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
BRAND = ROOT / "docs" / "media" / "brand"
ASSETS = ROOT / "src" / "Argos" / "Ui" / "Assets"
MEDIA = ROOT / "docs" / "media"
WINDOW_BROWN = (0x2E, 0x27, 0x26)  # ThemeColors.Background
INSTALLER_BROWN = (0x2C, 0x25, 0x25)  # the install wizard's own ground
NAME_ON_LIGHT = (0x33, 0x22, 0x1A)  # the mark's dark brown: the name on light pages
ICON_SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]  # AppIconArt.Sizes
SIDEBAR_HEIGHT = 144  # six times the 24 px the sidebar shows it at (MainWindow.xaml)


def trimmed(image):
    return image.crop(image.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())


def scaled(image, width):
    return image.resize((width, max(1, round(image.height * width / image.width))), Image.LANCZOS)


def square(image, side, fill=0.94):
    """The picture centred on a clear square, its longer side at `fill` of the square's."""
    small = scaled(image, round(side * fill * image.width / max(image.size)))
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.alpha_composite(small, ((side - small.width) // 2, (side - small.height) // 2))
    return canvas


def name_start(logo):
    """Where the name begins: the first filled column after the clear gap that follows the mark."""
    alpha = logo.getchannel("A")
    filled = [alpha.crop((x, 0, x + 1, logo.height)).getbbox() is not None for x in range(logo.width)]
    gap = next(x for x in range(1, logo.width) if filled[x - 1] and not filled[x])
    return next(x for x in range(gap, logo.width) if filled[x])


def recoloured(image, rgb):
    out = Image.new("RGBA", image.size, rgb + (0,))
    out.putalpha(image.getchannel("A"))
    return out


def main():
    mark = trimmed(Image.open(BRAND / "argos-mark.png").convert("RGBA"))
    logo = trimmed(Image.open(BRAND / "argos-logo.png").convert("RGBA"))
    start = name_start(logo)
    logo_mark = trimmed(logo.crop((0, 0, start, logo.height)))
    name = trimmed(logo.crop((start, 0, logo.width, logo.height)))

    logo.resize((round(logo.width * SIDEBAR_HEIGHT / logo.height), SIDEBAR_HEIGHT), Image.LANCZOS).save(ASSETS / "logo.png")
    square(mark, 1024).save(ROOT / "src" / "Argos" / "app.ico", sizes=[(s, s) for s in ICON_SIZES])
    square(mark, 116, fill=0.9).save(ROOT / "installer" / "wizard-small.png")

    # The installer's side picture: the logo's mark above the name, on the wizard's brown.
    side = Image.new("RGBA", (328, 628), INSTALLER_BROWN + (255,))
    top_mark, below = scaled(logo_mark, 150), scaled(name, 236)
    top = (side.height - top_mark.height - 28 - below.height) // 2
    side.alpha_composite(top_mark, ((side.width - top_mark.width) // 2, top))
    side.alpha_composite(below, ((side.width - below.width) // 2, top + top_mark.height + 28))
    side.convert("RGB").save(ROOT / "installer" / "wizard.png")

    # The README: the logo as drawn (a light name) for dark pages, the name in dark brown for light pages.
    logo.save(MEDIA / "logo-dark.png")
    light = logo.copy()
    light.paste(recoloured(logo.crop((start, 0, logo.width, logo.height)), NAME_ON_LIGHT), (start, 0))
    light.save(MEDIA / "logo-light.png")

    preview = Image.new("RGBA", (1280, 640), WINDOW_BROWN + (255,))
    big = scaled(logo, 900)
    preview.alpha_composite(big, ((preview.width - big.width) // 2, (preview.height - big.height) // 2))
    preview.convert("RGB").save(MEDIA / "social-preview.png")

    for path in [ASSETS / "logo.png", ROOT / "installer" / "wizard-small.png",
                 ROOT / "installer" / "wizard.png", MEDIA / "logo-dark.png", MEDIA / "logo-light.png",
                 MEDIA / "social-preview.png"]:
        with Image.open(path) as made:
            print(f"{path.relative_to(ROOT)}: {made.width}x{made.height}")
    with Image.open(ROOT / "src" / "Argos" / "app.ico") as icon:
        print(f"src/Argos/app.ico: {sorted(icon.info['sizes'])}")


if __name__ == "__main__":
    main()
