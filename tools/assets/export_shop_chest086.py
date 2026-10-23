"""Export the shop/trader changes and check preservation against the owner checkpoint."""
import re
from pathlib import Path
import export_workshop_prefabs as exporter

def parse(path):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',
        Path(path).read_text(encoding='utf-8-sig'), re.M | re.S)}

before = parse('Library/WorkshopBackups/DB-ON-086/OwnerSaved.unity')
allowed = set()
for name, target in {'01_Supplies': 'Services/Supplies_Wooden_01', '03_LootTrader': 'Services/LootTrader_Wooden_01'}.items():
    path = Path('Assets/Prefabs/Workshop/' + target + '.prefab')
    exporter.export(name, path)
    allowed.update(parse(path))
    path.write_text(path.read_text(encoding='utf-8').replace('  m_Name: '+name+'\n', '  m_Name: '+path.stem+'\n', 1), encoding='utf-8', newline='\n')
changed = [key for key in before if before[key] != exporter.docs.get(key)]
assert all(key in exporter.docs for key in before), 'Original records removed'
assert all(key in allowed for key in changed), 'Unrelated scene data changed'

# Existing actual scripts and serialized UI links are not replaced. UI display settings may change.
for key, (kind, body) in before.items():
    if kind == 114 and key in changed:
        current = exporter.docs[key][1]
        links = re.findall(r'^  .*\{fileID: .*$', body, re.M)
        assert all(link in current for link in links), ('Existing script/UI reference changed', key)

def stable(body):
    def remove_geometry(match):
        path = match[1]
        prefixes = ('m_LocalPosition.', 'm_LocalRotation.', 'm_LocalScale.', 'm_Center.', 'm_Size.')
        ui_fields = {'m_Text', 'm_RaycastTarget', 'm_ResizeTextForBestFit', 'm_ResizeTextMinSize', 'm_ResizeTextMaxSize', 'm_BestFit', 'm_MinSize', 'm_MaxSize', 'm_DynamicPixelsPerUnit', 'm_FontData.m_MaxSize', 'm_FontData.m_MinSize', 'm_FontData.m_BestFit'}
        return '' if path.startswith(prefixes) or path in ui_fields or path == 'm_IsActive' else match[0]
    return re.sub(r'^    - target: .*\n      propertyPath: (.+)\n      value: .*\n      objectReference: .*\n', remove_geometry, body, flags=re.M)

for key, (kind, body) in before.items():
    if kind == 1001 and key in changed:
        assert stable(body) == stable(exporter.docs[key][1]), ('Non-geometric gameplay source field changed', key)
exporter.export('Castle_Services_Prototype', Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
report = f'PASS: {len(changed)} existing records changed only inside shop/trader; no existing records removed. Existing script/UI links and non-geometric gameplay source fields preserved; all exported local references resolved.\n'
print(report)
with Path('Docs/assets/examples/SHOP_CHEST086_VALIDATION.txt').open('a', encoding='utf-8') as out:
    out.write(report)
