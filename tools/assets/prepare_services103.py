from pathlib import Path
import json
prefix=Path('tools/assets/Market100.cs.txt').read_text(encoding='utf-8-sig').split(' public static string Run()')[0].replace('class Market100','class Services103')
code=prefix+Path('tools/assets/Services103.body.txt').read_text(encoding='utf-8')
Path('tools/assets/Services103.cs.txt').write_text(code,encoding='utf-8');Path('Library/WorkshopBackups/DB-ON-103/services.json').write_text(json.dumps({'csharpCode':code,'className':'Services103','methodName':'Run'}),encoding='utf-8')
