"""Export the saved owner-edited hospital and empty fisherman house safely."""
import re
from pathlib import Path
import export_workshop_prefabs as exporter

def parse(path):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',
        Path(path).read_text(encoding='utf-8-sig'), re.M | re.S)}

before = parse('Library/WorkshopBackups/DB-ON-078/OwnerSaved.unity')
source = parse('Assets/Prefabs/Locations/Bilding/Hospital.prefab')
# Resolved by PrefabUtility.GetCorrespondingObjectFromSource +
# AssetDatabase.TryGetGUIDAndLocalFileIdentifier; Hospital inherits nested
# store points, so its stripped transforms do not repeat the buyPoint names.
buy_ids = {3373730374390058100, 4795506301996068751}
guid = re.search(r'^guid: (\w+)', Path('Assets/Prefabs/Locations/Bilding/Hospital.prefab.meta').read_text(), re.M)[1]
instance = next(key for key, (kind, body) in before.items()
                if kind == 1001 and f'm_SourcePrefab: {{fileID: 100100000, guid: {guid}' in body)
assert all(f'target: {{fileID: {key}, guid: {guid}' in exporter.docs[instance][1] for key in buy_ids)

def without_buy_positions(body):
    pattern = r'^    - target: \{fileID: (-?\d+), guid: '+guid+r', type: 3\}\n      propertyPath: (.+)\n      value: .*\n      objectReference: .*\n'
    return re.sub(pattern, lambda m: '' if int(m[1]) in buy_ids and m[2].startswith('m_LocalPosition.') else m[0], body, flags=re.M)

assert without_buy_positions(before[instance][1]) == without_buy_positions(exporter.docs[instance][1]), 'Unrelated hospital source field changed'

paths = [('06_AnimalMerge', 'Assets/Prefabs/Workshop/Hospital/Hospital_Stone_01.prefab', 'Hospital_Stone_01'),
         ('02_Armory', 'Assets/Prefabs/Workshop/Fisher/Fisher_Empty_01.prefab', 'Fisher_Empty_01')]
allowed = set()
for name, path, renamed in paths:
    dest = Path(path)
    exporter.export(name, dest)
    allowed.update(parse(dest))
    dest.write_text(dest.read_text().replace(f'  m_Name: {name}\n', f'  m_Name: {renamed}\n', 1), encoding='utf-8', newline='\n')
exporter.export('Castle_Services_Prototype', Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
changed = [key for key in before if before[key] != exporter.docs.get(key)]
assert all(key in allowed for key in changed), 'Unrelated scene object changed'
report = f'PASS: {len(changed)} existing changed objects belong only to hospital/fisher; other owner scene objects unchanged. Hospital gameplay fields preserved except two buyPoint positions. All prefab local references resolved.\n'
print(report)
with Path('Docs/assets/examples/HOSPITAL_WINDOWS_FISHER_VALIDATION.txt').open('a', encoding='utf-8') as out:
    out.write('\n'+report)
