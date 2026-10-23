from pathlib import Path
import re,json
p=Path('Assets/Igrodelnya/Localization/LocalizationData.asset')
before=p.read_bytes()
checkpoint=Path('Library/WorkshopBackups/DB-ON-098/LocalizationData.before.asset').read_bytes()
assert before==checkpoint
pattern=rb'(  - Key: UI/BankShop/Title\r?\n    Translations:\r?\n)(    - .*?\r?\n    - .*?\r?\n)'
m=re.search(pattern,before); assert m
nl=b'\r\n' if b'\r\n' in m[0] else b'\n'
lines=('    - '+json.dumps('Банк',ensure_ascii=True)).encode()+nl+b'    - "Bank"'+nl
after=before[:m.start(2)]+lines+before[m.end(2):]
p.write_bytes(after)
assert re.sub(pattern,rb'\1<bank>',before)==re.sub(pattern,rb'\1<bank>',after)
scene=Path('Assets/Scenes/LobbyIslandWorkshop.unity').read_text(encoding='utf-8-sig')
assert 'selectedKey: UI/BankShop/Title' in scene
blocks=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)\n.*?(?=^--- !u!|\Z)',scene,re.M|re.S))
go=next(b for b in blocks if b[1]=='1' and '  m_Name: Bank_Localized_UI096\n' in b[0])
txt=next(b for b in blocks if b[1]=='114' and f'  m_GameObject: {{fileID: {go[2]}}}' in b[0] and 'm_Text:' in b[0])
value=re.search(r'^  m_Text: (.+)$',txt[0],re.M)[1]
assert json.loads(value)=='Банк'
report={'key':'UI/BankShop/Title','Ru':'Банк','En':'Bank','other_localization_bytes_preserved':True,'bank_scene_ui_verified':True}
Path('Docs/assets/lobby-island/BANK_NAME098_VALIDATION.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
code='''using System;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;public static class Refresh098{public static string Run(){var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty)throw new Exception("Unsaved changes");AssetDatabase.ImportAsset("Assets/Igrodelnya/Localization/LocalizationData.asset",ImportAssetOptions.ForceUpdate);return "Localization refreshed; scene="+s.path+" clean="+!s.isDirty;}}'''
Path('Library/WorkshopBackups/DB-ON-098/refresh.json').write_text(json.dumps({'csharpCode':code,'className':'Refresh098','methodName':'Run'}),encoding='utf-8')
print(json.dumps(report,ensure_ascii=True))
