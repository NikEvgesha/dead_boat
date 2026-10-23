from pathlib import Path
import json
code=r'''using System;using System.Linq;using UnityEngine;using UnityEditor;public static class InspectBridge098{public static string Run(){var r=GameObject.Find("Lobby_Island_Visual_Prototype");return string.Join("\n",r.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("Bridge")).Select(t=>t.name+" parent="+t.parent.name))+"\nPATH="+(r.transform.Find("01_Central_Island/Park_Paths098")!=null);}}'''
Path('Library/WorkshopBackups/DB-ON-098/rescue.json').write_text(json.dumps({'csharpCode':code,'className':'InspectBridge098','methodName':'Run'}),encoding='utf-8')
