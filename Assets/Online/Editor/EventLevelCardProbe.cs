using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static DeadBoat.Online.Editor.SharedRecoveryProbe;

namespace DeadBoat.Online.Editor
{
    public static class EventLevelCardProbe
    {
        public static async Task CheckScreens(string previewDirectory = null)
        {
            await Until(() => LoadingManager.Instance?.CurrentLocation == Location.Lobby, "isolated game lobby");
            Require(SaveManager.Instance.PersistentProvider is SharedRunPreviewSave, "isolated player profile");
            var localization = LocalizationManager.Instance;
            string originalLanguage = localization.CurrentLanguage;
            var opens = new DateTimeOffset(2030, 1, 2, 3, 4, 0, TimeSpan.Zero);
            var now = opens.AddTicks(-1);
            var winter = RunEventCalendar.FirstWinterTheme;
            var entry = new ScheduledRunEvent(3, winter, "LevelEvent", opens, opens.AddHours(3), crewReady: false);
            var standard = RunEventCalendar.ReplaceForEditorTest(new RunEventCalendar(() => now, entry));
            var point = UnityEngine.Object.FindFirstObjectByType<GameStartPoint>();
            var lobby = UnityEngine.Object.FindFirstObjectByType<LobbyOnlineBootstrap>();
            var levels = LevelManager.Instance.Levels;
            try
            {
                point.SendMessage("ShowCanvas");
                var picker = UnityEngine.Object.FindFirstObjectByType<LevelMapUI>();
                await Until(() => picker.GetComponentInChildren<EventLevelCard>() != null, "upcoming card");
                var card = picker.GetComponentInChildren<EventLevelCard>();
                var button = card.GetComponentInChildren<Button>();
                var art = card.GetComponentsInChildren<Image>().Single(i => i.name == "Winter route");
                Require(art.sprite == levels[3].IMG && art.sprite != levels[0].IMG && art.sprite != levels[2].IMG &&
                    art.sprite.texture.width <= 512 && art.sprite.texture.height <= 512, "unique compact winter sprite");
                Require(LevelManager.Instance.GetLevelId(levels[3]) == 3 && card.transform.GetSiblingIndex() == 0,
                    "featured first, still level 3");
                localization.ChangeLanguage("Ru");
                await Until(() => Has(card, "Новогодний рейс") && Has(card, "Начало 02.01.2030 03:04 UTC"), "Russian upcoming");
                Require(!button.interactable && Has(card, "летучие сани"), "upcoming description and disabled button");
                SavePreview(previewDirectory, "solo-ru", card.GetComponentInParent<Canvas>().rootCanvas);
                Click(button, -1);
                localization.ChangeLanguage("En");
                await Until(() => Has(card, "New Year Voyage") && Has(card, "Opens 02 Jan 2030 03:04 UTC"), "English upcoming");
                now = opens;
                await Until(() => button.interactable && Has(card, "Until 02 Jan 2030 06:04 UTC"), "live open boundary");
                await CheckNarrowLayout(picker, button);
                var later = new ScheduledRunEvent(3, new RunEventContent { EventId = winter.EventId, ThemeRevision = 2 },
                    "FixtureNextTheme", opens, opens.AddHours(3), crewReady: true);
                var current = RunEventCalendar.ReplaceForEditorTest(new RunEventCalendar(() => now, later));
                Click(button, 0); // The screen still shows the first revision.
                var transition = typeof(LevelMapUI).GetField("_transitionStarted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Require(!(bool)transition.GetValue(picker) && LoadingManager.Instance.CurrentLocation == Location.Lobby, "tap cannot switch theme");
                RunEventCalendar.ReplaceForEditorTest(current);
                point.Cancel();

                lobby.SelectOnline();
                await Until(() => lobby.IsOnline && !lobby.IsConnecting, "visual lobby connected");
                var playerCollider = PlayerManager.Instance.GetComponentsInChildren<Collider>().First(c => c.CompareTag("Player"));
                point.SendMessage("OnTriggerEnter", playerCollider);
                var departure = point.GetComponent<LobbyDepartureUI>();
                await Until(() => lobby.IsBrowsingDepartures && !lobby.IsConnecting, "real departure directory");
                var canvas = GameObject.Find("Departure UI");
                Button Named(string name) => canvas.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
                await Until(() => Named("Primary").interactable, "crew creation available");
                Click(Named("Primary"), 0);
                await Until(() => canvas.GetComponentInChildren<EventLevelCard>() != null, "crew level picker");
                var crewCard = canvas.GetComponentInChildren<EventLevelCard>();
                var crewButton = crewCard.GetComponentInChildren<Button>();
                int layoutFrame = Time.frameCount;
                await Until(() => Time.frameCount > layoutFrame + 1, "new crew rows rendered");
                Require(!crewButton.interactable && Has(crewCard, "Solo for now") &&
                    !await lobby.CreateDepartureAsync(3, 2, winter), "unreleased crew rejected by UI and public creation API");
                Click(crewButton, -1);
                localization.ChangeLanguage("Ru");
                await Until(() => (crewCard = canvas.GetComponentInChildren<EventLevelCard>()) != null &&
                    Has(crewCard, "Пока только соло"), "crew language switch");
                crewButton = crewCard.GetComponentInChildren<Button>();
                SavePreview(previewDirectory, "crew-ru", canvas.GetComponent<Canvas>());
                var enabled = new ScheduledRunEvent(3, winter, "LevelEvent", opens, opens.AddHours(3), crewReady: true);
                RunEventCalendar.ReplaceForEditorTest(new RunEventCalendar(() => now, enabled));
                await Until(() => crewButton.interactable, "eligible crew card");
                Click(crewButton, -1);
                await Until(() => Named("Primary").gameObject.activeSelf && Named("Primary").interactable &&
                    canvas.GetComponentsInChildren<Text>().Any(t => t.text == "Создать экипаж"), "mouse opens crew configuration");
                now = opens.AddHours(3);
                await Until(() => !Named("Primary").interactable && Has(canvas, "Событие сейчас закрыто."), "expiry on configure screen");
                Click(Named("Back"), 0);
                await Until(() => !canvas.GetComponentInChildren<EventLevelCard>() && Named("Secondary").gameObject.activeSelf,
                    "closed event hidden in crew list");
                Click(Named("Secondary"), 0);
                await Until(() => picker.gameObject.activeInHierarchy && !lobby.IsBrowsingDepartures && !lobby.IsConnecting, "touch opens real solo picker");
                lobby.SelectOffline();
                await Until(() => !lobby.IsOnline, "offline again");
                Debug.Log("[Event card] PASS RU/EN, upcoming/open/ended, narrow layout, pointer hits, stale revision, real crew selector, solo-only gate and return to solo.");
            }
            finally
            {
                RunEventCalendar.ReplaceForEditorTest(standard);
                localization.ChangeLanguage(originalLanguage);
            }
        }

        private static async Task CheckNarrowLayout(LevelMapUI picker, Button button)
        {
            var panel = (RectTransform)picker.transform.parent;
            var original = (panel.anchorMin, panel.anchorMax, panel.offsetMin, panel.offsetMax);
            try
            {
                var available = ((RectTransform)panel.parent).rect.size;
                float height = Mathf.Min(available.y, available.x * 1.6f);
                panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
                panel.anchoredPosition = Vector2.zero;
                panel.sizeDelta = new Vector2(height * 9 / 16, height);
                await Until(() => picker.GetComponentInChildren<GridLayoutGroup>() != null, "portrait grid");
                Canvas.ForceUpdateCanvases();
                var grid = picker.GetComponentInChildren<GridLayoutGroup>();
                Require(grid.transform.childCount == 4 && grid.constraintCount == 2, "two-column picker");
                var surface = (RectTransform)button.transform;
                Require(surface.rect.width > 0 && surface.rect.height > 0 && Hit(button, 0).Count > 0, "touch target in narrow layout");
                foreach (var label in button.GetComponentsInChildren<Text>())
                {
                    var generator = new TextGenerator();
                    generator.Populate(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
                    Require(generator.characterCountVisible >= label.text.Length - 1, "all text fits: " + label.name);
                }
            }
            finally
            {
                panel.anchorMin = original.anchorMin; panel.anchorMax = original.anchorMax;
                panel.offsetMin = original.offsetMin; panel.offsetMax = original.offsetMax;
                picker.SendMessage("RefreshEventRows");
                Canvas.ForceUpdateCanvases();
            }
        }

        private static bool Has(Component root, string value) => root.GetComponentsInChildren<Text>().Any(t => t.text.Contains(value));
        private static bool Has(GameObject root, string value) => root.GetComponentsInChildren<Text>().Any(t => t.text.Contains(value));

        private static void SavePreview(string directory, string name, Canvas canvas)
        {
            if (directory == null) return;
            // Batch-mode has no Game View backbuffer. Render the actual picker against a neutral background.
            System.IO.Directory.CreateDirectory(directory);
            var camera = new GameObject("Map picker preview").AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(0, -200, 0);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 5;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(28, 42, 54, 255);
            var target = RenderTexture.GetTemporary(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = (canvas.renderMode, canvas.worldCamera, canvas.planeDistance, RenderTexture.active);
            try
            {
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                var picker = canvas.GetComponentInChildren<LevelMapUI>();
                if (picker != null)
                {
                    picker.SendMessage("RefreshEventRows");
                    Canvas.ForceUpdateCanvases();
                    var parent = (RectTransform)picker.transform.parent;
                    var close = (RectTransform)parent.Find("Cancel");
                    var cards = (RectTransform)picker.transform;
                    float right = parent.InverseTransformPoint(cards.TransformPoint(cards.rect.max)).x;
                    float left = parent.InverseTransformPoint(close.TransformPoint(close.rect.min)).x;
                    Require(right <= left + 1, "featured picker leaves room for Close");
                    foreach (var map in picker.GetComponentsInChildren<LevelMapElement>())
                    {
                        var cell = (RectTransform)map.transform;
                        var shape = map.GetComponentsInChildren<AspectRatioFitter>().Single(f => f.transform.parent == cell);
                        var face = (RectTransform)shape.transform;
                        Require(face.rect.width <= cell.rect.width + 1 && face.rect.height <= cell.rect.height + 1,
                            "ordinary map fits its cell alongside the event");
                    }
                }
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".jpg"), image.EncodeToJPG(87));
            }
            finally
            {
                canvas.worldCamera = previous.worldCamera;
                canvas.planeDistance = previous.planeDistance;
                canvas.renderMode = previous.renderMode;
                RenderTexture.active = previous.active;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
                Canvas.ForceUpdateCanvases();
                canvas.GetComponentInChildren<LevelMapUI>()?.SendMessage("RefreshEventRows");
            }
        }

        private static List<RaycastResult> Hit(Button button, int pointerId)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var data = new PointerEventData(EventSystem.current) { pointerId = pointerId,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
                button.name + " unreachable at " + data.position + ", size=" + rect.rect.size + ", depth=" +
                button.targetGraphic.depth + ", culled=" + button.targetGraphic.canvasRenderer.cull +
                ", first hit: " + hits.FirstOrDefault().gameObject);
            return hits;
        }

        internal static void Click(Button button, int pointerId)
        {
            var hit = Hit(button, pointerId)[0];
            var data = new PointerEventData(EventSystem.current) { pointerId = pointerId, button = PointerEventData.InputButton.Left,
                position = hit.screenPosition, pointerPressRaycast = hit, pointerCurrentRaycast = hit };
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        private static void Require(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
