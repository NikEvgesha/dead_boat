"""Build labelled sheets from Unity renders and verified event spawn eligibility."""
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageOps

ROOT = Path('Docs/locations/previews/event-loot252')

def main():
    labels = [s.split('. ', 1)[1].split(' | ')[0] for s in (ROOT/'labels.txt').read_text(encoding='utf-8').splitlines()]
    paths = (ROOT/'index.txt').read_text().splitlines()
    eligible = set((ROOT/'eligible-paths.txt').read_text().splitlines())
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 21)
    heading = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 30)
    for page, indices, title in [
        ('current-01', range(16), 'Собираемые предметы — 1'),
        ('current-02', range(16, 31), 'Собираемые предметы — 2'),
        ('current-03', range(31, 51), 'Ивентовые сокровища и топливо'),
    ]:
        indices = [i for i in indices if paths[i] in eligible]
        cw, ch, cols = 350, 325, 4
        out = Image.new('RGB', (cw*cols, 75+((len(indices)+cols-1)//cols)*ch), (244, 246, 248))
        draw = ImageDraw.Draw(out)
        draw.text((25, 22), title, font=heading, fill=(36, 47, 59))
        for j, i in enumerate(indices):
            x, y = j % cols*cw, 75+j//cols*ch
            im = Image.open(ROOT/f'item-{i+1:02}.png').convert('RGB')
            bg = Image.new('RGB', im.size, im.getpixel((0, 0)))
            box = ImageChops.difference(im, bg).convert('L').point(lambda v: 255 if v > 18 else 0).getbbox()
            if box:
                im = im.crop(box)
            im = ImageOps.contain(im, (cw-44, ch-85))
            out.paste(im, (x+(cw-im.width)//2, y+10+(ch-85-im.height)//2))
            lines = ['']
            for word in labels[i].split():
                line = (lines[-1]+' '+word).strip()
                if draw.textlength(line, font=font) > cw-24:
                    lines.append(word)
                else:
                    lines[-1] = line
            for k, line in enumerate(lines):
                draw.text((x+(cw-draw.textlength(line, font=font))/2, y+ch-66+k*25), line, font=font, fill=(28, 39, 51))
            draw.line((x+12, y+ch-8, x+cw-12, y+ch-8), fill=(213, 219, 225))
        out.save(ROOT/f'{page}.png')
    print(f'Eligible loot sheets: {len(eligible)} items')

if __name__ == '__main__':
    main()
