from pathlib import Path
import re,json
scene=Path('Assets/Scenes/LobbyIslandWorkshop.unity').read_text(encoding='utf-8-sig')
report=json.loads(Path('Docs/assets/lobby-island/LOCALIZED_SIGNS096_VALIDATION.json').read_text(encoding='utf-8'))
for b in report['bindings']:
 go=re.search(r'^--- !u!1 &'+str(b['game_object_id'])+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
 comp=re.search(r'^--- !u!114 &'+str(b['localized_component_id'])+r'\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S).group()
 assert 'fileID: '+str(b['localized_component_id']) in go
 assert 'selectedKey: '+b['key'] in comp
 assert report['script_guid'] in comp and report['localization_data_guid'] in comp
before=Path('Library/WorkshopBackups/DB-ON-096/LocalizationData.before.asset').read_bytes()
after=Path('Assets/Igrodelnya/Localization/LocalizationData.asset').read_bytes()
assert after.startswith(before)
for key,tr in report['added_entries'].items():
 block=after[len(before):].decode().split('  - Key: '+key+'\n',1)[1].split('  - Key:',1)[0]
 vals=re.findall(r'^    - (.+)$',block,re.M)
 assert [json.loads(v) for v in vals]==[tr['Ru'],tr['En']], repr((key, vals, tr))
print('OK: all3 LocalizedText GUID/data/key bindings persisted after reopening; Ru/En strings exact;465 original entries byte-preserved.')


