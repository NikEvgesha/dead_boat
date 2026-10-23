from pathlib import Path
import json, shutil
b=Path('Library/WorkshopBackups/DB-ON-104'); b.mkdir(parents=True,exist_ok=True)
for src,dst in [('Assets/Scenes/LobbyIslandWorkshop.unity','LobbyIslandWorkshop.before.unity'),('Assets/Igrodelnya/Localization/LocalizationData.asset','LocalizationData.before.asset')]:
    if not (b/dst).exists(): shutil.copy2(src,b/dst)
for name in ['Boat_DuckLuxury103','Boat_RoyalBlue103','Boat_SteamTug103']:
    p=Path('Assets/Prefabs/Workshop/Boats/Skins103')/(name+'.prefab')
    if not (b/p.name).exists():shutil.copy2(p,b/p.name)
prefix=Path('tools/assets/Boats103.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Boats103','class Boats104')
code=prefix+Path('tools/assets/Boats104.body.txt').read_text(encoding='utf-8-sig')
Path('tools/assets/Boats104.cs.txt').write_text(code,encoding='utf-8')
(b/'build.json').write_text(json.dumps({'csharpCode':code,'className':'Boats104','methodName':'Run'}),encoding='utf-8')
