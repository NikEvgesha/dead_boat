from pathlib import Path
import re,json
root=Path('.')
path=root/'Assets/Igrodelnya/Localization/LocalizationData.asset'
original=path.read_bytes(); raw=original.decode('utf-8-sig'); newline='\r\n' if '\r\n' in raw else '\n'
assert re.search(r'languages:\s*\n\s*- Ru\s*\n\s*- En',raw)
keys=[('UI/BoatShop/Title','Лодки','Boats'),('UI/BankShop/Title','Банк • Кристаллы','Bank • Crystals'),('UI/BoatShop/Catalogue','Каталог лодок','Boat catalogue')]
existing=re.findall(r'^  - Key: (.+)$',raw,re.M); existing=[s.strip() for s in existing]
for key,ru,en in keys:
 if key in existing: raise RuntimeError('Existing key needs inspection: '+key)
 assert len(re.findall(r'^    - (?:"Boats"|Boats)\s*$',raw,re.M))==0
append=''
for key,ru,en in keys: append+=f'  - Key: {key}{newline}    Translations:{newline}    - {json.dumps(ru,ensure_ascii=True)}{newline}    - {json.dumps(en)}{newline}'
new=original+(b'' if original.endswith(b'\n') else newline.encode())+append.encode()
assert new.startswith(original)
path.write_bytes(new)
scene=root/'Assets/Scenes/LobbyIslandWorkshop.unity'; text=scene.read_text(encoding='utf-8-sig')
blocks=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)); ids={int(b.group(2)) for b in blocks}
bindings=[]
for name,key in [('Boats_Localized_UI096',keys[0][0]),('Bank_Localized_UI096',keys[1][0]),('Catalogue_Localized_UI096',keys[2][0])]:
 found=[b for b in blocks if b.group(1)=='1' and re.search(r'^  m_Name: '+name+r'$',b.group(),re.M)]
 assert len(found)==1,name
 go=found[0]; goid=int(go.group(2)); new_id=max(i for i in ids if 0<i<9000000000000000000)+1; ids.add(new_id)
 old=go.group(); replacement=old.replace('  m_Layer:',f'  - component: {{fileID: {new_id}}}\n  m_Layer:',1); assert replacement!=old
 text=text.replace(old,replacement,1)
 text+=f'''--- !u!114 &{new_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {goid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: dd4151974eac4ae4ba8ed7bcb9cf28ae, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  localizationData: {{fileID: 11400000, guid: 247e385684679874b849f4c64cf8b6fc, type: 2}}
  selectedKey: {key}
  currentLanguage: Ru
'''
 bindings.append({'name':name,'key':key,'game_object_id':goid,'localized_component_id':new_id})
scene.write_text(text,encoding='utf-8',newline='\n')
assert path.read_bytes().startswith(original)
assert len(re.findall(r'^  - Key: ',new.decode('utf-8-sig'),re.M))==len(existing)+3
report={'previous_entries':len(existing),'added_entries':dict((k,{'Ru':ru,'En':en}) for k,ru,en in keys),'previous_asset_bytes_preserved':True,'bindings':bindings,'script_guid':'dd4151974eac4ae4ba8ed7bcb9cf28ae','localization_data_guid':'247e385684679874b849f4c64cf8b6fc','runtime_check':'Blocked: LocalizedText type unavailable while Assembly-CSharp cannot compile; existing GUID binding serialized for normal resolution after compilation.'}
(root/'Docs/assets/lobby-island/LOCALIZED_SIGNS096_VALIDATION.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=True,indent=2))
