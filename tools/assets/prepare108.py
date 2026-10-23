from pathlib import Path
import json
prefix=Path('tools/assets/Detail107.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Detail107','class Detail108').replace('83-duck-helm107','90-duck-helm108')
code=prefix+Path('tools/assets/Detail108.body.txt').read_text(encoding='utf-8-sig')
Path('tools/assets/Detail108.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-108/build.json').write_text(json.dumps({'csharpCode':code,'className':'Detail108','methodName':'Run'}),encoding='utf-8')
