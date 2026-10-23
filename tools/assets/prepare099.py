from pathlib import Path
import json
prefix=Path('tools/assets/Laboratory097.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Laboratory097','class Banks099')
code=prefix+Path('tools/assets/Banks099.body.txt').read_text(encoding='utf-8')
Path('tools/assets/Banks099.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-099/build.json').write_text(json.dumps({'csharpCode':code,'className':'Banks099','methodName':'Run'}),encoding='utf-8')
