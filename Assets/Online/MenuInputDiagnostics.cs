#if UNITY_EDITOR || DEADBOAT_COOP_PERFORMANCE
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadBoat.Online
{
    // Temporary draft diagnostics: a screenshot is enough when Console is unavailable.
    public sealed class MenuInputDiagnostics : MonoBehaviour
    {
        private readonly List<RaycastResult> hits = new List<RaycastResult>(16);
        private EventSystem events;
        private PointerEventData pointer;
        private string lastKey = "none", lastClick = "none", lastError = "none", state = "boot";
        private float nextSample;
        private GUIStyle style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var root = new GameObject("Draft menu input diagnostics");
            DontDestroyOnLoad(root);
            root.AddComponent<MenuInputDiagnostics>();
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;
        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception) return;
            if (!stack.Contains("SettingUI") && !stack.Contains("PlayerInput") &&
                !stack.Contains("InventoryUI") && !stack.Contains("ControlManager")) return;
            int end = message.IndexOf('\n');
            lastError = end < 0 ? message : message.Substring(0, end);
            if (lastError.Length > 180) lastError = lastError.Substring(0, 180);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) lastKey = "P @ " + Time.unscaledTime.ToString("F1");
            if (Input.GetKeyDown(KeyCode.Tab)) lastKey = "Tab @ " + Time.unscaledTime.ToString("F1");
            if (Input.GetMouseButtonDown(0))
            {
                var current = EventSystem.current;
                lastClick = "no EventSystem";
                if (current != null)
                {
                    if (events != current || pointer == null)
                    {
                        events = current;
                        pointer = new PointerEventData(current);
                    }
                    pointer.Reset();
                    pointer.position = Input.mousePosition;
                    current.RaycastAll(pointer, hits);
                    lastClick = hits.Count == 0 ? "world" : hits[0].gameObject.name + " / " + hits[0].module.GetType().Name;
                }
            }
            if (Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + .5f;
            var input = PlayerInput.Instance;
            var control = ControlManager.Instance;
            var menu = FindAnyObjectByType<SettingUI>();
            state = "scale=" + Time.timeScale + "; focus=" + Application.isFocused +
                "; touch=" + (control != null && control.UseTouchControl) +
                "\ninput=" + (input != null && input.isActiveAndEnabled) +
                "; P subs=" + (input?.APause?.GetInvocationList().Length ?? 0) +
                "; Tab subs=" + (input?.AInventory?.GetInvocationList().Length ?? 0) +
                "\nmenu=" + (menu != null && menu.IsOpen) +
                "; inventory=" + (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) +
                "; cursor=" + Cursor.lockState + "/" + (control != null && control.CursorActive);
        }

        private void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            float width = Mathf.Min(610, Screen.width - 16);
            Rect rect = new Rect(Mathf.Max(8, Screen.width - width - 8), 90, width, 190);
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12),
                "UI probe e7cf / menu-fix draft\n" + state + "\nkey=" + lastKey +
                "\nclick=" + lastClick + "\nUI exception=" + lastError, style);
        }
    }
}
#endif
