from pathlib import Path
import json
prefix=Path('tools/assets/Boats104.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Boats104','class Reference106').replace('66-duck-helm-forward104','77-duck-helm106')
code=prefix+Path('tools/assets/Reference106.body.txt').read_text(encoding='utf-8-sig')
Path('tools/assets/Reference106.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-106/build.json').write_text(json.dumps({'csharpCode':code,'className':'Reference106','methodName':'Run'}),encoding='utf-8')
