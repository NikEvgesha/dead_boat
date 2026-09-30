using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    // Boarding screen shown before the existing solo map picker.
    public sealed class LobbyDepartureUI : MonoBehaviour
    {
        private enum Page { Browser, Levels, Configure, Waiting }

        private GameStartPoint startPoint;
        private LobbyOnlineBootstrap online;
        private GameObject root;
        private RectTransform rows;
        private Text title;
        private Text status;
        private Button primary;
        private Button secondary;
        private Button back;
        private Page page;
        private LevelData selectedLevel;
        private int selectedLevelId;
        private int seats = 4;
        private string rowSignature;
        private string localMessage;
        private float nextRefresh;
        private bool busy;

        public void Open(GameStartPoint point, LobbyOnlineBootstrap owner)
        {
            startPoint = point;
            online = owner;
            if (root == null)
                BuildUI();
            root.SetActive(true);
            page = owner.IsInDepartureRoom ? Page.Waiting : Page.Browser;
            rowSignature = null;
            localMessage = null;
            Refresh();
            if (!owner.IsBrowsingDepartures && !owner.IsInDepartureRoom)
                _ = OpenBrowserAsync();
        }

        public void Close()
        {
            if (root != null)
                root.SetActive(false);
        }

        private async System.Threading.Tasks.Task OpenBrowserAsync()
        {
            if (busy || online == null)
                return;
            busy = true;
            localMessage = Text("Ищем экипажи…", "Finding crews…");
            Refresh();
            bool success = await online.OpenDepartureBrowserAsync();
            if (this == null)
                return;
            busy = false;
            localMessage = success ? null : Text("Не удалось получить список. Можно повторить поиск.",
                "Could not load crews. You can retry.");
            rowSignature = null;
            Refresh();
        }

        private async System.Threading.Tasks.Task JoinAsync(DepartureListing listing)
        {
            if (busy || online == null)
                return;
            busy = true;
            localMessage = Text("Присоединяемся…", "Joining…");
            Refresh();
            bool success = await online.JoinDepartureAsync(listing);
            if (this == null)
                return;
            busy = false;
            page = success ? Page.Waiting : Page.Browser;
            localMessage = success ? null : Text("Экипаж уже недоступен. Вернитесь в лобби и попробуйте снова.",
                "This crew is no longer available. Return to the lobby and try again.");
            rowSignature = null;
            Refresh();
        }

        private async System.Threading.Tasks.Task CreateAsync()
        {
            if (busy || online == null || selectedLevel == null)
                return;
            busy = true;
            localMessage = Text("Создаём экипаж…", "Creating crew…");
            Refresh();
            bool success = await online.CreateDepartureAsync(selectedLevelId, seats);
            if (this == null)
                return;
            busy = false;
            page = success ? Page.Waiting : Page.Configure;
            localMessage = success ? null : Text("Не удалось создать экипаж. Попробуйте снова.",
                "Could not create the crew. Please retry.");
            rowSignature = null;
            Refresh();
        }

        private void Update()
        {
            if (root == null || !root.activeSelf || online == null || Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + 0.25f;
            if (page == Page.Waiting && !online.IsInDepartureRoom && !online.IsConnecting)
            {
                page = Page.Browser;
                localMessage = Text("Соединение с экипажем потеряно. Вернитесь в лобби.",
                    "Crew connection lost. Return to the lobby.");
                rowSignature = null;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (root == null || online == null)
                return;

            title.text = page switch
            {
                Page.Browser => Text("Отправления", "Departures"),
                Page.Levels => Text("Выберите уровень", "Choose a level"),
                Page.Configure => Text("Создать экипаж", "Create a crew"),
                _ => Text("Ожидание экипажа", "Waiting for crew")
            };
            status.text = localMessage ?? (page switch
            {
                Page.Browser => online.IsBrowsingDepartures
                    ? Text("Доступные публичные экипажи", "Available public crews")
                    : Text("Подключаем каталог отправлений…", "Connecting to departure directory…"),
                Page.Levels => Text("Для одиночной игры есть отдельная кнопка.",
                    "Use the separate button for solo play."),
                Page.Configure => Text("Публичный экипаж · до ", "Public crew · up to ") + seats,
                _ => Text("Игроков: ", "Players: ") + online.DeparturePlayerCount +
                    "/" + online.DepartureTargetPlayers
            });

            primary.gameObject.SetActive(page == Page.Browser || page == Page.Configure);
            secondary.gameObject.SetActive(page == Page.Browser || page == Page.Configure);
            primary.interactable = !busy && (page != Page.Browser || online.IsBrowsingDepartures);
            secondary.interactable = !busy;
            back.interactable = !busy && !online.IsConnecting;

            SetButton(primary, page == Page.Browser ? Text("Создать свой", "Create a crew") :
                Text("Создать", "Create"), page == Page.Browser ? (Action)ShowLevels : () => _ = CreateAsync());
            SetButton(secondary, page == Page.Browser ? Text("Играть одному", "Play solo") :
                Text("Мест: ", "Seats: ") + seats + "  ↻",
                page == Page.Browser ? (Action)(() => startPoint.ShowSoloMapFromDeparture()) : CycleSeats);
            SetButton(back, page switch
            {
                Page.Browser => Text("Назад", "Back"),
                Page.Levels => Text("К экипажам", "Crews"),
                Page.Configure => Text("К уровням", "Levels"),
                _ => Text("Покинуть экипаж", "Leave crew")
            }, GoBack);

            if (page == Page.Browser)
            {
                var listings = online.GetAvailableDepartures();
                string signature = "browser:" + string.Join("|", listings.Select(x =>
                    x.SessionName + ":" + x.PlayerCount));
                if (signature != rowSignature)
                {
                    rowSignature = signature;
                    DrawBrowserRows(listings);
                }
            }
            else if (rowSignature != page.ToString())
            {
                rowSignature = page.ToString();
                DrawCurrentPage();
            }
        }

        private void DrawBrowserRows(List<DepartureListing> listings)
        {
            ClearRows();
            if (!online.IsBrowsingDepartures)
            {
                AddMessage(Text("Каталог недоступен", "Directory unavailable"));
                return;
            }
            if (listings.Count == 0)
            {
                AddMessage(Text("Ожидающих экипажей пока нет. Создайте свой.",
                    "No crews are waiting yet. Create your own."));
                return;
            }

            foreach (var listing in listings)
            {
                LevelData level = null;
                LevelManager.Instance?.TryGetLevel(listing.LevelId, out level);
                string levelName = level != null ? level.Title : Text("Уровень ", "Level ") + listing.LevelId;
                var button = AddRow(levelName + "   " + listing.PlayerCount + "/" + listing.TargetPlayers +
                    Text("  · Присоединиться", "  · Join"),
                    () => _ = JoinAsync(listing));
                button.interactable = !busy && level != null && level.Unlocked;
            }
        }

        private void DrawCurrentPage()
        {
            ClearRows();
            if (page == Page.Levels)
            {
                var levels = LevelManager.Instance?.Levels;
                if (levels == null)
                {
                    AddMessage(Text("Уровни не найдены", "No levels found"));
                    return;
                }
                for (int i = 0; i < levels.Count; i++)
                {
                    LevelData level = levels[i];
                    int id = i;
                    var button = AddRow(level.Title, () => SelectLevel(id, level));
                    button.interactable = level.Unlocked;
                }
            }
            else if (page == Page.Configure)
            {
                AddMessage(selectedLevel != null ? selectedLevel.Title : string.Empty);
                AddMessage(Text("В каталоге видны только публичные экипажи. Доступ только для друзей добавим после проверки аккаунтов.",
                    "Only public crews are listed. Friends-only access needs verified player accounts."));
            }
            else if (page == Page.Waiting)
            {
                AddMessage(Text("Экипаж создан. Другие игроки могут присоединиться из каталога.",
                    "Crew ready. Other players can join from the directory."));
                AddMessage(Text("Запуск совместного забега — следующий этап разработки.",
                    "Starting the co-op run is the next development step."));
            }
        }

        private void ShowLevels()
        {
            page = Page.Levels;
            localMessage = null;
            rowSignature = null;
            Refresh();
        }

        private void SelectLevel(int id, LevelData level)
        {
            if (level == null || !level.Unlocked)
                return;
            selectedLevelId = id;
            selectedLevel = level;
            page = Page.Configure;
            rowSignature = null;
            Refresh();
        }

        private void CycleSeats()
        {
            seats = seats >= 4 ? 2 : seats + 1;
            Refresh();
        }

        private void GoBack()
        {
            if (busy || online.IsConnecting)
                return;
            if (page == Page.Configure)
                page = Page.Levels;
            else if (page == Page.Levels)
                page = Page.Browser;
            else
            {
                startPoint.Cancel();
                return;
            }
            localMessage = null;
            rowSignature = null;
            Refresh();
        }

        private void BuildUI()
        {
            root = new GameObject("Departure UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1002;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            var shade = MakeRect(root.transform, "Shade", Vector2.zero, Vector2.zero,
                new Vector2(1280, 720), new Color(0f, 0f, 0f, 0.55f));
            shade.anchorMin = Vector2.zero;
            shade.anchorMax = Vector2.one;
            shade.offsetMin = shade.offsetMax = Vector2.zero;
            var panel = MakeRect(shade, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(800, 600), Color.white);
            panel.GetComponent<Image>().sprite = Resources.Load<Sprite>("OnlineUI/OnlineFrame");
            panel.GetComponent<Image>().type = Image.Type.Sliced;
            title = MakeText(panel, "Title", new Vector2(0, 250), new Vector2(750, 52), 32);
            status = MakeText(panel, "Status", new Vector2(0, 195), new Vector2(750, 55), 21);

            var viewport = MakeRect(panel, "List viewport", new Vector2(0.5f, 0.5f),
                new Vector2(0, 15), new Vector2(750, 300), new Color(0.22f, 0.25f, 0.29f, 1f));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            rows.SetParent(viewport, false);
            rows.anchorMin = new Vector2(0, 1);
            rows.anchorMax = new Vector2(1, 1);
            rows.pivot = new Vector2(0.5f, 1);
            rows.anchoredPosition = Vector2.zero;
            rows.sizeDelta = Vector2.zero;
            var layout = rows.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            rows.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rows;

            primary = MakeButton(panel, "Primary", new Vector2(-245, -245), new Vector2(230, 55));
            secondary = MakeButton(panel, "Secondary", new Vector2(0, -245), new Vector2(230, 55));
            back = MakeButton(panel, "Back", new Vector2(245, -245), new Vector2(230, 55));
        }

        private Button AddRow(string label, Action action)
        {
            var button = MakeButton(rows, "Row", Vector2.zero, new Vector2(710, 52));
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            SetButton(button, label, action);
            return button;
        }

        private void AddMessage(string message)
        {
            var label = MakeText(rows, "Message", Vector2.zero, new Vector2(710, 70), 21);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
            label.text = message;
        }

        private void ClearRows()
        {
            for (int i = rows.childCount - 1; i >= 0; i--)
            {
                var child = rows.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private static void SetButton(Button button, string label, Action action)
        {
            button.GetComponentInChildren<Text>().text = label;
            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }

        private static RectTransform MakeRect(Transform parent, string name, Vector2 anchor,
            Vector2 position, Vector2 size, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            item.GetComponent<Image>().color = color;
            return rect;
        }

        private static Button MakeButton(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = MakeRect(parent, name, new Vector2(0.5f, 0.5f), position, size,
                Color.white);
            rect.GetComponent<Image>().sprite = Resources.Load<Sprite>("OnlineUI/OnlineButton");
            rect.GetComponent<Image>().type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            MakeText(rect, "Label", Vector2.zero, size - new Vector2(12, 6), 20);
            return button;
        }

        private static Text MakeText(Transform parent, string name, Vector2 position, Vector2 size, int sizePt)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Text));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = item.GetComponent<Text>();
            text.font = Resources.Load<Font>("Fonts/RussoOne-Regular");
            text.fontSize = sizePt;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static string Text(string ru, string en)
        {
            string language = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : null;
            return !string.IsNullOrEmpty(language) && language.StartsWith("En", StringComparison.OrdinalIgnoreCase)
                ? en : ru;
        }

        private void OnDestroy()
        {
            if (root != null)
                Destroy(root);
        }
    }
}
