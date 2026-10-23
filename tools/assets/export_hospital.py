"""Export the saved hospital graph, retaining unresolved gameplay script fields."""
from pathlib import Path
import re
import export_workshop_prefabs as exporter

def parse(path):
    text = Path(path).read_text(encoding='utf-8-sig')
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)', text, re.M | re.S)}

before = parse('Library/WorkshopBackups/DB-ON-077/OwnerSaved.unity')
after = exporter.docs
# Source-prefab instance serialization must remain exactly as saved by the owner.
hospital_guid = re.search(r'^guid: (\w+)', Path('Assets/Prefabs/Locations/Bilding/Hospital.prefab.meta').read_text(), re.M)[1]
sources = [key for key, (kind, body) in before.items()
           if kind == 1001 and f'm_SourcePrefab: {{fileID: 100100000, guid: {hospital_guid}' in body]
assert len(sources) == 1
assert all(before[key] == after[key] for key in sources), 'Gameplay source overrides changed'

dest = Path('Assets/Prefabs/Workshop/Hospital/Hospital_Stone_01.prefab')
exporter.export('06_AnimalMerge', dest)
saved = dest.read_text()
saved = saved.replace('  m_Name: 06_AnimalMerge\n', '  m_Name: Hospital_Stone_01\n', 1)
dest.write_text(saved, encoding='utf-8', newline='\n')
exporter.export('Castle_Services_Prototype', Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
print('PASS: Hospital source prefab and all gameplay overrides unchanged versus owner checkpoint.')
