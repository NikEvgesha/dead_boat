using System.Collections.Generic;
using UnityEngine;

namespace DeadBoat.Online
{
    public static class CompanionVisual
    {
        // Copy only render data. Never instantiate an animal's gameplay scripts,
        // colliders, audio, income emitters or physics components, even for one frame.
        public static GameObject Create(GameObject prefab, Color tint, Transform parent)
        {
            if (prefab == null) return null;
            var map = new Dictionary<Transform, Transform>();
            Transform root = Copy(prefab.transform, parent);
            var block = new MaterialPropertyBlock();
            foreach (var source in prefab.GetComponentsInChildren<Renderer>(true))
            {
                Renderer target;
                if (source is SkinnedMeshRenderer skin)
                {
                    var clone = map[source.transform].gameObject.AddComponent<SkinnedMeshRenderer>();
                    clone.sharedMesh = skin.sharedMesh; clone.localBounds = skin.localBounds;
                    var bones = skin.bones; var mapped = new Transform[bones.Length];
                    for (int i = 0; i < bones.Length; i++) if (bones[i] != null) map.TryGetValue(bones[i], out mapped[i]);
                    clone.bones = mapped;
                    if (skin.rootBone != null && map.TryGetValue(skin.rootBone, out var bone)) clone.rootBone = bone;
                    target = clone;
                }
                else if (source is MeshRenderer && source.TryGetComponent<MeshFilter>(out var filter))
                {
                    map[source.transform].gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    target = map[source.transform].gameObject.AddComponent<MeshRenderer>();
                }
                else continue;
                target.enabled = source.enabled;
                target.sharedMaterials = source.sharedMaterials;
                target.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                target.receiveShadows = false;
                block.SetColor("_Color", tint); block.SetColor("_BaseColor", tint); target.SetPropertyBlock(block);
            }
            root.localScale *= .25f;
            return root.gameObject;

            Transform Copy(Transform source, Transform owner)
            {
                var result = new GameObject(source.name).transform;
                result.SetParent(owner, false);
                result.localPosition = source.localPosition; result.localRotation = source.localRotation; result.localScale = source.localScale;
                result.gameObject.SetActive(source.gameObject.activeSelf);
                map.Add(source, result);
                foreach (Transform child in source) Copy(child, result);
                return result;
            }
        }
    }
}
