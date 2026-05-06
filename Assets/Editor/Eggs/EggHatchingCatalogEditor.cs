using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EggHatchingCatalog))]
public sealed class EggHatchingCatalogEditor : Editor
{
    private readonly Dictionary<Object, bool> _foldouts = new();

    private SerializedProperty _eggs;
    private SerializedProperty _animals;

    private void OnEnable()
    {
        _eggs = serializedObject.FindProperty("_eggs");
        _animals = serializedObject.FindProperty("_animals");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((EggHatchingCatalog)target), typeof(MonoScript), false);

        if (GUILayout.Button("Sync Zoo Prefabs To Catalog", GUILayout.Height(28f)))
        {
            EggZooCatalogSync.SyncDefaultFolders();
            serializedObject.Update();
        }

        EditorGUILayout.Space(6f);

        DrawAssetList("Eggs", _eggs, typeof(EggDefinition));
        EditorGUILayout.Space(8f);
        DrawAssetList("Animals", _animals, typeof(AnimalDefinition));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAssetList(string label, SerializedProperty list, System.Type assetType)
    {
        if (list == null)
            return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        using (new EditorGUILayout.HorizontalScope())
        {
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, $"{label} ({list.arraySize})", true, EditorStyles.foldoutHeader);

            if (GUILayout.Button("+", GUILayout.Width(28f)))
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = null;
            }
        }

        if (list.isExpanded)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < list.arraySize; i++)
                DrawAssetListElement(list, i, assetType);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAssetListElement(SerializedProperty list, int index, System.Type assetType)
    {
        SerializedProperty element = list.GetArrayElementAtIndex(index);
        Object asset = element.objectReferenceValue;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool canExpand = asset != null;
                bool expanded = canExpand && GetFoldout(asset);

                using (new EditorGUI.DisabledScope(!canExpand))
                {
                    bool nextExpanded = EditorGUILayout.Foldout(expanded, string.Empty, true);
                    if (canExpand && nextExpanded != expanded)
                        _foldouts[asset] = nextExpanded;
                }

                EditorGUILayout.PropertyField(element, new GUIContent($"Element {index}"));

                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    DeleteArrayElement(list, index);
                    return;
                }
            }

            if (asset == null)
                DrawCreateAssetButton(element, assetType);
            else if (GetFoldout(asset))
                DrawNestedAsset(asset);
        }
    }

    private void DrawCreateAssetButton(SerializedProperty element, System.Type assetType)
    {
        string assetName = assetType == typeof(EggDefinition) ? "egg_new" : "animal_new";
        if (!GUILayout.Button($"Create {assetType.Name}", GUILayout.Height(22f)))
            return;

        string path = EditorUtility.SaveFilePanelInProject(
            $"Create {assetType.Name}",
            assetName,
            "asset",
            "Choose where to create the config asset.",
            "Assets/Resources/Eggs/Definitions");

        if (string.IsNullOrWhiteSpace(path))
            return;

        ScriptableObject created = CreateInstance(assetType);
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path);

        element.objectReferenceValue = created;
        _foldouts[created] = true;
    }

    private void DrawNestedAsset(Object asset)
    {
        EditorGUI.indentLevel++;

        if (asset is EggNamedDefinition namedDefinition)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Generated Id", namedDefinition.Id);
        }

        SerializedObject nested = new SerializedObject(asset);
        nested.Update();

        SerializedProperty iterator = nested.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.propertyPath == "m_Script" || iterator.propertyPath == "_id")
                continue;

            EditorGUILayout.PropertyField(iterator, true);
        }

        if (nested.ApplyModifiedProperties())
            EditorUtility.SetDirty(asset);

        EditorGUI.indentLevel--;
    }

    private bool GetFoldout(Object asset)
    {
        return asset != null && _foldouts.TryGetValue(asset, out bool expanded) && expanded;
    }

    private static void DeleteArrayElement(SerializedProperty list, int index)
    {
        SerializedProperty element = list.GetArrayElementAtIndex(index);
        if (element.objectReferenceValue != null)
            element.objectReferenceValue = null;

        list.DeleteArrayElementAtIndex(index);
    }
}
