from pathlib import Path
import json
prefix=Path('tools/assets/Detail108.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Detail108','class Detail111').replace('90-duck-helm108','106-duck-helm111')
code=prefix+Path('tools/assets/Detail111.body.txt').read_text(encoding='utf-8-sig')
Path('tools/assets/Detail111.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-111/build.json').write_text(json.dumps({'csharpCode':code,'className':'Detail111','methodName':'Run'}),encoding='utf-8')
