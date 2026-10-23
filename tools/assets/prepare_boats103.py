from pathlib import Path
import json
prefix=Path('tools/assets/Market100.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Market100','class Boats103')
code=prefix+Path('tools/assets/Boats103.body.txt').read_text(encoding='utf-8')
Path('tools/assets/Boats103.cs.txt').write_text(code,encoding='utf-8');Path('Library/WorkshopBackups/DB-ON-103/boats.json').write_text(json.dumps({'csharpCode':code,'className':'Boats103','methodName':'Run'}),encoding='utf-8')
