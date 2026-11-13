using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;

namespace DeadBoat.EditorChecks
{
    public static class WebDependencyAudit
    {
        [Serializable] private sealed class RootRow { public string path; public string reason; }
        [Serializable] private sealed class Dependency
        {
            public string path;
            public long sourceBytes = -1;
            public bool inLoadingSceneClosure;
            public string[] reasons;
            public string[] flags;
        }
        [Serializable] private sealed class SizeGroup { public string extension; public int count; public long availableSourceBytes; }
        [Serializable] private sealed class Setting { public string name; public string value; public string type; public bool supported; }
        [Serializable] private sealed class TexturePlatform
        {
            public string platform;
            public bool overridden;
            public int maxTextureSize;
            public string format;
            public string textureCompression;
            public int compressionQuality;
            public bool crunchedCompression;
        }
        [Serializable] private sealed class TextureRow
        {
            public string path;
            public bool readable;
            public bool mipmaps;
            public bool streamingMipmaps;
            public int maxTextureSize;
            public string textureType;
            public string textureCompression;
            public bool sourceDimensionsAvailable;
            public int sourceWidth;
            public int sourceHeight;
            public TexturePlatform defaults;
            public TexturePlatform webGL;
        }
        [Serializable] private sealed class AudioSample
        {
            public string loadType;
            public string compressionFormat;
            public float quality;
            public string sampleRateSetting;
            public uint sampleRateOverride;
            public string preloadAudioData;
        }
        [Serializable] private sealed class AudioRow
        {
            public string path;
            public bool loadInBackground;
            public bool forceToMono;
            public string importerPreloadAudioData;
            public bool hasWebGLOverride;
            public AudioSample defaults;
            public AudioSample webGL;
        }
        [Serializable] private sealed class ModelRow
        {
            public string path;
            public bool readable;
            public bool importAnimation;
            public string meshCompression;
            public string animationCompression;
        }
        [Serializable] private sealed class StreamingFile { public string path; public long sourceBytes; }
        [Serializable] private sealed class Report
        {
            public string utc;
            public string unityVersion;
            public string activeEditorBuildTarget;
            public string scope = "AssetDatabase explicit dependency closure for enabled scenes, runtime Resources and preloaded assets. Source bytes are NOT imported memory, compressed WebGL payload, download size or resident memory. LoadingScene closure excludes additional runtime Resources.Load and future code paths. Folder flags are diagnostics, not proof of unused assets. No Play/Build/save/importer changes/Texture2D bulk loads.";
            public string webGLManagedStripping;
            public string webGLScriptingBackend;
            public bool stripEngineCode;
            public bool editorDevelopmentFlag;
            public string currentEditorQuality;
            public int currentEditorGlobalTextureMipmapLimit;
            public int rootCount;
            public int enabledSceneCount;
            public int runtimeResourceRootCount;
            public int preloadedAssetCount;
            public int dependencyCount;
            public long availableDependencySourceBytes;
            public int dependencySourceByteCountUnavailable;
            public int loadingSceneExplicitDependencyCount;
            public long loadingSceneAvailableSourceBytes;
            public bool passed;
            public List<RootRow> roots = new List<RootRow>();
            public List<Setting> nativeWebGLSettings = new List<Setting>();
            public List<Dependency> dependencies = new List<Dependency>();
            public List<Dependency> top30BySourceBytes = new List<Dependency>();
            public List<SizeGroup> sourceSizesByExtension = new List<SizeGroup>();
            public List<string> loadingSceneExplicitDependencies = new List<string>();
            public List<TextureRow> textures = new List<TextureRow>();
            public List<AudioRow> audio = new List<AudioRow>();
            public List<ModelRow> models = new List<ModelRow>();
            public List<StreamingFile> streamingAssets = new List<StreamingFile>();
            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();
        }

        public static void Run()
        {
            var result = new Report { utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Use Edit Mode: existing gameplay was not stopped.");
                result.activeEditorBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                result.editorDevelopmentFlag = EditorUserBuildSettings.development;
                result.webGLManagedStripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL).ToString();
                result.webGLScriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.WebGL).ToString();
                result.stripEngineCode = PlayerSettings.stripEngineCode;
                int quality = QualitySettings.GetQualityLevel();
                result.currentEditorQuality = quality >= 0 && quality < QualitySettings.names.Length ? QualitySettings.names[quality] : quality.ToString();
                result.currentEditorGlobalTextureMipmapLimit = QualitySettings.globalTextureMipmapLimit;
                foreach (string name in new[] { "compressionFormat", "decompressionFallback", "dataCaching", "memoryGrowthMode", "initialMemorySize", "maximumMemorySize", "memoryGeometricGrowthStep", "memoryGeometricGrowthCap", "memoryLinearGrowthStep", "threadsSupport", "debugSymbolMode", "exceptionSupport", "textureCompressionFormat" })
                    result.nativeWebGLSettings.Add(ReadSetting(typeof(PlayerSettings.WebGL), null, name));

                foreach (var scene in EditorBuildSettings.scenes)
                    if (scene.enabled) { AddRoot(result, scene.path, "enabled-scene"); result.enabledSceneCount++; }
                foreach (string path in AssetDatabase.GetAllAssetPaths())
                {
                    string normalized = path.Replace('\\', '/');
                    string[] parts = normalized.Split('/');
                    if (!parts.Contains("Resources") || parts.Contains("Editor") || parts.Any(value => value.EndsWith("~", StringComparison.Ordinal))) continue;
                    if (!(normalized.StartsWith("Assets/", StringComparison.Ordinal) || normalized.StartsWith("Packages/", StringComparison.Ordinal))) continue;
                    if (AssetDatabase.IsValidFolder(normalized)) continue;
                    AddRoot(result, normalized, "runtime-resources"); result.runtimeResourceRootCount++;
                }
                var preloaded = PlayerSettings.GetPreloadedAssets();
                result.preloadedAssetCount = preloaded == null ? 0 : preloaded.Length;
                if (preloaded != null) foreach (var asset in preloaded)
                {
                    string path = asset ? AssetDatabase.GetAssetPath(asset) : null;
                    if (string.IsNullOrEmpty(path)) result.warnings.Add("A preloaded slot has no asset path.");
                    else AddRoot(result, path, "preloaded-asset");
                }
                result.rootCount = result.roots.Count;
                const string loading = "Assets/Scenes/LoadingScene.unity";
                var startup = new HashSet<string>(AssetDatabase.GetDependencies(loading, true), StringComparer.Ordinal);
                result.loadingSceneExplicitDependencies = startup.OrderBy(value => value, StringComparer.Ordinal).ToList();
                result.loadingSceneExplicitDependencyCount = startup.Count;

                var reasons = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
                foreach (var root in result.roots)
                {
                    foreach (string path in AssetDatabase.GetDependencies(root.path, true))
                    {
                        if (!reasons.TryGetValue(path, out var list)) reasons.Add(path, list = new HashSet<string>(StringComparer.Ordinal));
                        list.Add(root.reason + ":" + root.path);
                    }
                }
                foreach (var entry in reasons.OrderBy(value => value.Key, StringComparer.Ordinal))
                {
                    var row = new Dependency
                    {
                        path = entry.Key, sourceBytes = SourceLength(entry.Key), inLoadingSceneClosure = startup.Contains(entry.Key),
                        reasons = entry.Value.OrderBy(value => value, StringComparer.Ordinal).ToArray(), flags = Flags(entry.Key)
                    };
                    result.dependencies.Add(row);
                    if (row.sourceBytes >= 0) { result.availableDependencySourceBytes += row.sourceBytes; if (row.inLoadingSceneClosure) result.loadingSceneAvailableSourceBytes += row.sourceBytes; }
                    else result.dependencySourceByteCountUnavailable++;
                    CollectImporter(entry.Key, result);
                }
                result.dependencyCount = result.dependencies.Count;
                result.top30BySourceBytes = result.dependencies.OrderByDescending(value => value.sourceBytes).ThenBy(value => value.path, StringComparer.Ordinal).Take(30).ToList();
                foreach (var group in result.dependencies.GroupBy(value => Path.GetExtension(value.path).ToLowerInvariant()).OrderBy(value => value.Key, StringComparer.Ordinal))
                    result.sourceSizesByExtension.Add(new SizeGroup { extension = group.Key, count = group.Count(), availableSourceBytes = group.Where(value => value.sourceBytes >= 0).Sum(value => value.sourceBytes) });
                if (Directory.Exists(Application.streamingAssetsPath))
                    foreach (string path in Directory.GetFiles(Application.streamingAssetsPath, "*", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.Ordinal))
                        if (!path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                            result.streamingAssets.Add(new StreamingFile { path = "Assets/StreamingAssets/" + path.Substring(Application.streamingAssetsPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/'), sourceBytes = new FileInfo(path).Length });
            }
            catch (Exception exception) { result.errors.Add(exception.ToString()); }
            finally
            {
                result.passed = result.errors.Count == 0;
                string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "WebOptimizationAudit20261113", "dependencies.json");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(result, true));
                Debug.Log("[WebDependencyAudit] roots=" + result.rootCount + "; dependencies=" + result.dependencyCount + "; textures=" + result.textures.Count + "; audio=" + result.audio.Count + "; errors=" + result.errors.Count + "; report=" + output);
            }
            if (!result.passed) throw new InvalidOperationException("Web dependency audit failed; inspect dependencies.json.");
        }

        private static void AddRoot(Report report, string path, string reason)
        {
            if (!report.roots.Any(value => value.path == path && value.reason == reason)) report.roots.Add(new RootRow { path = path, reason = reason });
        }
        private static string[] Flags(string path)
        {
            var result = new List<string>();
            if (path.Contains("/Resources/")) result.Add("Resources");
            if (path.IndexOf("workshop", StringComparison.OrdinalIgnoreCase) >= 0) result.Add("workshop-name");
            if (path.StartsWith("Assets/Feel/", StringComparison.Ordinal)) result.Add("Feel");
            if (path.IndexOf("demo", StringComparison.OrdinalIgnoreCase) >= 0) result.Add("demo-name");
            if (path.IndexOf("sample", StringComparison.OrdinalIgnoreCase) >= 0) result.Add("sample-name");
            return result.ToArray();
        }
        private static long SourceLength(string path)
        {
            string physical = path.StartsWith("Assets/", StringComparison.Ordinal) ? Path.Combine(Path.GetDirectoryName(Application.dataPath), path) : path;
            if (path.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
                if (package != null && path.StartsWith(package.assetPath + "/", StringComparison.Ordinal))
                    physical = Path.Combine(package.resolvedPath, path.Substring(package.assetPath.Length + 1));
            }
            return File.Exists(physical) ? new FileInfo(physical).Length : -1;
        }
        private static Setting ReadSetting(Type type, object instance, string name)
        {
            var row = new Setting { name = name };
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            try
            {
                var property = type.GetProperty(name, flags);
                object value = property != null ? property.GetValue(instance) : type.GetField(name, flags)?.GetValue(instance);
                row.supported = property != null || type.GetField(name, flags) != null;
                row.type = value == null ? null : value.GetType().FullName;
                row.value = value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            catch (Exception exception) { row.value = "Unavailable: " + exception.GetType().Name; }
            return row;
        }
        private static TexturePlatform TextureSettings(string platform, TextureImporterPlatformSettings value) => new TexturePlatform
        {
            platform = platform, overridden = value.overridden, maxTextureSize = value.maxTextureSize,
            format = value.format.ToString(), textureCompression = value.textureCompression.ToString(),
            compressionQuality = value.compressionQuality, crunchedCompression = value.crunchedCompression
        };
        private static AudioSample SampleSettings(AudioImporterSampleSettings value) => new AudioSample
        {
            loadType = value.loadType.ToString(), compressionFormat = value.compressionFormat.ToString(),
            quality = value.quality, sampleRateSetting = value.sampleRateSetting.ToString(), sampleRateOverride = value.sampleRateOverride,
            preloadAudioData = ReadSetting(typeof(AudioImporterSampleSettings), value, "preloadAudioData").value
        };
        private static void CollectImporter(string path, Report result)
        {
            try
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer is TextureImporter texture)
                {
                    var row = new TextureRow
                    {
                        path = path, readable = texture.isReadable, mipmaps = texture.mipmapEnabled,
                        streamingMipmaps = texture.streamingMipmaps, maxTextureSize = texture.maxTextureSize,
                        textureType = texture.textureType.ToString(), textureCompression = texture.textureCompression.ToString(),
                        defaults = TextureSettings("DefaultTexturePlatform", texture.GetDefaultPlatformTextureSettings()),
                        webGL = TextureSettings("WebGL", texture.GetPlatformTextureSettings("WebGL"))
                    };
                    var method = typeof(TextureImporter).GetMethod("GetSourceTextureWidthAndHeight", BindingFlags.Public | BindingFlags.Instance, null,
                        new[] { typeof(int).MakeByRefType(), typeof(int).MakeByRefType() }, null);
                    if (method != null)
                    {
                        try
                        {
                            object[] values = { 0, 0 }; object returned = method.Invoke(texture, values);
                            row.sourceWidth = (int)values[0]; row.sourceHeight = (int)values[1];
                            row.sourceDimensionsAvailable = row.sourceWidth > 0 && row.sourceHeight > 0 && (!(returned is bool ok) || ok);
                        }
                        catch (Exception exception) { result.warnings.Add("Source dimensions " + path + ": " + exception.GetType().Name); }
                    }
                    result.textures.Add(row);
                }
                else if (importer is AudioImporter audio)
                {
                    bool overridden = audio.ContainsSampleSettingsOverride("WebGL");
                    result.audio.Add(new AudioRow
                    {
                        path = path, loadInBackground = audio.loadInBackground, forceToMono = audio.forceToMono,
                        importerPreloadAudioData = ReadSetting(typeof(AudioImporter), audio, "preloadAudioData").value,
                        hasWebGLOverride = overridden, defaults = SampleSettings(audio.defaultSampleSettings),
                        webGL = overridden ? SampleSettings(audio.GetOverrideSampleSettings("WebGL")) : null
                    });
                }
                else if (importer is ModelImporter model)
                    result.models.Add(new ModelRow { path = path, readable = model.isReadable, importAnimation = model.importAnimation, meshCompression = model.meshCompression.ToString(), animationCompression = model.animationCompression.ToString() });
            }
            catch (Exception exception) { result.warnings.Add("Importer metadata " + path + ": " + exception); }
        }
    }
}
