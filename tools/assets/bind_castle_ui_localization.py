"""Bind new native UI Texts to the existing LocalizedText component schema.
The gameplay assembly is unavailable; keep script GUIDs rather than substituting scripts.
Run after Unity saves the conversion, then reload and validate in Unity.
"""
import json,re
from pathlib import Path
scene=Path('Assets/Scenes/CastleServicesWorkshop.unity')
text=scene.read_text(encoding='utf-8-sig')
docs={int(m[2]):(int(m[1]),m[0]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)}
translations={
'ПЯТЁРОЧКА':('Supplies','PYATEROCHKA'), 'ДОМ РЫБАКА':('Fisher','FISHERMAN'),
'ТОРГОВЫЙ ДОМ':('Trader','TRADING HOUSE'),'СДАЧА НАХОДОК':('SellLoot','SELL LOOT'),
'НАГРАДА':('Reward','REWARD'),'ОРУЖЕЙНЫЙ':('Armory','ARMORY'),
'ЯЙЦО → ВЫСИЖИВАНИЕ → ПИТОМЕЦ':('Hatching','EGG → HATCH → PET'),
'ШЕРИФ':('Sheriff','SHERIFF'),'ДОСТАВЬ ПИРАТА':('DeliverPirate','DELIVER A PIRATE'),
'РОЗЫСК':('Wanted','WANTED'),'СДАЧА':('Delivery','DELIVERY'),
'ДВА ОДИНАКОВЫХ → УЛУЧШЕНИЕ':('Merge','TWO MATCHING → UPGRADE'),'БОЛЬНИЦА':('Hospital','HOSPITAL')}
manifest=[]
for line in Path('Temp/castle-ui084.tsv').read_text(encoding='utf-8-sig').splitlines():
    path,ru=line.split('\t',1)
    match=re.search(r'/Localized_UI_(\d+)/Localized_Text$',path)
    if not match:continue # Existing shop Buy/NoMoney UI already has LocalizedText in BuyCanvas.prefab.
    canvas_name='Localized_UI_'+match[1]
    canvas_go=next(k for k,(kind,b) in docs.items() if kind==1 and f'  m_Name: {canvas_name}\n' in b)
    canvas_rt=next(k for k,(kind,b) in docs.items() if kind==224 and f'  m_GameObject: {{fileID: {canvas_go}}}\n' in b)
    child_rt=next(k for k,(kind,b) in docs.items() if kind==224 and f'  m_Father: {{fileID: {canvas_rt}}}\n' in b)
    child=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',docs[child_rt][1])[1])
    key,en=translations[ru]; key='Castle/'+key
    ident=9000000000000+int(match[1]);assert ident not in docs
    body=docs[child][1];assert 'm_Name: Localized_Text' in body
    new=body.replace('  m_Layer:',f'  - component: {{fileID: {ident}}}\n  m_Layer:',1)
    text=text.replace(body,new,1)
    text+=f'''--- !u!114 &{ident}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {child}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: dd4151974eac4ae4ba8ed7bcb9cf28ae, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  localizationData: {{fileID: 11400000, guid: 247e385684679874b849f4c64cf8b6fc, type: 2}}
  selectedKey: {key}
  currentLanguage: Ru
'''
    manifest.append({'path':path,'gameObject':child,'component':ident,'key':key,'Ru':ru,'En':en})
assert len(manifest)==17
loc=Path('Assets/Igrodelnya/Localization/LocalizationData.asset')
old=loc.read_text(encoding='utf-8-sig');new=old
assert re.search(r'  languages:\n  - Ru\n  - En\n  entries:',old)
for key in dict.fromkeys(m['key'] for m in manifest):
    m=next(m for m in manifest if m['key']==key)
    assert f'  - Key: {key}\n' not in old, 'Already bound; inspect instead of duplicate'
    new+=f"  - Key: {key}\n    Translations:\n    - {json.dumps(m['Ru'],ensure_ascii=True)}\n    - {json.dumps(m['En'],ensure_ascii=True)}\n"
backup=Path('Library/WorkshopBackups/DB-ON-084')
(backup/'BeforeLocaleBinding.unity').write_bytes(scene.read_bytes())
(backup/'LocalizationData.asset').write_bytes(loc.read_bytes())
scene.write_text(text,encoding='utf-8',newline='\n');loc.write_text(new,encoding='utf-8',newline='\n')
Path('Docs/assets/examples/Castle_UI_Localization.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('PASS: 17 native UI labels bound to existing LocalizedText, 13 Ru/En keys appended; existing localization entries preserved.')
