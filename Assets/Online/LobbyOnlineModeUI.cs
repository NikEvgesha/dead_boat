using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    // Created by LobbyOnlineBootstrap so the choice also works in older saved Lobby scenes.
    public sealed class LobbyOnlineModeUI : MonoBehaviour
    {
        private LobbyOnlineBootstrap bootstrap;
        private GameObject uiRoot;
        private GameObject panel;
        private Text chipText;
        private Text statusText;
        private Button onlineButton;
        private Button offlineButton;
        private Button retryButton;
        private Button stayButton;
        private bool open;
        private bool ownsCursor;
        private float nextRefresh;

        public void Initialize(LobbyOnlineBootstrap owner, bool showFirstChoice)
        {
            bootstrap = owner;
            BuildUI();
            SetOpen(showFirstChoice);
            Refresh();
        }

        private void Update()
        {
            if (bootstrap == null)
                return;

            if (Input.GetKeyDown(KeyCode.O))
                SetOpen(!open);

            if (Time.unscaledTime < nextRefresh)
                return;

            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        private void BuildUI()
        {
            var root = new GameObject("Online mode UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiRoot = root;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            var chip = CreateButton(root.transform, "Mode button", new Vector2(1, 1),
                new Vector2(-172, -32), new Vector2(330, 54), () => SetOpen(!open));
            chipText = chip.GetComponentInChildren<Text>();

            panel = new GameObject("Mode panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(530, 320);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.13f, 0.96f);

            CreateText(panel.transform, "Title", new Vector2(0, 115), new Vector2(490, 52), 29);
            statusText = CreateText(panel.transform, "Status", new Vector2(0, 43), new Vector2(480, 80), 21);
            onlineButton = CreateButton(panel.transform, "Online", new Vector2(0.5f, 0.5f),
                new Vector2(-124, -55), new Vector2(225, 52), ChooseOnline);
            offlineButton = CreateButton(panel.transform, "Offline", new Vector2(0.5f, 0.5f),
                new Vector2(124, -55), new Vector2(225, 52), ChooseOffline);
            retryButton = CreateButton(panel.transform, "Reconnect", new Vector2(0.5f, 0.5f),
                new Vector2(0, -116), new Vector2(310, 46), ChooseOnline);
            stayButton = CreateButton(panel.transform, "Stay online", new Vector2(0.5f, 0.5f),
                new Vector2(0, -116), new Vector2(310, 46), bootstrap.StayOnline);
            CreateButton(panel.transform, "Close", new Vector2(1, 1),
                new Vector2(-23, -22), new Vector2(42, 42), () => SetOpen(false))
                .GetComponentInChildren<Text>().text = "×";
        }

        private void ChooseOnline()
        {
            bootstrap.SelectOnline();
            SetOpen(false);
            Refresh();
        }

        private void ChooseOffline()
        {
            bootstrap.SelectOffline();
            SetOpen(false);
            Refresh();
        }

        private void SetOpen(bool value)
        {
            open = value;
            if (panel != null)
                panel.SetActive(value);

            var controls = ControlManager.Instance;
            if (controls == null || controls.UseTouchControl)
                return;

            if (value && !ownsCursor)
            {
                controls.CursorActive = true;
                ownsCursor = true;
            }
            else if (!value && ownsCursor)
            {
                controls.CursorActive = false;
                ownsCursor = false;
            }
        }

        private void Refresh()
        {
            bool english = LocalizationManager.Instance != null &&
                !string.IsNullOrEmpty(LocalizationManager.Instance.CurrentLanguage) &&
                LocalizationManager.Instance.CurrentLanguage.StartsWith("En", System.StringComparison.OrdinalIgnoreCase);
            if (panel != null)
                panel.transform.Find("Title").GetComponent<Text>().text = english ? "Play mode" : "Режим игры";

            string state;
            if (bootstrap.IdleWarning)
                state = (english ? "Idle disconnect in " : "Отключение из-за бездействия через ") +
                    bootstrap.IdleSecondsRemaining + (english ? " s. Press Stay online." : " с. Нажмите «Остаться». ");
            else if (bootstrap.IsOnline)
                state = english ? "Online lobby" : "Онлайн-лобби";
            else if (bootstrap.IsConnecting)
                state = english ? "Connecting… Solo play is available." : "Подключаемся… Одиночная игра доступна.";
            else if (bootstrap.Mode == LobbyOnlineMode.Unselected)
                state = english ? "Choose how to play." : "Выберите, как играть.";
            else if (bootstrap.Mode == LobbyOnlineMode.Offline)
                state = english ? "Offline. Photon is disconnected." : "Офлайн. Photon отключён.";
            else if (bootstrap.IdleDisconnected)
                state = english ? "Disconnected after inactivity. Reconnect when ready." :
                    "Отключено из-за бездействия. Можно подключиться снова.";
            else
                state = FailureText(english);

            statusText.text = state;
            chipText.text = bootstrap.IdleWarning
                ? (english ? "Disconnect in " : "Отключение через ") + bootstrap.IdleSecondsRemaining + " s  [O]"
                : (english ? "Mode: " : "Режим: ") +
                  (bootstrap.IsOnline ? (english ? "online" : "онлайн") :
                   bootstrap.IsConnecting ? (english ? "connecting" : "подключение") :
                   (english ? "offline" : "офлайн")) + "  [O]";
            chipText.color = bootstrap.IdleWarning ? new Color(1f, 0.8f, 0.3f) : Color.white;
            onlineButton.GetComponentInChildren<Text>().text = english ? "Play online" : "Играть онлайн";
            offlineButton.GetComponentInChildren<Text>().text = english ? "Play offline" : "Играть офлайн";
            retryButton.GetComponentInChildren<Text>().text = english ? "Reconnect" : "Подключиться ↻";
            stayButton.GetComponentInChildren<Text>().text = english ? "Stay online" : "Остаться онлайн";
            stayButton.gameObject.SetActive(bootstrap.IdleWarning);
            retryButton.gameObject.SetActive(bootstrap.Mode == LobbyOnlineMode.Online &&
                !bootstrap.IsOnline && !bootstrap.IsConnecting);
            retryButton.interactable = !bootstrap.IsConnecting;
        }

        private string FailureText(bool english)
        {
            switch (bootstrap.Failure)
            {
                case LobbyConnectionFailure.NoNetwork:
                    return english ? "No network. Play offline or reconnect later." :
                        "Нет сети. Играйте офлайн или подключитесь позже.";
                case LobbyConnectionFailure.Capacity:
                    return english ? "Online is full. Try again later; offline play is available." :
                        "Сейчас нет мест. Попробуйте позже; офлайн доступен.";
                case LobbyConnectionFailure.Timeout:
                    return english ? "Connection timed out. You can retry." :
                        "Сервер не ответил. Можно попробовать ещё раз.";
                default:
                    return english ? "Offline. You can reconnect at any time." :
                        "Офлайн. Можно подключиться в любой момент.";
            }
        }

        private static Button CreateButton(Transform parent, string name, Vector2 anchor,
            Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = item.GetComponent<Image>();
            image.color = new Color(0.13f, 0.30f, 0.42f, 0.97f);
            var button = item.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            CreateText(item.transform, "Label", Vector2.zero, size - new Vector2(8, 4), 20);
            return button;
        }

        private static Text CreateText(Transform parent, string name, Vector2 position,
            Vector2 size, int fontSize)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Text));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = item.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        private void OnDestroy()
        {
            if (ownsCursor && ControlManager.Instance != null)
                ControlManager.Instance.CursorActive = false;
            if (uiRoot != null)
                Destroy(uiRoot);
        }
    }
}
