using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TextureTreeHouse192
{
    const string Folder = "Assets/Prefabs/Workshop/TreeHousePrototypes";
    const string PrefabPath = Folder + "/TreeHouse_Original_Refined.prefab";
    const string MeshPath = Folder + "/TreeHouse_Textured192.asset";

    static float Noise(int x, int y)
    {
        uint seed = unchecked((uint)(x * 11369 + y * 53323 + 2917));
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return (seed & 65535) / 65535f;
    }

    static Texture2D MakePattern(string name, bool shingles)
    {
        const int size = 256;
        const int rows = 4;
        const int rowHeight = size / rows;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            int row = y / rowHeight;
            int rowY = y % rowHeight;
            for (int x = 0; x < size; x++)
            {
                int length = shingles ? 64 : 128;
                int stagger = (row % 2) * (length / 2);
                int tileX = (x + stagger) % size;
                int joint = tileX % length;
                int board = tileX / length;
                float variation = Noise(board,row);
                float value = .78f + variation * .15f;
                if (rowY < 2) value *= .57f;
                else if (rowY < 5) value *= .82f;
                else if (rowY == 5) value *= 1.06f;
                if (joint < (shingles ? 2 : 1)) value *= .68f;
                if (shingles)
                    value += .045f * rowY / rowHeight;
                else
                {
                    float grain = Mathf.Sin((rowY + 1.3f * Mathf.Sin(x * (2 * Mathf.PI / size) * 3 + row)) * 1.8f);
                    value += grain * .022f;
                }
                byte shade = (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255),0,255);
                pixels[y*size+x] = new Color32(shade,shade,shade,255);
            }
        }
        var texture = new Texture2D(size,size,TextureFormat.RGBA32,true,false)
        {
            name=name, wrapMode=TextureWrapMode.Repeat, filterMode=FilterMode.Trilinear, anisoLevel=8
        };
        texture.SetPixels32(pixels);
        texture.Apply(true,false);
        AssetDatabase.CreateAsset(texture,Folder+"/"+name+".asset");
        return texture;
    }

    static Material MaterialFor(Material source, string name, Texture2D texture, string tint)
    {
        var material = new Material(source) {name=name, mainTexture=texture, mainTextureScale=Vector2.one};
        if (!ColorUtility.TryParseHtmlString(tint, out var color)) throw new InvalidOperationException("Invalid color");
        material.color=color;
        if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.15f);
        AssetDatabase.CreateAsset(material,Folder+"/"+name+".mat");
        return material;
    }

    static Vector2 Coordinates(Vector3 point, Vector3 normal, bool roof)
    {
        if (roof) return new Vector2(point.x/2.70f,point.z/2.12f);
        if (Mathf.Abs(normal.y) >= .55f) return new Vector2(point.x/4.8f,point.z/1.16f);
        float horizontal = Mathf.Abs(normal.x) > Mathf.Abs(normal.z) ? point.z : point.x;
        return new Vector2(horizontal/4.8f,point.y/1.16f);
    }

    static Mesh MapHouse(MeshFilter filter, out int roofFaceCount)
    {
        var source = filter.sharedMesh;
        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = "TreeHouse_Textured";
        var points = source.vertices.Select(filter.transform.TransformPoint).ToArray();
        var uv = source.uv;
        var roofIndices = new List<int>();
        int roofSlot = source.subMeshCount;
        mesh.subMeshCount = roofSlot + 1;
        foreach(int sub in new[]{0,1,2})
        {
            var indices=source.GetTriangles(sub);
            var remaining=new List<int>(indices.Length);
            for(int i=0;i<indices.Length;i+=3)
            {
                int a=indices[i], b=indices[i+1], c=indices[i+2];
                var normal = Vector3.Cross(points[b]-points[a],points[c]-points[a]).normalized;
                var center=(points[a]+points[b]+points[c])/3;
                bool roof=sub==0 && center.y>8 && normal.y>.3f && normal.y<.995f &&
                    Mathf.Abs(normal.x)<.06f && Mathf.Abs(normal.z)>.18f;
                uv[a]=Coordinates(points[a],normal,roof);
                uv[b]=Coordinates(points[b],normal,roof);
                uv[c]=Coordinates(points[c],normal,roof);
                var list=roof ? roofIndices : remaining;
                list.Add(a);list.Add(b);list.Add(c);
            }
            mesh.SetTriangles(remaining,sub,false);
        }
        mesh.uv=uv;
        mesh.SetTriangles(roofIndices,roofSlot,false);
        roofFaceCount=roofIndices.Count/3;
        return mesh;
    }

    public static string Main()
    {
        if(AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath))throw new InvalidOperationException("Texture pass already saved");
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var filter=root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="dom");
            var renderer=filter.GetComponent<Renderer>();
            var originals=renderer.sharedMaterials;
            var wood=MakePattern("TreeHouse_BoardPattern192",false);
            var shingles=MakePattern("TreeHouse_ShinglePattern192",true);
            var trim=MaterialFor(originals[0],"TreeHouse_DarkBoards192",wood,"#77513B");
            var siding=MaterialFor(originals[1],"TreeHouse_WarmSiding192",wood,"#B48159");
            var deck=MaterialFor(originals[2],"TreeHouse_DeckBoards192",wood,"#C0946D");
            var roof=MaterialFor(originals[0],"TreeHouse_ShingleRoof192",shingles,"#736050");
            var infill=MaterialFor(originals[1],"TreeHouse_SidingInfill192",wood,"#B48159");
            infill.mainTextureScale=new Vector2(.85f,3.65f);
            EditorUtility.SetDirty(infill);

            var mesh=MapHouse(filter,out int roofFaces);
            if(roofFaces<12)throw new InvalidOperationException("Roof surfaces were not mapped");
            AssetDatabase.CreateAsset(mesh,MeshPath);
            filter.sharedMesh=mesh;
            var materials=originals.Concat(new[]{roof}).ToArray();
            materials[0]=trim;materials[1]=siding;materials[2]=deck;
            renderer.sharedMaterials=materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            foreach(var wall in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Lower_Room_Closed_Side_Opening" || r.name=="Upper_Room_Closed_Far_Door"))
            {
                wall.sharedMaterial=infill;
                PrefabUtility.RecordPrefabInstancePropertyModifications(wall);
            }
            if(!PrefabUtility.SaveAsPrefabAsset(root,PrefabPath))throw new InvalidOperationException("Save failed");
            AssetDatabase.SaveAssets();
            return $"Applied board textures to wall, deck and structural surfaces; mapped {roofFaces} shingle faces";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
