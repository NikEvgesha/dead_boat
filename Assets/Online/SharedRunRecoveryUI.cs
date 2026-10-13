using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    public sealed class SharedRunRecoveryUI : MonoBehaviour
    {
        private SharedRunRecovery recovery;
        private GameObject panel;
        private GameObject backdrop;
        private Text title, message;
        private Button resume, solo, exit;
        private Button recall;
        private bool confirming, opened, previousMove;
        public void Initialize(SharedRunRecovery value)
        {
            recovery = value;
            var canvas = new GameObject("Run recovery UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 1100;
            canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
            recall = LobbyOnlineModeUI.CreateButton(canvas.transform, "PreviousCrew", new Vector2(.5f, 1),
                new Vector2(0, -115), new Vector2(320, 40), () => { _ = recovery.CheckAsync(); });
            recall.GetComponentInChildren<Text>().text = "Вернуться к прошлому экипажу";
            recall.gameObject.SetActive(false);
            backdrop = new GameObject("Recovery backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvas.transform, false);
            var screen = backdrop.GetComponent<RectTransform>();
            screen.anchorMin = Vector2.zero; screen.anchorMax = Vector2.one;
            screen.offsetMin = screen.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, .45f);
            panel = new GameObject("Recovery panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(backdrop.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(610, 400);
            panel.GetComponent<Image>().color = new Color(.07f, .14f, .19f, .98f);
            title = LobbyOnlineModeUI.CreateText(panel.transform, "Title", new Vector2(0, 143), new Vector2(560, 60), 30);
            message = LobbyOnlineModeUI.CreateText(panel.transform, "Message", new Vector2(0, 60), new Vector2(550, 95), 20);
            message.supportRichText = false;
            resume = LobbyOnlineModeUI.CreateButton(panel.transform, "Return", new Vector2(.5f, .5f),
                new Vector2(0, -30), new Vector2(500, 48), () => { confirming = false; _ = recovery.ResumeAsync(); });
            solo = LobbyOnlineModeUI.CreateButton(panel.transform, "Solo", new Vector2(.5f, .5f),
                new Vector2(0, -91), new Vector2(500, 48), () =>
                {
                    if (!confirming) confirming = true;
                    else { confirming = false; _ = recovery.ContinueSoloAsync(); }
                });
            exit = LobbyOnlineModeUI.CreateButton(panel.transform, "Exit", new Vector2(.5f, .5f),
                new Vector2(0, -150), new Vector2(500, 43), () =>
                {
                    if (recovery.CanCancelCheck) recovery.CancelCheck();
                    else if (confirming) confirming = false;
                    else if (recovery.Lost) _ = recovery.ExitAsync();
                    else _ = recovery.LaterAsync();
                });
            panel.SetActive(false);
            backdrop.SetActive(false);
        }
        private void Update()
        {
            bool visible = recovery != null && recovery.Visible;
            recall.gameObject.SetActive(!visible && SharedRunRecovery.HasPending && LoadingManager.Instance != null &&
                LoadingManager.Instance.CurrentLocation == Location.Lobby);
            recall.interactable = recovery != null && !recovery.Busy;
            backdrop.SetActive(visible);
            panel.SetActive(visible);
            var controls = ControlManager.Instance;
            if (controls != null && visible && !opened)
            {
                previousMove = controls.MoveActive;
                controls.MoveActive = false;
                controls.CursorActive = true;
            }
            else if (controls != null && !visible && opened)
            {
                controls.MoveActive = previousMove;
                controls.ForceGameplayCursor();
            }
            opened = visible;
            if (!visible) { confirming = false; return; }
            title.text = confirming ? "Продолжить одному?" : recovery.Lost ? "Связь потеряна" : "Вернуться к экипажу";
            message.text = confirming ? "Соло-забег будет сохраняться. После этого вернуться к прежнему экипажу уже нельзя." : recovery.Message;
            resume.gameObject.SetActive(!confirming);
            resume.GetComponentInChildren<Text>().text = recovery.Lost ? "Переподключиться" : "Вернуться в забег";
            solo.gameObject.SetActive(recovery.CanSolo);
            solo.GetComponentInChildren<Text>().text = confirming ? "Да, продолжить одному" : "Продолжить одному";
            exit.GetComponentInChildren<Text>().text = recovery.CanCancelCheck ? "Отменить подключение" : confirming ? "Остаться и ждать" : recovery.Lost ? "В лобби" : "Позже";
            resume.interactable = solo.interactable = exit.interactable = !recovery.Busy;
            resume.interactable &= recovery.CanRetry;
            exit.interactable = !recovery.Busy || recovery.CanCancelCheck;
        }
    }
}
