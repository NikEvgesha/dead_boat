from pathlib import Path
import json
prefix=Path('tools/assets/Detail108.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Detail108','class Detail109').replace('90-duck-helm108','96-duck-helm109')
code=prefix+Path('tools/assets/Detail109.body.txt').read_text(encoding='utf-8-sig')
Path('tools/assets/Detail109.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-109/build.json').write_text(json.dumps({'csharpCode':code,'className':'Detail109','methodName':'Run'}),encoding='utf-8')
