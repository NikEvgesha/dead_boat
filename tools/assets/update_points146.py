"""Move the two existing exterior pickup transforms; retain all prefab references."""
from pathlib import Path
import re

paths = {
    'House1_2_RustCourtyard': 'House1_2_LocationVariant',
    'House1_3_GreenCourtyard': 'House1_3_LocationVariant',
}
for line in Path('Temp/points146.txt').read_text().splitlines():
    name, *points = line.split('|')
    path = Path('Assets/Prefabs/Workshop/Locations') / (paths[name] + '.prefab')
    raw = path.read_bytes()
    text = raw.decode('utf-8-sig').replace('\r\n', '\n')
    for ident, point in zip(('1106624075444175402', '3339743795788123962'), points):
        x, y, z = map(float, point.split(','))
        xyz = (x - 58.9, y + 1.07, z + 5.9)
        pattern = rf'(^--- !u!4 &{ident}\n(?:(?!^---).)*?^  m_LocalPosition: )\{{[^\n]+\}}'
        text, count = re.subn(pattern, lambda m: m[1] + f'{{x: {xyz[0]:.4f}, y: {xyz[1]:.4f}, z: {xyz[2]:.4f}}}', text, flags=re.M | re.S)
        assert count == 1, (name, ident, count)
    if b'\r\n' in raw:
        text = text.replace('\n', '\r\n')
    path.write_bytes(text.encode('utf-8'))
    print(name + ': two exterior points updated; references retained')
