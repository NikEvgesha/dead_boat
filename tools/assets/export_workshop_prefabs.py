"""Export saved Unity scene subgraphs without dropping temporarily unresolved scripts.

Unity's SaveAsPrefabAsset refuses Missing Script components. This exporter retains
their serialized GUIDs and fields; it never guesses or creates replacement scripts.
Run only after saving the workshop scene, then import/validate through Unity.
"""
import re
from pathlib import Path

SCENE = Path('Assets/Scenes/CastleServicesWorkshop.unity')
DEST = Path('Assets/Prefabs/Workshop/GameAligned')
text = SCENE.read_text(encoding='utf-8-sig')
header = text[:text.index('--- !u!')]
docs = {}
for match in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)', text, re.M | re.S):
    docs[int(match[2])] = (int(match[1]), match[0])

def export(name, path):
    matches = [(key, body) for key, (kind, body) in docs.items()
               if kind == 1 and re.search(r'^  m_Name: '+re.escape(name)+r'$', body, re.M)]
    assert len(matches) == 1, (name, len(matches))
    root, body = matches[0]
    transform = next(int(ref) for ref in re.findall(r'component: \{fileID: (-?\d+)\}', body)
                     if docs[int(ref)][0] in (4, 224))
    overrides = {transform: re.sub(r'^  m_Father: .*$', '  m_Father: {fileID: 0}', docs[transform][1], flags=re.M)}
    overrides[transform] = re.sub(r'^  m_LocalPosition: .*$', '  m_LocalPosition: {x: 0, y: 0, z: 0}', overrides[transform], flags=re.M)
    chosen, pending = set(), [root]
    while pending:
        key = pending.pop()
        if key == 0 or key in chosen:
            continue
        assert key in docs, (name, 'unresolved local reference', key)
        chosen.add(key)
        body = overrides.get(key, docs[key][1])
        pending += [int(ref) for ref in re.findall(r'\{fileID: (-?\d+)\}', body)]
    # No parent or other scene roots may have leaked into the graph.
    assert not any(kind == 1660057539 for key, (kind, _) in docs.items() if key in chosen)
    result = header + ''.join(overrides.get(key, body) for key, (_, body) in docs.items() if key in chosen)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(result, encoding='utf-8', newline='\n')
    print(f'{path}: {len(chosen)} serialized objects; all local references resolved')

if __name__ == '__main__':
    # Final boat instances are linked: do not overwrite their restored prefabs.
    export('Castle_Services_Prototype', Path('Assets/Prefabs/Workshop/CastleServices/CastleServices_01.prefab'))
