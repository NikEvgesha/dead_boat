from pathlib import Path
import re,json
asset=Path('Assets/Igrodelnya/Localization/LocalizationData.asset'); before=asset.read_bytes(); raw=before.decode('utf-8-sig'); key='UI/AnimalLaboratory/Title'; assert key not in raw
newline='\r\n' if '\r\n' in raw else '\n'
addition=f'  - Key: {key}{newline}    Translations:{newline}    - '+json.dumps('Лаборатория',ensure_ascii=True)+newline+'    - "Laboratory"'+newline
asset.write_bytes(before+addition.encode()); assert asset.read_bytes().startswith(before)
scene=Path('Assets/Scenes/LobbyIslandWorkshop.unity'); text=scene.read_text(encoding='utf-8-sig'); bs=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)\n.*?(?=^--- !u!|\Z)',text,re.M|re.S)); go=[b for b in bs if b.group(1)=='1' and re.search(r'^  m_Name: Laboratory_Localized_UI097$',b.group(),re.M)]; assert len(go)==1
old=go[0].group(); gid=int(go[0].group(2)); cid=max(int(b.group(2)) for b in bs if 0<int(b.group(2))<9000000000000000000)+1
text=text.replace(old,old.replace('  m_Layer:',f'  - component: {{fileID: {cid}}}\n  m_Layer:',1),1)
text+=f'''--- !u!114 &{cid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: dd4151974eac4ae4ba8ed7bcb9cf28ae, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  localizationData: {{fileID: 11400000, guid: 247e385684679874b849f4c64cf8b6fc, type: 2}}
  selectedKey: {key}
  currentLanguage: Ru
'''
scene.write_text(text,encoding='utf-8',newline='\n')
report={'key':key,'Ru':'Лаборатория','En':'Laboratory','previous_entries':len(re.findall(r'^  - Key:',raw,re.M)),'new_entries':1,'previous_asset_bytes_preserved':True,'game_object_id':gid,'localized_component_id':cid,'runtime_localization':'Existing MonoScript GUID configured; blocked by unavailable game assembly until compilation restored, no PlayMode claimed.'}
Path('Docs/assets/lobby-island/LAB_LOCALIZATION097_VALIDATION.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(report,ensure_ascii=True))
