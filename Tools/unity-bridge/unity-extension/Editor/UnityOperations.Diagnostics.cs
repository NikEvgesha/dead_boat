using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityBridge
{
    public static partial class UnityOperations
    {
        public static OperationResult GetProjectStatus(UnityRequest request)
        {
            try
            {
                var includePackages = request.GetValue("include_packages", false);
                var includeSelection = request.GetValue("include_selection", true);
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

                var data = new Dictionary<string, object>
                {
                    { "unityVersion", Application.unityVersion },
                    { "projectPath", Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath },
                    { "dataPath", Application.dataPath },
                    { "buildTarget", EditorUserBuildSettings.activeBuildTarget.ToString() },
                    { "buildTargetGroup", EditorUserBuildSettings.selectedBuildTargetGroup.ToString() },
                    { "isPlaying", EditorApplication.isPlaying },
                    { "isPaused", EditorApplication.isPaused },
                    { "isCompiling", EditorApplication.isCompiling },
                    { "isUpdating", EditorApplication.isUpdating },
                    { "scriptCompilationFailed", EditorUtility.scriptCompilationFailed },
                    { "activeScene", new Dictionary<string, object>
                        {
                            { "name", scene.name },
                            { "path", scene.path },
                            { "isDirty", scene.isDirty },
                            { "isLoaded", scene.isLoaded },
                            { "rootCount", scene.IsValid() ? scene.rootCount : 0 }
                        }
                    }
                };

                if (includeSelection)
                {
                    data["selection"] = Selection.objects
                        .Where(o => o != null)
                        .Take(50)
                        .Select(o => new Dictionary<string, object>
                        {
                            { "name", o.name },
                            { "type", o.GetType().Name },
                            { "instanceId", o.GetInstanceID() },
                            { "assetPath", AssetDatabase.GetAssetPath(o) }
                        })
                        .Cast<object>()
                        .ToList();
                }

                if (includePackages)
                {
                    data["packages"] = UnityEditor.PackageManager.PackageInfo
                        .GetAllRegisteredPackages()
                        .OrderBy(p => p.name)
                        .Select(p => new Dictionary<string, object>
                        {
                            { "name", p.name },
                            { "version", p.version },
                            { "source", p.source.ToString() },
                            { "resolvedPath", p.resolvedPath }
                        })
                        .Cast<object>()
                        .ToList();
                }

                return OperationResult.Ok("Project status collected", data);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Project status failed: {ex.Message}");
            }
        }

        public static OperationResult GetConsoleEntries(UnityRequest request)
        {
            try
            {
                var maxEntries = Math.Max(1, Math.Min(500, request.GetValue("max_entries", 100)));
                var includeStack = request.GetValue("include_stack", false);
                var filter = request.GetValue<string>("filter", null);
                var typeFilter = ParseStringList(request.Data, "types")
                    .Select(t => t.ToLowerInvariant())
                    .ToHashSet();

                var entries = ReadUnityConsoleEntries(maxEntries, includeStack, filter, typeFilter);
                var data = new Dictionary<string, object>
                {
                    { "count", entries.Count },
                    { "entries", entries.Cast<object>().ToList() },
                    { "isCompiling", EditorApplication.isCompiling },
                    { "scriptCompilationFailed", EditorUtility.scriptCompilationFailed }
                };

                return OperationResult.Ok($"Console entries collected: {entries.Count}", data);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Console read failed: {ex.Message}");
            }
        }

        public static OperationResult FindAssets(UnityRequest request)
        {
            try
            {
                var query = request.GetValue<string>("query", "") ?? "";
                var type = request.GetValue<string>("type", "") ?? "";
                var under = request.GetValue<string>("under", "Assets") ?? "Assets";
                var maxResults = Math.Max(1, Math.Min(1000, request.GetValue("max_results", 100)));

                var filterParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(query))
                    filterParts.Add(query.Trim());
                if (!string.IsNullOrWhiteSpace(type))
                    filterParts.Add("t:" + type.Trim());

                var filter = string.Join(" ", filterParts);
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? "";
                var folders = Directory.Exists(Path.Combine(projectRoot, under)) ? new[] { under } : null;

                var results = AssetDatabase.FindAssets(filter, folders)
                    .Take(maxResults)
                    .Select(guid =>
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                        var asset = AssetDatabase.LoadMainAssetAtPath(path);
                        return new Dictionary<string, object>
                        {
                            { "guid", guid },
                            { "path", path },
                            { "name", Path.GetFileNameWithoutExtension(path) },
                            { "type", assetType != null ? assetType.Name : "" },
                            { "labels", asset != null ? AssetDatabase.GetLabels(asset).Cast<object>().ToList() : new List<object>() }
                        };
                    })
                    .Cast<object>()
                    .ToList();

                var data = new Dictionary<string, object>
                {
                    { "filter", filter },
                    { "under", under },
                    { "count", results.Count },
                    { "results", results }
                };

                return OperationResult.Ok($"Assets found: {results.Count}", data);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Asset search failed: {ex.Message}");
            }
        }

        public static OperationResult ReadAsset(UnityRequest request)
        {
            try
            {
                var path = request.GetValue<string>("path", null);
                var guid = request.GetValue<string>("guid", null);
                var includeDependencies = request.GetValue("include_dependencies", true);
                var includeText = request.GetValue("include_text", false);
                var maxTextChars = Math.Max(256, Math.Min(20000, request.GetValue("max_text_chars", 4000)));

                if (string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(guid))
                    path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrWhiteSpace(path))
                    return OperationResult.Fail("Use 'path' or 'guid'.");

                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null)
                    return OperationResult.Fail($"Asset not found: {path}");

                var importer = AssetImporter.GetAtPath(path);
                var data = new Dictionary<string, object>
                {
                    { "path", path },
                    { "guid", AssetDatabase.AssetPathToGUID(path) },
                    { "name", asset.name },
                    { "type", asset.GetType().FullName },
                    { "labels", AssetDatabase.GetLabels(asset).Cast<object>().ToList() },
                    { "importerType", importer != null ? importer.GetType().FullName : "" }
                };

                if (includeDependencies)
                {
                    data["dependencies"] = AssetDatabase.GetDependencies(path, true)
                        .Take(200)
                        .Cast<object>()
                        .ToList();
                }

                if (includeText && IsTextLikeAsset(path))
                {
                    var absolutePath = Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? "", path);
                    if (File.Exists(absolutePath))
                    {
                        var text = File.ReadAllText(absolutePath);
                        data["text"] = text.Length > maxTextChars
                            ? text.Substring(0, maxTextChars) + "\n[truncated]"
                            : text;
                    }
                }

                return OperationResult.Ok($"Asset read: {path}", data);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Asset read failed: {ex.Message}");
            }
        }

        private static List<Dictionary<string, object>> ReadUnityConsoleEntries(
            int maxEntries,
            bool includeStack,
            string filter,
            HashSet<string> typeFilter)
        {
            var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
            var logEntryType = Type.GetType("UnityEditor.LogEntry,UnityEditor");
            if (logEntriesType == null || logEntryType == null)
                return new List<Dictionary<string, object>>();

            var getCount = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var startGetting = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var endGetting = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var getEntry = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (getCount == null || getEntry == null)
                return new List<Dictionary<string, object>>();

            var total = Convert.ToInt32(getCount.Invoke(null, null));
            var first = Math.Max(0, total - maxEntries);
            var results = new List<Dictionary<string, object>>();

            startGetting?.Invoke(null, null);
            try
            {
                for (var i = first; i < total; i++)
                {
                    var entry = Activator.CreateInstance(logEntryType);
                    getEntry.Invoke(null, new[] { (object)i, entry });

                    var condition = Convert.ToString(GetFieldOrProperty(logEntryType, entry, "condition")) ?? "";
                    var stackTrace = Convert.ToString(GetFieldOrProperty(logEntryType, entry, "stackTrace")) ?? "";
                    var mode = Convert.ToInt32(GetFieldOrProperty(logEntryType, entry, "mode") ?? 0);
                    var kind = ClassifyConsoleMode(mode);

                    if (!string.IsNullOrWhiteSpace(filter) &&
                        condition.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        stackTrace.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (typeFilter.Count > 0 && !typeFilter.Contains(kind.ToLowerInvariant()))
                        continue;

                    var item = new Dictionary<string, object>
                    {
                        { "index", i },
                        { "type", kind },
                        { "message", condition },
                        { "file", Convert.ToString(GetFieldOrProperty(logEntryType, entry, "file")) ?? "" },
                        { "line", Convert.ToInt32(GetFieldOrProperty(logEntryType, entry, "line") ?? 0) },
                        { "instanceId", Convert.ToInt32(GetFieldOrProperty(logEntryType, entry, "instanceID") ?? 0) }
                    };

                    if (includeStack)
                        item["stackTrace"] = stackTrace;

                    results.Add(item);
                }
            }
            finally
            {
                endGetting?.Invoke(null, null);
            }

            return results;
        }

        private static object GetFieldOrProperty(Type type, object instance, string name)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var field = type.GetField(name, flags);
            if (field != null)
                return field.GetValue(instance);

            var property = type.GetProperty(name, flags);
            return property != null ? property.GetValue(instance, null) : null;
        }

        private static string ClassifyConsoleMode(int mode)
        {
            const int errorBits = 1 | 2 | 16 | 64 | 256 | 2048 | 8192;
            const int warningBits = 128 | 512 | 4096;

            if ((mode & errorBits) != 0)
                return "Error";
            if ((mode & warningBits) != 0)
                return "Warning";
            return "Log";
        }

        private static List<string> ParseStringList(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out var value) || value == null)
                return new List<string>();

            if (value is string s)
                return s.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

            if (value is System.Collections.IEnumerable enumerable)
                return enumerable.Cast<object>().Select(x => x?.ToString() ?? "").Where(x => x.Length > 0).ToList();

            return new List<string>();
        }

        private static bool IsTextLikeAsset(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            return ext == ".cs" ||
                   ext == ".json" ||
                   ext == ".txt" ||
                   ext == ".md" ||
                   ext == ".asmdef" ||
                   ext == ".shader" ||
                   ext == ".uss" ||
                   ext == ".uxml" ||
                   ext == ".asset" ||
                   ext == ".prefab" ||
                   ext == ".mat";
        }
    }
}
