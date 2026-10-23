from pathlib import Path
import json,shutil
b=Path('Library/WorkshopBackups/DB-ON-105')
for name in ['Boat_DuckLuxury103','Boat_RoyalBlue103','Boat_SteamTug103']:
 p=Path('Assets/Prefabs/Workshop/Boats/Skins103')/(name+'.prefab')
 if not (b/p.name).exists():shutil.copy2(p,b/p.name)
code=Path('tools/assets/Boats104.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Boats104','class Design105').replace('66-duck-helm-forward104','72-sculpted-duck-helm105')+Path('tools/assets/Design105.body.txt').read_text(encoding='utf-8-sig')
Path('tools/assets/Design105.cs.txt').write_text(code,encoding='utf-8')
(b/'build.json').write_text(json.dumps({'csharpCode':code,'className':'Design105','methodName':'Run'}),encoding='utf-8')
