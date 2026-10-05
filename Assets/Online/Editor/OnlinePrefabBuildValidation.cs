using Fusion;
using Fusion.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeadBoat.Online.Editor
{
    // Runtime Resources.Load alone does not register a prefab in Fusion's imported table.
    public sealed class OnlinePrefabBuildValidation : IPreprocessBuildWithReport
    {
        private static readonly string[] RequiredPrefabs =
        {
            "Assets/Online/Avatars/LobbyNetworkAvatar.prefab",
            "Assets/Resources/Online/SharedDepartureState.prefab"
            ,"Assets/Resources/Online/SharedWorldPage.prefab"
        };

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report) => Prepare();

        [MenuItem("Tools/Online/Prepare and Validate Network Prefabs")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying)
                throw new BuildFailedException("Stop Play Mode before preparing network prefabs.");

            SharedItemCatalogBuilder.Prepare();
            foreach (string path in RequiredPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
                    throw new BuildFailedException($"Required network prefab missing: {path}");
                if (System.Array.IndexOf(AssetDatabase.GetLabels(prefab), "FusionPrefab") < 0)
                    throw new BuildFailedException($"Required network prefab has no FusionPrefab label: {path}");
                int words = 64; // Conservative header allowance; Fusion's allocation ceiling is 32 KiB.
                foreach (var behaviour in prefab.GetComponents<NetworkBehaviour>())
                    words += NetworkBehaviourUtils.GetWordCount(behaviour);
                if (words * 4 >= 32000)
                    throw new BuildFailedException($"Network prefab exceeds safe state size: {path} ({words * 4} bytes)");
            }

            // Use the SDK's public importer; do not edit Photon or Mirra SDK sources.
            if (!FusionGlobalScriptableObjectUtils.TryGetGlobalAssetPath<NetworkProjectConfigAsset>(out var configPath))
                throw new BuildFailedException("Could not locate the global Fusion config.");
            AssetDatabase.ImportAsset(configPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            NetworkProjectConfig.UnloadGlobal();
            foreach (string path in RequiredPrefabs)
            {
                var guid = NetworkObjectGuid.Parse(AssetDatabase.AssetPathToGUID(path));
                var id = NetworkProjectConfig.Global.PrefabTable.GetId(guid);
                if (!id.IsValid)
                    throw new BuildFailedException($"Fusion prefab table is missing {path}. Rebuild the prefab table before building.");
            }
            Debug.Log("[Online build] Required network prefabs registered: avatar and departure state.");
        }
    }
}
