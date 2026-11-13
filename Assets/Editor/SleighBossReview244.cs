using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DeadBoat.Events.NewYear;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Reproducible local encounter; the saved LevelEvent and final boss stay intact.</summary>
[InitializeOnLoad]
public static class SleighBossReview244
{
    public const string Folder = "Assets/Events/NewYear/Boss244";
    public const string BossPath = Folder + "/SleighThief_Boss.prefab";
    const string Pending = "Sleigh244.Pending";
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static bool loading;

    static SleighBossReview244() { EditorApplication.update += Poll; }

    [MenuItem("Tools/New Year/Build Sleigh Boss Assets")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build in Edit Mode");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var colors = new[] { new Color(.83f,.12f,.19f), new Color(.08f,.49f,.95f), new Color(1,.63f,.04f) };
        var names = new[] { "Red_Small", "Blue_Medium", "Gold_Large" };
        var sizes = new[] { .65f, 1f, 1.5f };
        var ribbon = Material("Ribbon_Cream", new Color(1,.92f,.7f), false);
        var projectiles = new List<SleighGiftAttack>();
        for (int i = 0; i < 3; i++)
        {
            var root = new GameObject("Gift_" + names[i]);
            try
            {
                var present = new GameObject("Present"); present.transform.SetParent(root.transform, false);
                present.transform.localScale = Vector3.one * sizes[i];
                Box(present.transform, "Box", Vector3.zero, Vector3.one, Material(names[i], colors[i], false));
                Box(present.transform, "Lid", new Vector3(0,.39f,0), new Vector3(1.06f,.23f,1.06f), Material(names[i], colors[i], false));
                Box(present.transform, "Ribbon_X", new Vector3(0,.515f,0), new Vector3(1.075f,.035f,.16f), ribbon);
                Box(present.transform, "Ribbon_Z", new Vector3(0,.52f,0), new Vector3(.16f,.04f,1.075f), ribbon);
                Box(present.transform, "Ribbon_Front", new Vector3(0,0,.506f), new Vector3(.16f,1.02f,.025f), ribbon);
                Box(present.transform, "Ribbon_Back", new Vector3(0,0,-.506f), new Vector3(.16f,1.02f,.025f), ribbon);
                Box(present.transform, "Bow_Left", new Vector3(-.16f,.62f,0), new Vector3(.34f,.17f,.22f), ribbon).localRotation = Quaternion.Euler(0,0,-25);
                Box(present.transform, "Bow_Right", new Vector3(.16f,.62f,0), new Vector3(.34f,.17f,.22f), ribbon).localRotation = Quaternion.Euler(0,0,25);
                var attack = root.AddComponent<SleighGiftAttack>();
                var so = new SerializedObject(attack);
                so.FindProperty("giftVisual").objectReferenceValue = present.transform;
                so.FindProperty("warningMaterial").objectReferenceValue = Material("Warning_" + names[i], colors[i], true);
                so.ApplyModifiedPropertiesWithoutUndo();
                string path = Folder + "/Gift_" + names[i] + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                projectiles.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<SleighGiftAttack>());
            }
            finally { Object.DestroyImmediate(root); }
        }
        var boss = new GameObject("SleighThief_Boss");
        // Existing ranged weapons hit Ground and Pickable layers, including enemy hulls.
        boss.layer = LayerMask.NameToLayer("Ground"); boss.tag = "Enemy";
        try
        {
            var visual = new GameObject("VisualRoot"); visual.transform.SetParent(boss.transform, false);
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Models/SleighThief_Visual.prefab"), visual.transform);
            model.name = "SleighThief_Model";
            var drop = new GameObject("DropAnchor").transform; drop.SetParent(boss.transform, false); drop.localPosition = new Vector3(0,.5f,-2.4f);
            var box = boss.AddComponent<BoxCollider>(); box.center = new Vector3(0,.65f,0); box.size = new Vector3(2.8f,2,4.8f);
            var rb = boss.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            visual.AddComponent<DamageFlash>();
            var controller = boss.AddComponent<SleighBossController>();
            var so = new SerializedObject(controller);
            so.FindProperty("visualRoot").objectReferenceValue = visual.transform;
            so.FindProperty("dropAnchor").objectReferenceValue = drop;
            so.FindProperty("engine").objectReferenceValue = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Engine_Ice");
            so.FindProperty("engageDistance").floatValue = 28;
            so.FindProperty("volleysBeforeRecovery").intValue = 3;
            so.FindProperty("recoveryDuration").floatValue = 4.5f;
            so.FindProperty("flightSpeedMultiplier").floatValue = 2;
            so.FindProperty("giftsPerVolley").intValue = 5;
            so.FindProperty("bombardmentDuration").floatValue = 5;
            so.FindProperty("timeBetweenGifts").floatValue = .45f;
            var voice = AddSoundSource(boss.transform, "Mischievous_Laugh", .85f);
            var throws = AddSoundSource(drop, "Gift_Throw_Sound", .65f);
            so.FindProperty("laughSource").objectReferenceValue = voice;
            so.FindProperty("throwSource").objectReferenceValue = throws;
            var laughs = so.FindProperty("laughClips"); laughs.arraySize = 3;
            for(int i=0;i<3;i++) laughs.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Audio/Sleigh_Chuckle_" + (i+1) + ".wav");
            so.FindProperty("throwClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Audio/Gift_Throw.wav");
            var array = so.FindProperty("gifts"); array.arraySize = 3;
            for (int i = 0; i < 3; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = projectiles[i];
            var fallingIce=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Events/NewYear/Boss250/Icicle_Falling.prefab");
            if(fallingIce)
            {
                so.FindProperty("icicleProjectile").objectReferenceValue=fallingIce.GetComponent<SleighGiftAttack>();
                var ice=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Engine_Ice",StringComparison.Ordinal)).OrderBy(t=>t.name).ToArray();
                var attached=so.FindProperty("hangingIcicles");attached.arraySize=ice.Length;
                for(int i=0;i<ice.Length;i++)attached.GetArrayElementAtIndex(i).objectReferenceValue=ice[i];
            }
            boss.AddComponent<SleighBossEncounter>();
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(boss, BossPath);
        }
        finally { Object.DestroyImmediate(boss); }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/New Year/Restore Angry Eyebrows")]
    public static void SaveAngryBrows()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Save expression in Edit Mode");
        const string visualPath = "Assets/Events/NewYear/Models/SleighThief_Visual.prefab";
        var visual = PrefabUtility.LoadPrefabContents(visualPath);
        try
        {
            var brows = visual.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Brow_")).ToArray();
            if(brows.Length != 2) throw new InvalidOperationException("Expected two editable eyebrows");
            foreach(var brow in brows)
            {
                // Imported model uses a rotated parent: inner tips must point DOWN in Unity.
                brow.localRotation = Quaternion.Euler(0,brow.name=="Brow_-1"?22:-22,0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(brow);
            }
            PrefabUtility.SaveAsPrefabAsset(visual,visualPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(visual); }
    }

    static AudioSource AddSoundSource(Transform parent, string label, float volume)
    {
        var go = new GameObject(label); go.transform.SetParent(parent,false);
        var sound = go.AddComponent<AudioSource>();
        sound.playOnAwake=false; sound.loop=false; sound.volume=volume; sound.spatialBlend=1;
        sound.dopplerLevel=0; sound.rolloffMode=AudioRolloffMode.Linear; sound.minDistance=8; sound.maxDistance=65;
        var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/Igrodelnya/Sound/AudioMixer.mixer");
        if(mixer) sound.outputAudioMixerGroup=mixer.FindMatchingGroups("Sound").FirstOrDefault();
        return sound;
    }

    static Material Material(string label, Color color, bool unlit)
    {
        string path = Folder + "/" + label + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            if (!shader) throw new InvalidOperationException("Missing gift shader");
            mat = new Material(shader); AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .12f);
        EditorUtility.SetDirty(mat); return mat;
    }

    static Transform Box(Transform parent, string label, Vector3 pos, Vector3 scale, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = label;
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.layer = 2; box.transform.SetParent(parent, false);
        box.transform.localPosition = pos; box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box.transform;
    }

    [MenuItem("Tools/New Year/Play Sleigh Boss at Event Start")]
    public static void Play()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit the current Play Mode, then start the review");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save the open scenes before starting Play Mode");
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(BossPath)) Build();
        SessionState.SetString("Sleigh244.PriorStart", EditorSceneManager.playModeStartScene ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "");
        DeadBoat.Online.Editor.SharedPerformanceTestSave.Prepare();
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/LoadingScene.unity");
        SessionState.SetBool(Pending, true); loading = false;
        EditorApplication.EnterPlaymode();
    }

    static void Poll()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (!LoadingManager.Instance || !SaveManager.Instance || !(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave)) return;
            if (SceneManager.GetActiveScene().name != "LevelEvent")
            {
                if (!loading && SceneManager.GetActiveScene().name == "Lobby")
                { loading = true; LoadingManager.Instance.LoadLocation(Location.Game, "LevelEvent", false); }
                return;
            }
            if (!PlayerMovement.Instance || !BoardController.Instance || !BoardController.Instance.StartGame) return;
            SessionState.SetBool(Pending, false);
            OpenEncounter();
        }
        catch (Exception e) { SessionState.SetBool(Pending, false); Debug.LogException(e); RestoreStart(); }
    }

    static void RestoreStart()
    {
        string prior = SessionState.GetString("Sleigh244.PriorStart", "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(prior) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(prior);
    }

    public static string OpenEncounter()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "LevelEvent" ||
            !(SaveManager.Instance.PersistentProvider is DeadBoat.Online.SharedRunPreviewSave))
            throw new InvalidOperationException("Use isolated LevelEvent gameplay");
        if (Object.FindAnyObjectByType<SleighBossController>()) throw new InvalidOperationException("Encounter already running");
        foreach (var spawner in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b => b &&
            new[] { "BossSpawner", "ZombieSpawner" }.Contains(b.GetType().Name)))
        { spawner.StopAllCoroutines(); spawner.enabled = false; }
        foreach (var enemy in Object.FindObjectsByType<EnemyCore>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
        var preview = GameObject.Find("SleighThief_VisualPreview"); if (preview) preview.SetActive(false);
        var player = PlayerMovement.Instance;
        PlayerInput.Instance.SitTrain(false); player.transform.SetParent(null);
        // Start courtyard: find an unobstructed actual surface beside the initial player.
        Vector3 start = player.transform.position;
        var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BossPath)).GetComponent<SleighBossController>();
        boss.name = "SleighThief_StartFight_Review";
        Vector3 preferred = new Vector3(7, start.y, 7);
        if (boss.FindSurface(preferred, out var ground)) start = ground.point + Vector3.up * .1f;
        player.Teleport(start);
        boss.transform.position = start + new Vector3(0, 18, -34);
        boss.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(start - boss.transform.position, Vector3.up));
        float yaw = Quaternion.LookRotation(boss.transform.position - start).eulerAngles.y;
        typeof(PlayerMovement).GetField("_startYRotation", Fields).SetValue(player, yaw);
        typeof(PlayerMovement).GetField("_currentYRotation", Fields).SetValue(player, 0f);
        typeof(PlayerMovement).GetField("_currentXRotation", Fields).SetValue(player, -27f);
        player.transform.rotation = Quaternion.Euler(0,yaw,0);
        var rifle = Inventory.Instance.GetInventoryList().FirstOrDefault(i => i && i.GetComponentInChildren<RangedWeaponController>(true));
        if (!rifle)
        {
            rifle = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Rifle.prefab")).GetComponent<PickableItem>();
            if (!Inventory.Instance.AddItem(rifle)) throw new InvalidOperationException("No inventory space for review rifle");
        }
        PlayerAmmoManager.Instance.AddAmmo(WeaponType.Rifle, 240);
        Object.FindAnyObjectByType<ActiveItemManager>().SwitchActiveItem(rifle);
        rifle.GetComponentInChildren<RangedWeaponController>(true).ApplySharedAmmo(1);
        PlayerStatsManager.Instance.Revive();
        var diagnostics = Object.FindAnyObjectByType<DeadBoat.Online.MenuInputDiagnostics>();
        if (diagnostics) diagnostics.enabled = false;
        if (DayTime.instanse)
        {
            typeof(DayTime).GetField("_timeOfDay", Fields).SetValue(DayTime.instanse, 12f);
            typeof(DayTime).GetField("_currentOrbitSpeed", Fields).SetValue(DayTime.instanse, 0f);
            typeof(DayTime).GetMethod("ProgressTime", Fields).Invoke(DayTime.instanse, null);
        }
        PauseManager.Instance.SetPause(false); ControlManager.Instance.MoveActive = true;
        ControlManager.Instance.ForceGameplayCursor();
        RestoreStart();
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        return "LevelEvent local sleigh review ready; approach within 28 horizontal metres to trigger its attack";
    }
}
