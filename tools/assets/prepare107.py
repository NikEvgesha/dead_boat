from pathlib import Path
import json
prefix=Path('tools/assets/Reference106.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Reference106','class Detail107').replace('77-duck-helm106','83-duck-helm107')
code=prefix+Path('tools/assets/Detail107.body.txt').read_text(encoding='utf-8-sig')
Path('tools/assets/Detail107.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-107/build.json').write_text(json.dumps({'csharpCode':code,'className':'Detail107','methodName':'Run'}),encoding='utf-8')
