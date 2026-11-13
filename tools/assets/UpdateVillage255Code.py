"""Scoped, encoding-preserving patch for the approved village integration."""
from pathlib import Path
import hashlib
import json

root = Path(__file__).resolve().parents[2]
backup = root / 'Library/Village255/integration-backup'
paths = [
    'Assets/Scripts/Locations/LocationSpawnCollection.cs',
    'Assets/Scripts/Locations/LocationSpawnItem.cs',
    'Assets/Online/SharedRunRecovery.cs',
    'Assets/Online/Editor/AvatarNamesProbe.cs',
    'Assets/Scripts/Locations/LocationSpawnCollection.asset',
    'Assets/Scripts/Locations/LocationSpawnCollectionForest.asset',
    'Assets/Prefabs/Locations/Enemy/Village_1.prefab',
    'Assets/Prefabs/Locations/Enemy/Village_2.prefab',
    'Assets/Scenes/VillageWorkshop.unity',
]
assert not backup.exists(), 'Integration backup already exists; inspect before rerunning'
backup.mkdir(parents=True)
manifest = {}
for rel in paths:
    raw = (root / rel).read_bytes()
    dest = backup / rel
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_bytes(raw)
    manifest[rel] = hashlib.sha256(raw).hexdigest()
(backup / 'sha256.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')

p = root / paths[0]
raw = p.read_bytes()
assert raw.count(b'CurrentPoolRevision = 2;') == 1
raw = raw.replace(b'CurrentPoolRevision = 2;', b'CurrentPoolRevision = 3;')
marker = b'    [SerializeField, Min(0)] private int _revisionOnePoolSize;'
raw = raw.replace(marker, marker + b'\r\n    [SerializeField] private List<LocationSpawnItem> _revisionTwoSpawnItems;')
start = raw.index(b'    public void ApplyPoolRevision(int revision)')
end = raw.index(b'\r\n    }', start) + len(b'\r\n    }')
body = '''    public void ApplyPoolRevision(int revision)
    {
        if (revision >= CurrentPoolRevision) return;

        // Village consolidation changes order/count, so retain the complete old
        // pool for revision 2 before applying the earlier prefix migrations.
        bool changed = _revisionTwoSpawnItems != null && _revisionTwoSpawnItems.Count > 0;
        if (changed) spawnItems = new List<LocationSpawnItem>(_revisionTwoSpawnItems);
        if (revision <= 1)
        {
            int poolSize = revision <= 0 ? _legacyPoolSize : _revisionOnePoolSize;
            if (poolSize > 0 && poolSize < spawnItems.Count)
            {
                spawnItems.RemoveRange(poolSize, spawnItems.Count - poolSize);
                changed = true;
            }
        }
        if (changed) OnEnable();
    }'''.replace('\n', '\r\n').encode('ascii')
raw = raw[:start] + body + raw[end:]
assert raw.count(b'return fallback.prefab;') == 1 and raw.count(b'return chosenItem.prefab;') == 1
raw = raw.replace(b'return fallback.prefab;', b'return ResolveBank(fallback, random);')
raw = raw.replace(b'return chosenItem.prefab;', b'return ResolveBank(chosenItem, random);')
end = raw.rfind(b'}')
helper = '''
    private static LocationContentSpawner ResolveBank(LocationSpawnItem item, DeadBoat.Online.RunRandom random)
    {
        // Existing entries consume no extra random value; only the unified
        // village selects between its two placements after the family is chosen.
        if (item.bankAlternativePrefab == null) return item.prefab;
        int bank = random != null ? random.Range(0, 2) : Random.Range(0, 2);
        return bank == 0 ? item.prefab : item.bankAlternativePrefab;
    }
'''.replace('\n', '\r\n').encode('ascii')
p.write_bytes(raw[:end] + helper + raw[end:])

p = root / paths[1]
raw = p.read_bytes()
idx = raw.index(b'\n', raw.index(b'public LocationContentSpawner prefab;')) + 1
raw = raw[:idx] + b'    public LocationContentSpawner bankAlternativePrefab; // Optional placement on the opposite bank.\r\n' + raw[idx:]
p.write_bytes(raw)
for rel in paths[2:4]:
    p = root / rel
    raw = p.read_bytes()
    assert raw.count(b'shorepoi6') == 1, rel
    p.write_bytes(raw.replace(b'shorepoi6', b'shorepoi7'))
print('Updated village pool revision, bank alternative and matching network protocol; scoped backups saved.')
