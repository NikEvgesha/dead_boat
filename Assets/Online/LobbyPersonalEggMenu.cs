using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    // Local meta UI. No egg inventory or incubation data is sent to Photon.
    public sealed class LobbyPersonalEggMenu : MonoBehaviour
    {
        private LobbyOnlineBootstrap owner;
        private EggHatchingManager manager;
        private EggFeatureUnlockVisibility[] visibility;
        private AnimalPlacementPoints[] placements;
        private AnimalLoadoutBoardSpawner[] boards;
        private GameObject root, panel, chip;
        private Text title, summary;
        private Button eggs, animals, loadout, merge;
        private readonly List<NestRow> rows = new();
        private bool hidden, open, ownsCursor;
        private float nextRefresh;
        private PlayerInput input;
        internal bool IsOpen => open;
        private bool English => LocalizationManager.Instance != null &&
            LocalizationManager.Instance.CurrentLanguage?.StartsWith("En", StringComparison.OrdinalIgnoreCase) == true;
        private string L(string ru, string en) => English ? en : ru;

        public void Initialize(LobbyOnlineBootstrap bootstrap)
        {
            owner = bootstrap;
            manager = EggHatchingManager.Instance;
            visibility = FindObjectsByType<EggFeatureUnlockVisibility>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            placements = FindObjectsByType<AnimalPlacementPoints>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            boards = FindObjectsByType<AnimalLoadoutBoardSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            BuildUI();
            Refresh();
        }

        private void Update()
        {
            if (owner == null) return;
            if (input != PlayerInput.Instance)
            {
                if (input != null) input.AOpenWindow -= OtherWindow;
                input = PlayerInput.Instance;
                if (input != null) input.AOpenWindow += OtherWindow;
            }
            if (open && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .25f;
            Refresh();
        }

        private void OtherWindow(MonoBehaviour other)
        {
            if (other != this && open) SetOpen(false);
        }

        private void Refresh()
        {
            if (manager == null) manager = EggHatchingManager.Instance;
            // Failed/idle connections use the existing offline presentation.
            bool online = owner.IsOnline || owner.IsConnecting || owner.IsBrowsingDepartures || owner.IsInDepartureRoom;
            if (hidden != online)
            {
                hidden = online;
                ApplyPresentation();
            }
            bool available = online && !owner.IsBrowsingDepartures && !owner.IsInDepartureRoom && manager != null;
            chip.SetActive(available && !open);
            if (!available && open) SetOpen(false);
            chip.GetComponentInChildren<Text>().text = L("Яйца и животные", "Eggs and animals");
            if (!open || manager == null) return;
            title.text = L("Мои яйца и животные", "My eggs and animals");
            summary.text = L("Личное меню • Яиц: ", "Private menu • Eggs: ") + manager.GetTotalEggCount();
            eggs.GetComponentInChildren<Text>().text = L("Яйца", "Eggs");
            animals.GetComponentInChildren<Text>().text = L("Животные", "Animals");
            loadout.GetComponentInChildren<Text>().text = L("Выбрать бонусы", "Choose bonuses");
            merge.GetComponentInChildren<Text>().text = L("Объединение", "Merge");
            animals.interactable = loadout.interactable = manager.HasHatchedAnimal();
            merge.interactable = manager.HasUnlockedAnimalMerge();
            foreach (var row in rows)
            {
                var nest = manager.GetNestState(row.Id);
                int seconds = nest == null ? 0 : manager.GetRemainingSeconds(row.Id);
                row.Label.text = L("Гнездо ", "Nest ") + row.Number + "\n" + (nest == null
                    ? L("Свободно", "Empty") : seconds <= 0 ? L("Животное готово!", "Animal ready!")
                    : L("Вылупление: ", "Hatching: ") + TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss"));
                row.Button.GetComponentInChildren<Text>().text = nest == null ? L("Положить яйцо", "Incubate")
                    : seconds <= 0 ? L("Забрать", "Collect") : L("Ускорить • реклама", "Speed up • ad");
                row.Button.interactable = nest == null ? manager.GetTotalEggCount() > 0
                    : seconds <= 0 || row.Point.BoostIncubationWithRewardedAd;
            }
        }

        private void ApplyPresentation()
        {
            // Set spawn guards before disabling anchors; keeps periodic restorers from respawning animals.
            if (manager != null) manager.SetWorldHidden(hidden);
            foreach (var item in placements) if (item != null) item.SetWorldHidden(hidden);
            foreach (var item in boards) if (item != null) item.SetWorldHidden(hidden);
            foreach (var item in visibility) if (item != null) item.SetWorldHidden(hidden);
        }

        private void SetOpen(bool value)
        {
            if (value && (owner == null || !chip.activeSelf ||
                GetComponent<LobbyFriendsUI>()?.IsOpen == true || GetComponent<LobbyChatUI>()?.IsOpen == true ||
                GetComponent<LobbyOnlineModeUI>()?.IsOpen == true)) return;
            if (open == value) return;
            open = value;
            panel.SetActive(value);
            if (ControlManager.Instance != null && !ControlManager.Instance.UseTouchControl)
            {
                ControlManager.Instance.CursorActive = value;
                ownsCursor = value;
            }
            if (value)
            {
                owner.StayOnline();
                PlayerInput.Instance?.AOpenWindow?.Invoke(this);
            }
            Refresh();
        }

        private void Handoff(Action action)
        {
            SetOpen(false);
            action();
        }

        private void UseNest(EggNestPoint point)
        {
            UseNest(point.NestId, point);
        }

        private void UseNest(string id, EggNestPoint point)
        {
            if (manager == null) return;
            owner.StayOnline();
            if (manager.GetNestState(id) == null)
                Handoff(() => EggNestSelectionPanel.Instance?.OpenForNest(id));
            else
            {
                if (manager.GetRemainingSeconds(id) <= 0) manager.TryCollectReadyAnimal(id);
                else point.Use(); // Existing rewarded boost, with the same duration and duplicate-ad guard.
                Refresh();
            }
        }

        private void BuildUI()
        {
            root = new GameObject("Personal eggs UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 1003;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            chip = Button(root.transform, "Eggs menu", new Vector2(1, 1), new Vector2(-172, -203), new Vector2(330, 48), () => SetOpen(!open)).gameObject;
            panel = new GameObject("Personal eggs panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(720, 560);
            panel.GetComponent<Image>().color = new Color(.12f, .18f, .22f, .98f);
            title = Label(panel.transform, "Title", new Vector2(0, 235), new Vector2(620, 46), 28);
            summary = Label(panel.transform, "Summary", new Vector2(0, 190), new Vector2(650, 38), 20);
            Button(panel.transform, "×", new Vector2(.5f, .5f), new Vector2(325, 235), new Vector2(42, 42), () => SetOpen(false));
            eggs = Button(panel.transform, "Eggs", new Vector2(.5f, .5f), new Vector2(-165, 140), new Vector2(310, 46),
                () => Handoff(() => EggNestSelectionPanel.Instance?.OpenInventory()));
            animals = Button(panel.transform, "Animals", new Vector2(.5f, .5f), new Vector2(165, 140), new Vector2(310, 46),
                () => Handoff(() => AnimalInventoryPanel.Instance?.OpenInventory()));
            var viewport = new GameObject("Nests scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(panel.transform, false);
            viewport.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 290);
            viewport.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -45);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, .12f);
            var content = new GameObject("Nests", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (manager != null)
                foreach (var point in manager.GetNestPoints())
                {
                    if (point == null || string.IsNullOrWhiteSpace(point.NestId) || !ids.Add(point.NestId)) continue;
                    string id = point.NestId;
                    int number = rows.Count + 1;
                    float y = -38 - rows.Count * 74;
                    var label = Label(content, "Nest " + number, new Vector2(-110, y), new Vector2(390, 68), 19);
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, 1);
                    var button = Button(content, "Nest action", new Vector2(.5f, 1), new Vector2(205, y), new Vector2(220, 52), () => UseNest(point));
                    rows.Add(new NestRow { Id = id, Point = point, Number = number, Label = label, Button = button });
                }
            content.sizeDelta = new Vector2(0, Mathf.Max(290, rows.Count * 74));
            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content; scroll.horizontal = false;
            loadout = Button(panel.transform, "Loadout", new Vector2(.5f, .5f), new Vector2(-165, -230), new Vector2(310, 48),
                () => Handoff(() => AnimalLoadoutPanel.Instance?.Open()));
            merge = Button(panel.transform, "Merge", new Vector2(.5f, .5f), new Vector2(165, -230), new Vector2(310, 48),
                () => Handoff(() => AnimalMergeSelectionPanel.Instance?.Open()));
            panel.SetActive(false);
        }

        private static Text Label(Transform parent, string name, Vector2 pos, Vector2 size, int font) =>
            LobbyOnlineModeUI.CreateText(parent, name, pos, size, font);
        private static Button Button(Transform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var button = LobbyOnlineModeUI.CreateButton(parent, text, anchor, pos, size, action);
            button.GetComponentInChildren<Text>().text = text;
            return button;
        }

        private void OnDestroy()
        {
            if (input != null) input.AOpenWindow -= OtherWindow;
            if (ownsCursor && ControlManager.Instance != null) ControlManager.Instance.CursorActive = false;
            if (root != null) Destroy(root);
            // Scene-owned presentation flags disappear with this lobby. No save data was changed.
        }

        private sealed class NestRow
        {
            public string Id;
            public EggNestPoint Point;
            public int Number;
            public Text Label;
            public Button Button;
        }
    }
}
