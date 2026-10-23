from pathlib import Path
import json
body=Path('tools/assets/Park098.body.txt').read_text(encoding='utf-8')
body=body.replace('t.name=="Main_Island_Bridge092"','(t.name=="Final_Connected_Pier094"||t.name=="Aligned_Plot01_Pier095")')
Path('tools/assets/Park098.body.txt').write_text(body,encoding='utf-8')
full=Path('tools/assets/Park098.cs.txt').read_text(encoding='utf-8')
prefix=full.split(' public static string Run()')[0].replace('class Park098','class Finish098')
routes=body.split(' Curve(2.8f,true,')[1].split(' var paths=')[0]
routes=' Curve(2.8f,true,'+routes
end=body.split(' Shot(main.TransformPoint')[1]
end=' Shot(main.TransformPoint'+end
code=prefix+''' public static string Run(){var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var root=GameObject.Find("Lobby_Island_Visual_Prototype").transform;var main=root.Find("01_Central_Island");var lab=main.Find("Animal_Laboratory097");var hospital=main.Find("02_Open_Market/04_Hospital_Source_Home_Hospital");UnityEngine.Object.DestroyImmediate(main.Find("Park_Paths098").gameObject);foreach(var p in new[]{"Park_Path_Edging098.asset","Park_Path_Surface098.asset"})AssetDatabase.DeleteAsset(D+p);
'''+routes+'''
 if(piers!=8)throw new Exception("Expected eight current piers, actual="+piers);var paths=G("Park_Paths098",main,Vector3.zero);Surface(paths,"Park_Path_Edging098",.18f,.035f,AssetDatabase.LoadAssetAtPath<Material>(D+"Park_Edge098.mat"));Surface(paths,"Park_Path_Surface098",0,.045f,AssetDatabase.LoadAssetAtPath<Material>(D+"Park_Limestone098.mat"));report.AppendLine("All8 actual current bridge arrivals connected. Bank=Банк. Hospital: original bandage+Heal,6counter2reserve copies. Lab: animals unchanged; floor/reactor/result hidden, tilted tablet on open metal legs. Physics="+root.GetComponentsInChildren<Collider>(true).Length);
'''+end
Path('tools/assets/Finish098.cs.txt').write_text(code,encoding='utf-8')
Path('Library/WorkshopBackups/DB-ON-098/finish.json').write_text(json.dumps({'csharpCode':code,'className':'Finish098','methodName':'Run'}),encoding='utf-8')
