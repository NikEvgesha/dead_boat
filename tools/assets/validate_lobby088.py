"""Validate checkpoint and preserve all source scene YAML, including unavailable scripts."""
from pathlib import Path
import re, hashlib, json

ROOT = Path(__file__).resolve().parents[2]
D = ROOT / 'Docs/assets/lobby-island'
original = ROOT / 'Assets/Scenes/Lobby.unity'
copy = ROOT / 'Assets/Scenes/LobbyIslandWorkshop.unity'

def records(text):
    return {m.group(2): m.group(0) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(?:(?!^--- !u!).)*', text, re.M | re.S)}

def main():
    src = (D / 'checkpoint/Lobby.saved.unity').read_text(encoding='utf-8')
    dst = copy.read_text(encoding='utf-8')
    a, b = records(src), records(dst)
    missing = [ident for ident in a if ident not in b]
    changed = []
    allowed = []
    for ident in a.keys() & b.keys():
        if a[ident] == b[ident]:
            continue
        aa, bb = a[ident], b[ident]
        if re.sub(r'  m_IsActive: [01]', '  m_IsActive: X', aa) == re.sub(r'  m_IsActive: [01]', '  m_IsActive: X', bb):
            allowed.append(ident)
        elif aa.startswith('--- !u!104 ') or aa.startswith('--- !u!166 ') or aa.startswith('--- !u!29 ') or aa.startswith('--- !u!196 '):
            allowed.append(ident)
        elif aa.startswith('--- !u!1001 ') and re.sub(r'    - target: [^\n]*\n      propertyPath: m_IsActive\n      value: [01]\n      objectReference: \{fileID: 0\}\n', '', aa) == re.sub(r'    - target: [^\n]*\n      propertyPath: m_IsActive\n      value: [01]\n      objectReference: \{fileID: 0\}\n', '', bb):
            allowed.append(ident)
        elif aa.startswith('--- !u!1660057539 ') and bb.startswith(aa):
            allowed.append(ident)
        else:
            changed.append(ident)
    report = {'original_matches_checkpoint': original.read_bytes() == (D/'checkpoint/Lobby.saved.unity').read_bytes(),
              'build_settings_match_checkpoint': (ROOT/'ProjectSettings/EditorBuildSettings.asset').read_bytes() == (D/'checkpoint/EditorBuildSettings.asset').read_bytes(),
              'source_records':len(a),'new_records':len(b),'missing_source_records':missing,
              'changed_source_records':changed,'allowed_inactive_roots_and_render_settings':allowed}
    (D/'source-preservation-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False,indent=2))
    if changed:
        (D/'source-record-differences.txt').write_text('\n'.join('SOURCE\n'+a[i]+'\nCOPY\n'+b[i] for i in changed),encoding='utf-8')
    if missing:
        (D/'missing-source-records.txt').write_text('\n'.join(a[i] for i in missing),encoding='utf-8')
    assert not missing and not changed, 'Original source scene fields changed'
    assert report['original_matches_checkpoint'] and report['build_settings_match_checkpoint'], 'Release file changed'

if __name__ == '__main__':
    main()
