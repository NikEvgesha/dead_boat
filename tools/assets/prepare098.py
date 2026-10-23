from pathlib import Path
import json
prefix=Path('tools/assets/Laboratory097.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Laboratory097','class Park098')
code=prefix+Path('tools/assets/Park098.body.txt').read_text(encoding='utf-8')
Path('tools/assets/Park098.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-098/build.json').write_text(json.dumps({'csharpCode':code,'className':'Park098','methodName':'Run'}),encoding='utf-8')
