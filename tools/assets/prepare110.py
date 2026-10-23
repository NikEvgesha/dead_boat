from pathlib import Path
import json
prefix=Path('tools/assets/Boats103.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Boats103','class Texture110')
code=prefix+Path('tools/assets/Texture110.body.txt').read_text(encoding='utf-8-sig')
Path('tools/assets/Texture110.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-110/build.json').write_text(json.dumps({'csharpCode':code,'className':'Texture110','methodName':'Run'}),encoding='utf-8')
