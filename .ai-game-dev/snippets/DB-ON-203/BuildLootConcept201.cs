using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BuildLootConcept201
{
    public const string Root = "Assets/Prefabs/Workshop/LootConcept201";
    const string Output = "Docs/locations/previews/loot201";
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Material[] palette;
    const int Brass=0,Red=1,Green=2,Wood=3,Oak=4,Silver=5,Dark=6,Blue=7,Ivory=8,Cyan=9,Black=10,Paper=11,Olive=12,Glass=13,Teal=14,Straw=15;
    static readonly string[] IDs={"T01_Star","T02_MusicBox","T03_Bell","T04_AuroraCrystal","T05_PorcelainFox","T06_Compass","T07_ShipInBottle","T08_SignetRing","T09_LighthouseGlobe","T10_Coffer","F01_BrokenSled","F02_CrateScraps","F03_PineCones","F04_DriedWreath","F05_CandleStubs","F06_WrappingPaper","F07_CharcoalSack","F08_StrawBundle","F09_BrokenNutcracker","F10_HempRope"};
    static readonly string[] ColorNames={"Brass","Cranberry","Pine","Wood","Oak","Silver","Navy","Blue","Ivory","Ice","Charcoal","Paper","DriedEvergreen","TintedGlass","Teal","Straw"};
    static readonly string[] Hex={"E3AC35","A93A34","3E723F","77492D","AF7546","A3A9B1","263851","426788","F2E4CB","55BED6","353239","C39359","666941","77BFE7","3B7571","D4A654"};
    static bool MainMap(int i)=>i==5||i==6||i==9;
    static string Folder(int i)=>Root+(MainMap(i)?"/MainMaps":"/NewYear");
    static string Prefab(int i)=>Folder(i)+"/"+IDs[i]+".prefab";
    static void Folders(string path){var parts=path.Split('/');string at=parts[0];foreach(var part in parts.Skip(1)){if(!AssetDatabase.IsValidFolder(at+"/"+part))AssetDatabase.CreateFolder(at,part);at+="/"+part;}}
    static void Materials(){palette=new Material[Hex.Length];for(int i=0;i<Hex.Length;i++){
        var shader=Shader.Find("Standard");if(!shader)throw new Exception("Standard shader unavailable");
        ColorUtility.TryParseHtmlString("#"+Hex[i],out var color);
        var m=new Material(shader){name="Loot201_"+ColorNames[i],color=color};m.SetFloat("_Glossiness",.04f);m.SetFloat("_Metallic",0);
        if(i==Glass){color.a=.26f;m.color=color;m.SetFloat("_Mode",2);m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.EnableKeyword("_ALPHABLEND_ON");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;}
        AssetDatabase.CreateAsset(m,Root+"/Materials/"+m.name+".mat");palette[i]=m;
    }}
    sealed class Shape
    {
        public readonly List<Vector3> verts=new List<Vector3>();
        public readonly SortedDictionary<int,List<int>> faces=new SortedDictionary<int,List<int>>();
        void Face(int color,Vector3 inner,params Vector3[] pts){
            var c=pts.Aggregate(Vector3.zero,(a,b)=>a+b)/pts.Length;
            if(Vector3.Dot(Vector3.Cross(pts[1]-pts[0],pts[2]-pts[0]),c-inner)<0)Array.Reverse(pts);
            if(!faces.TryGetValue(color,out var ids))faces[color]=ids=new List<int>();int o=verts.Count;verts.AddRange(pts);
            for(int i=1;i<pts.Length-1;i++){ids.Add(o);ids.Add(o+i);ids.Add(o+i+1);}
        }
        public void Box(Vector3 p,Vector3 s,int c,Quaternion q=default(Quaternion)){
            if(q==default(Quaternion))q=Quaternion.identity;var h=s*.5f;
            Vector3 A(float x,float y,float z)=>p+q*Vector3.Scale(h,V(x,y,z));
            Face(c,p,A(-1,-1,-1),A(-1,1,-1),A(1,1,-1),A(1,-1,-1));Face(c,p,A(1,-1,1),A(1,1,1),A(-1,1,1),A(-1,-1,1));
            Face(c,p,A(-1,-1,1),A(-1,1,1),A(-1,1,-1),A(-1,-1,-1));Face(c,p,A(1,-1,-1),A(1,1,-1),A(1,1,1),A(1,-1,1));
            Face(c,p,A(-1,1,-1),A(-1,1,1),A(1,1,1),A(1,1,-1));Face(c,p,A(-1,-1,1),A(-1,-1,-1),A(1,-1,-1),A(1,-1,1));
        }
        public void Loft(Vector3 p,int n,int c,Vector3[] levels,Quaternion q=default(Quaternion),bool caps=true){
            if(q==default(Quaternion))q=Quaternion.identity;
            Vector3 R(int l,int i){var v=levels[l];float a=(i+.5f)*Mathf.PI*2/n;return p+q*V(Mathf.Cos(a)*v.x,v.y,Mathf.Sin(a)*v.z);}
            for(int l=0;l<levels.Length-1;l++)for(int i=0;i<n;i++)Face(c,p+q*V(0,(levels[l].y+levels[l+1].y)/2,0),R(l,i),R(l,i+1),R(l+1,i+1),R(l+1,i));
            if(caps){Face(c,p+q*V(0,levels[0].y+.01f,0),Enumerable.Range(0,n).Select(i=>R(0,i)).ToArray());int top=levels.Length-1;Face(c,p+q*V(0,levels[top].y-.01f,0),Enumerable.Range(0,n).Select(i=>R(top,i)).ToArray());}
        }
        public void Disc(Vector3 p,float r,float h,int n,int c,Quaternion q=default(Quaternion))=>Loft(p,n,c,new[]{V(r,-h/2,r),V(r,h/2,r)},q);
        public void Ring(Vector3 p,float outside,float inside,float h,int n,int c,Quaternion q=default(Quaternion),int skip=-1){
            if(q==default(Quaternion))q=Quaternion.identity;
            Vector3 R(float r,float y,int i){float a=(i+.5f)*Mathf.PI*2/n;return p+q*V(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);}
            for(int i=0;i<n;i++){if(i==skip)continue;int j=i+1;float a=(i+1f)*Mathf.PI*2/n;var inner=p+q*V(Mathf.Cos(a)*(outside+inside)/2,0,Mathf.Sin(a)*(outside+inside)/2);
            var ot=R(outside,h/2,i);var ou=R(outside,h/2,j);var ob=R(outside,-h/2,i);var oc=R(outside,-h/2,j);var it=R(inside,h/2,i);var iu=R(inside,h/2,j);var ib=R(inside,-h/2,i);var ic=R(inside,-h/2,j);
            Face(c,inner,ot,ou,oc,ob);Face(c,inner,it,ib,ic,iu);Face(c,inner,ot,it,iu,ou);Face(c,inner,ob,oc,ic,ib);
            if(skip>=0){if(j%n==skip)Face(c,inner,ou,iu,ic,oc);if((i-1+n)%n==skip)Face(c,inner,ot,ob,ib,it);}}
        }
        public void Beam(Vector3 a,Vector3 b,float width,float depth,int c){var d=b-a;Box((a+b)*.5f,V(width,d.magnitude,depth),c,Quaternion.FromToRotation(Vector3.up,d));}
        public void Prism(Vector3 p,Vector2[] points,float thickness,int c,Quaternion q=default(Quaternion)){
            if(q==default(Quaternion))q=Quaternion.identity;
            var a=points.Select(v=>p+q*V(v.x,v.y,-thickness/2)).ToArray();var b=points.Select(v=>p+q*V(v.x,v.y,thickness/2)).ToArray();
            for(int i=0;i<a.Length;i++){int j=(i+1)%a.Length;Face(c,p,p+q*V(0,0,-thickness/2),a[i],a[j]);Face(c,p,p+q*V(0,0,thickness/2),b[i],b[j]);Face(c,p,a[i],b[i],b[j],a[j]);}
        }
        public void Taper(Vector3 a,Vector3 b,float start,float end,int color){var d=b-a;Loft(a,6,color,new[]{V(start,0,start),V(end,d.magnitude,end)},Quaternion.FromToRotation(Vector3.up,d));}
        public void Rotate(Quaternion q){for(int i=0;i<verts.Count;i++)verts[i]=q*verts[i];}
        public Mesh Mesh(string name){
            var min=verts.Min(v=>v.y);for(int i=0;i<verts.Count;i++)verts[i]-=Vector3.up*min;
            var m=new Mesh{name=name};m.SetVertices(verts);m.subMeshCount=faces.Count;int k=0;foreach(var t in faces.Values)m.SetTriangles(t,k++);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
    static void Fir(Shape s,Vector3 p,float r,float h){s.Disc(p+V(0,h*.10f,0),r*.23f,h*.2f,6,Wood);for(int i=0;i<2;i++)s.Loft(p+V(0,h*(.14f+.34f*i),0),6,Green,new[]{V(r*(1-i*.24f),0,r*(1-i*.24f)),V(.007f,h*.52f,.007f)});}
    static Shape Model(int i){
        var s=new Shape();var upright=Quaternion.Euler(90,0,0);var sideways=Quaternion.Euler(0,0,-90);
        switch(i){
        case 0:
            s.Disc(V(0,.085f,0),.108f,.17f,6,Red);
            s.Prism(V(0,.32f,0),Enumerable.Range(0,10).Select(j=>{float a=(90+j*36)*Mathf.Deg2Rad;float r=j%2==0?.255f:.109f;return new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r);}).ToArray(),.055f,Brass);break;
        case 1:
            s.Disc(V(0,.105f,0),.244f,.17f,10,Red);s.Ring(V(0,.199f,0),.246f,.211f,.041f,10,Red);s.Disc(V(0,.194f,0),.209f,.018f,10,Ivory);
            s.Disc(V(0,.397f,.168f),.238f,.049f,10,Red,Quaternion.Euler(106,0,0));s.Ring(V(0,.390f,.139f),.233f,.207f,.018f,10,Red,Quaternion.Euler(106,0,0));Fir(s,V(0,.203f,-.045f),.111f,.246f);
            foreach(float x in new[]{-.135f,.135f})foreach(float z in new[]{-.114f,.114f})s.Box(V(x,.035f,z),V(.061f,.070f,.061f),Brass);
            s.Box(V(.262f,.113f,0),V(.105f,.035f,.038f),Brass);s.Box(V(.319f,.113f,0),V(.038f,.100f,.062f),Brass);break;
        case 2:
            s.Loft(V(0,.07f,0),8,Silver,new[]{V(.182f,0,.182f),V(.131f,.079f,.131f),V(.085f,.219f,.085f)});s.Disc(V(0,.04f,0),.048f,.08f,6,Brass);s.Disc(V(0,.345f,0),.042f,.13f,6,Ivory);s.Disc(V(0,.443f,0),.071f,.087f,6,Red);break;
        case 3:
            s.Disc(V(0,.060f,0),.141f,.12f,6,Brass);s.Loft(V(0,.10f,0),6,Cyan,new[]{V(.086f,0,.086f),V(.125f,.37f,.125f),V(.006f,.535f,.006f)});break;
        case 4:
            s.Loft(V(0,0,0),7,Ivory,new[]{V(.165f,0,.150f),V(.165f,.095f,.155f),V(.099f,.317f,.092f),V(.074f,.395f,.069f)});
            s.Loft(V(0,.32f,-.027f),6,Ivory,new[]{V(.069f,0,.082f),V(.140f,.079f,.103f),V(.118f,.17f,.089f),V(.073f,.19f,.068f)});
            s.Loft(V(0,.412f,-.075f),6,Ivory,new[]{V(.073f,-.14f,.056f),V(.098f,.016f,.070f)},upright);s.Box(V(0,.412f,-.217f),V(.057f,.04f,.027f),Black);
            foreach(float x in new[]{-.074f,.074f}){s.Loft(V(x,.481f,.014f),3,Ivory,new[]{V(.061f,0,.059f),V(.004f,.155f,.004f)},Quaternion.Euler(0,0,x>0?-16:16));s.Box(V(x*1.34f,.444f,-.096f),V(.035f,.017f,.028f),Black,Quaternion.Euler(0,x>0?-37:37,0));}
            s.Loft(V(0,.29f,-.006f),8,Red,new[]{V(.117f,0,.108f),V(.113f,.058f,.109f)});s.Box(V(.108f,.235f,-.069f),V(.066f,.158f,.025f),Red,Quaternion.Euler(6,-9,-13));
            var tail0=V(0,.118f,.10f);var tail1=V(.07f,.088f,.252f);var tail2=V(.205f,.092f,.247f);var tail3=V(.252f,.156f,.158f);
            s.Taper(tail0,tail1,.061f,.086f,Ivory);s.Taper(tail1,tail2,.086f,.075f,Ivory);s.Taper(tail2,tail3,.075f,.016f,Ivory);break;
        case 5:
            s.Disc(V(0,.042f,0),.215f,.084f,8,Brass);s.Disc(V(0,.088f,0),.172f,.015f,8,Dark);s.Ring(V(0,.089f,0),.215f,.172f,.02f,8,Brass);
            s.Disc(V(0,.243f,.164f),.203f,.044f,8,Brass,Quaternion.Euler(111,0,0));s.Box(V(0,.09f,.184f),V(.105f,.046f,.069f),Oak);
            var bearing=Quaternion.Euler(90,0,-43);
            var diagonal=Quaternion.Euler(0,43,0)*Vector3.forward;
            foreach(int k in new[]{-1,1})s.Prism(V(0,.113f,0)+diagonal*(k*.043f),new[]{new Vector2(-.039f,-.043f),new Vector2(.039f,-.043f),new Vector2(0,.087f)},.016f,k>0?Ivory:Red,k>0?bearing:Quaternion.Euler(0,180,0)*bearing);
            for(int j=0;j<8;j++){var angle=Quaternion.Euler(0,j*45,0);s.Box(V(0,.101f,0)+angle*V(0,0,.146f),V(.009f,.006f,.025f),Brass,angle);}
            s.Disc(V(0,.129f,0),.025f,.019f,6,Brass);break;
        case 6:
            foreach(float x in new[]{-.22f,.20f})s.Box(V(x,.046f,0),V(.105f,.092f,.288f),Wood);s.Box(V(0,.035f,0),V(.56f,.07f,.165f),Oak);
            s.Loft(V(0,.244f,0),8,Glass,new[]{V(.078f,-.366f,.078f),V(.081f,-.278f,.081f),V(.168f,-.204f,.168f),V(.168f,.263f,.168f),V(.129f,.325f,.129f)},sideways);
            s.Disc(V(-.364f,.244f,0),.088f,.094f,8,Oak,sideways);s.Ring(V(-.266f,.244f,0),.095f,.080f,.029f,8,Cyan,sideways);
            s.Box(V(.045f,.135f,0),V(.35f,.027f,.138f),Blue);s.Loft(V(.049f,.157f,-.012f),6,Wood,new[]{V(.126f,0,.045f),V(.165f,.059f,.065f)});s.Beam(V(.043f,.21f,-.01f),V(.043f,.378f,-.01f),.015f,.014f,Oak);
            s.Prism(V(.090f,.272f,-.022f),new[]{new Vector2(-.046f,-.060f),new Vector2(.085f,-.060f),new Vector2(-.046f,.104f)},.013f,Ivory);break;
        case 7:
            s.Ring(V(0,.168f,0),.167f,.106f,.084f,8,Brass,Quaternion.Euler(78,0,0));s.Box(V(0,.324f,-.023f),V(.187f,.078f,.151f),Brass,Quaternion.Euler(-12,0,0));s.Box(V(0,.376f,-.034f),V(.139f,.052f,.109f),Red,Quaternion.Euler(-12,0,0));break;
        case 8:
            s.Loft(V(0,0,0),8,Dark,new[]{V(.185f,0,.185f),V(.153f,.103f,.153f)});s.Disc(V(0,.101f,0),.158f,.025f,8,Brass);
            s.Loft(V(0,.11f,0),10,Glass,new[]{V(.113f,0,.113f),V(.181f,.085f,.181f),V(.179f,.229f,.179f),V(.117f,.328f,.117f),V(.023f,.354f,.023f)});
            s.Disc(V(0,.126f,0),.128f,.022f,7,Ivory);s.Loft(V(0,.14f,0),6,Red,new[]{V(.05f,0,.05f),V(.034f,.155f,.034f)});s.Disc(V(0,.226f,0),.045f,.056f,6,Ivory);s.Disc(V(0,.302f,0),.042f,.044f,6,Brass);s.Loft(V(0,.325f,0),6,Red,new[]{V(.063f,0,.063f),V(.007f,.055f,.007f)});break;
        case 9:
            s.Box(V(0,.110f,0),V(.493f,.220f,.327f),Dark);s.Box(V(0,.211f,0),V(.505f,.032f,.339f),Black);
            var arch=new[]{new Vector2(-.17f,-.026f),new Vector2(.17f,-.026f),new Vector2(.169f,.028f),new Vector2(.124f,.086f),new Vector2(.057f,.111f),new Vector2(-.057f,.111f),new Vector2(-.124f,.086f),new Vector2(-.169f,.028f)};
            var across=Quaternion.Euler(0,90,0);s.Prism(V(0,.251f,0),arch,.508f,Blue,across);
            foreach(float x in new[]{-.208f,.208f}){s.Box(V(x,.111f,-.168f),V(.047f,.221f,.022f),Silver);s.Box(V(x,.111f,.168f),V(.047f,.221f,.022f),Silver);s.Prism(V(x,.252f,0),arch.Select(p=>p*1.061f).ToArray(),.050f,Silver,across);}
            s.Box(V(0,.206f,-.179f),V(.099f,.104f,.029f),Silver);s.Disc(V(0,.208f,-.198f),.032f,.024f,6,Green,upright);break;
        case 10:
            foreach(float x in new[]{-.165f,.160f}){var points=new[]{V(x,.038f,-.365f),V(x,.027f,-.274f),V(x,.031f,.217f)};for(int j=0;j<points.Length-1;j++)s.Beam(points[j],points[j+1],.061f,.062f,Wood);
            if(x<0)s.Beam(V(x,.038f,-.365f),V(x,.159f,-.425f),.060f,.060f,Wood);s.Beam(V(x,.04f,-.207f),V(x,.200f,-.207f),.050f,.055f,Oak);s.Beam(V(x,.04f,.14f),V(x,.20f,.14f),.051f,.055f,Oak);}
            for(int j=0;j<3;j++){float x=-.147f+j*.147f;s.Box(V(x,.213f,.003f),V(.132f,.049f,j==2?.44f:.617f),j==1?Red:Oak,Quaternion.Euler(0,j==2?-6:0,0));}break;
        case 11:
            for(int j=0;j<4;j++)s.Box(V((j-1.5f)*.094f,.065f+j*.012f,0),V(.101f,.057f,.35f+j*.048f),j==0?Wood:Oak,Quaternion.Euler(2,j*7-9,0));
            s.Box(V(0,.126f,-.02f),V(.397f,.043f,.061f),Wood,Quaternion.Euler(0,13,-4));break;
        case 12:
            s.Loft(V(0,0,0),8,Oak,new[]{V(.129f,0,.180f),V(.173f,.074f,.205f)},default(Quaternion),false);s.Ring(V(0,.077f,0),.173f,.14f,.036f,8,Paper);s.Disc(V(0,.027f,0),.137f,.053f,8,Oak);
            foreach(var p in new[]{V(-.077f,.052f,-.057f),V(.073f,.051f,-.066f),V(.009f,.053f,.073f)})for(int j=0;j<3;j++)s.Loft(p+V(0,j*.045f,0),6,j%2==0?Wood:Oak,new[]{V(.074f-j*.015f,0,.074f-j*.015f),V(.045f-j*.012f,.064f,.045f-j*.012f)});break;
        case 13:
            s.Ring(V(0,.072f,0),.239f,.144f,.106f,8,Olive,Quaternion.identity,2);s.Box(V(0,.121f,-.192f),V(.085f,.063f,.058f),Red);s.Box(V(-.078f,.108f,-.238f),V(.137f,.032f,.058f),Red,Quaternion.Euler(0,-24,0));s.Box(V(.079f,.105f,-.236f),V(.129f,.028f,.060f),Red,Quaternion.Euler(0,26,0));break;
        case 14:
            s.Box(V(0,.021f,0),V(.298f,.039f,.24f),Paper,Quaternion.Euler(0,-12,0));for(int j=0;j<3;j++){float h=.091f+j*.044f;var p=V((j-1)*.077f,.037f,(j==2?.033f:-.049f));s.Disc(p+V(0,h/2,0),.054f,h,6,Ivory);s.Disc(p+V(0,h+.011f,0),.009f,.023f,5,Wood);}break;
        case 15:
            s.Ring(V(0,.081f,.060f),.079f,.053f,.466f,8,Paper,sideways);
            s.Loft(V(.018f,.081f,.060f),8,Red,new[]{V(.086f,-.199f,.086f),V(.090f,.165f,.09f)},sideways,false);
            s.Prism(V(-.016f,.023f,-.068f),new[]{new Vector2(-.166f,-.10f),new Vector2(.167f,-.109f),new Vector2(.173f,.123f),new Vector2(.108f,.157f),new Vector2(.026f,.112f),new Vector2(-.053f,.164f),new Vector2(-.151f,.126f)},.014f,Red,upright);
            for(int j=0;j<2;j++){float x=-.094f+j*.151f;s.Box(V(x,.032f,-.088f),V(.015f,.007f,.158f),Ivory,Quaternion.Euler(0,-27,0));}
            break;
        case 16:
            s.Loft(V(0,0,0),7,Paper,new[]{V(.126f,0,.096f),V(.151f,.088f,.117f),V(.117f,.162f,.093f)});s.Ring(V(0,.163f,0),.120f,.092f,.063f,7,Oak);
            for(int j=0;j<4;j++)s.Loft(V((j%2-.5f)*.08f,.147f,(j/2-.5f)*.082f),5,Black,new[]{V(.045f,0,.036f),V(.040f,.072f+j*.008f,.029f),V(.016f,.084f+j*.008f,.014f)},Quaternion.Euler(j*8,12+j*17,0));break;
        case 17:
            for(int j=0;j<7;j++){float x=(j-3)*.031f;float y=(j%3)*.025f;s.Beam(V(x*.78f,y,-.169f),V(x*1.5f,y+.056f,.139f+j*.01f),.027f,.026f,Straw);}s.Box(V(0,.043f,-.025f),V(.21f,.106f,.059f),Paper);break;
        case 18:
            s.Box(V(0,.263f,0),V(.185f,.193f,.097f),Red);s.Box(V(0,.417f,0),V(.145f,.13f,.128f),Paper);s.Box(V(0,.542f,0),V(.151f,.138f,.140f),Red);s.Box(V(0,.48f,0),V(.154f,.027f,.141f),Brass);
            s.Box(V(0,.373f,-.071f),V(.112f,.061f,.046f),Ivory);s.Box(V(0,.411f,-.077f),V(.026f,.030f,.030f),Oak);
            foreach(float x in new[]{-.036f,.036f})s.Box(V(x,.444f,-.066f),V(.013f,.013f,.011f),Black);
            foreach(float x in new[]{-.112f,.112f}){s.Box(V(x,.247f,0),V(.045f,.153f,.066f),Red);s.Box(V(x,.319f,0),V(.062f,.036f,.075f),Brass);}
            s.Box(V(-.049f,.091f,0),V(.075f,.148f,.076f),Teal);s.Box(V(.049f,.112f,0),V(.077f,.105f,.076f),Teal,Quaternion.Euler(0,0,12));s.Box(V(-.051f,.019f,-.025f),V(.081f,.041f,.12f),Wood);s.Box(V(.091f,.018f,.044f),V(.056f,.033f,.061f),Wood,Quaternion.Euler(0,16,21));s.Rotate(Quaternion.Euler(90,22,0));break;
        case 19:
            s.Ring(V(0,.043f,0),.165f,.103f,.081f,9,Wood);s.Ring(V(0,.111f,0),.159f,.101f,.064f,9,Oak);s.Beam(V(.132f,.057f,-.069f),V(.151f,.028f,-.195f),.048f,.044f,Oak);s.Beam(V(.151f,.028f,-.195f),V(.091f,.024f,-.238f),.047f,.045f,Oak);break;
        }
        return s;
    }
    public static string Build()
    {
        if(AssetDatabase.IsValidFolder(Root))throw new Exception("LootConcept201 already exists; refuse overwrite");
        Folders(Root+"/Materials");Folders(Root+"/NewYear");Folders(Root+"/MainMaps");Materials();
        var sc=EditorSceneManager.NewPreviewScene();var report=new List<string>();
        try{for(int i=0;i<IDs.Length;i++){
            var shape=Model(i);var mesh=shape.Mesh(IDs[i]);var mats=shape.faces.Keys.Select(k=>palette[k]).ToArray();
            if(mesh.bounds.size.y<.1f||Mathf.Abs(mesh.bounds.min.y)>.001f||mesh.triangles.Length<30||mesh.triangles.Length/3>1500)throw new Exception("Invalid geometry "+IDs[i]);
            AssetDatabase.CreateAsset(mesh,Folder(i)+"/"+IDs[i]+"_Mesh.asset");
            var go=new GameObject(IDs[i]);SceneManager.MoveGameObjectToScene(go,sc);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=mats;
            if(!PrefabUtility.SaveAsPrefabAsset(go,Prefab(i)))throw new Exception("Cannot save "+IDs[i]);
            report.Add(IDs[i]+" | "+(MainMap(i)?"MainMaps":"NewYear")+" | dimensions "+mesh.bounds.size.ToString("F3")+" | "+(mesh.triangles.Length/3)+" triangles | ground "+mesh.bounds.min.y.ToString("F3"));
            UnityEngine.Object.DestroyImmediate(go);
        }}finally{EditorSceneManager.ClosePreviewScene(sc);}
        AssetDatabase.SaveAssets();Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/model-validation.txt",report);
        return string.Join("\n",report);
    }
    public static string Revise203()
    {
        var old=Root+"/NewYear/"+IDs[9];var to=Root+"/MainMaps/"+IDs[9];
        foreach(var suffix in new[]{".prefab","_Mesh.asset"})if(AssetDatabase.LoadMainAssetAtPath(old+suffix)){
            string error=AssetDatabase.MoveAsset(old+suffix,to+suffix);if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        var result=new List<string>();
        foreach(int i in new[]{4,5,9,15}){
            var s=Model(i);var mesh=s.Mesh(IDs[i]);var asset=AssetDatabase.LoadAssetAtPath<Mesh>(Folder(i)+"/"+IDs[i]+"_Mesh.asset");
            if(!asset||Mathf.Abs(mesh.bounds.min.y)>.001f||asset.subMeshCount!=mesh.subMeshCount) {
                if(!asset||Mathf.Abs(mesh.bounds.min.y)>.001f)throw new Exception("Cannot revise "+IDs[i]);
            }
            var guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            EditorUtility.CopySerialized(mesh,asset);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);
            var root=PrefabUtility.LoadPrefabContents(Prefab(i));try{
                root.GetComponent<MeshRenderer>().sharedMaterials=s.faces.Keys.Select(k=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Loot201_"+ColorNames[k]+".mat")).ToArray();
                if(!PrefabUtility.SaveAsPrefabAsset(root,Prefab(i)))throw new Exception(Prefab(i));
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            if(guid!=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)))throw new Exception("Unexpected mesh GUID change");
            result.Add(IDs[i]+" "+asset.bounds.size.ToString("F3")+" triangles="+asset.triangles.Length/3);
        }
        return string.Join("\n",result);
    }
}
