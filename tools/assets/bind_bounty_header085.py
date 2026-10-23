from pathlib import Path
import re
p=Path('Assets/Scenes/CastleServicesWorkshop.unity');s=p.read_text(encoding='utf-8-sig')
m=next(m for m in re.finditer(r'^--- !u!1 &(\d+)\n.*?(?=^--- !u!|\Z)',s,re.M|re.S) if '  m_Name: Bounty_Header_LocalizedUI\n' in m[0])
id=9000000000101;assert f'&{id}\n' not in s
b=m[0].replace('  m_Layer:',f'  - component: {{fileID: {id}}}\n  m_Layer:',1)
s=s.replace(m[0],b,1)+f'''--- !u!114 &{id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {m[1]}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: dd4151974eac4ae4ba8ed7bcb9cf28ae, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  localizationData: {{fileID: 11400000, guid: 247e385684679874b849f4c64cf8b6fc, type: 2}}
  selectedKey: Castle/Wanted
  currentLanguage: Ru
'''
p.write_text(s,encoding='utf-8',newline='\n');print('Board header bound to existing Castle/Wanted Ru/En key.')
