using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    public sealed class LobbyChatUI : MonoBehaviour
    {
        private MirraLobbyChat chat;
        private LobbyOnlineBootstrap owner;
        private GameObject root, panel;
        private InputField input;
        private Text status, history;
        private Button send;
        private bool ownsCursor;
        internal bool IsOpen => panel != null && panel.activeSelf;

        public void Initialize(MirraLobbyChat transport, LobbyOnlineBootstrap bootstrap)
        {
            chat = transport; owner = bootstrap;
            root = new GameObject("Lobby chat UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 1003;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
            MakeButton(root.transform, "Чат", new Vector2(1,1), new Vector2(-172,-147), new Vector2(330,48), () => SetOpen(true));
            panel = new GameObject("Lobby chat panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform,false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f); rect.sizeDelta = new Vector2(700,580);
            panel.GetComponent<Image>().color = new Color(.12f,.18f,.22f,.98f);
            status = Label(panel.transform,"",new Vector2(-25,244),new Vector2(610,50),21);
            MakeButton(panel.transform,"×",new Vector2(.5f,.5f),new Vector2(315,244),new Vector2(45,40),()=>SetOpen(false));
            var viewport = new GameObject("Chat scroll",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
            viewport.transform.SetParent(panel.transform,false);
            viewport.GetComponent<RectTransform>().sizeDelta = new Vector2(650,380);
            viewport.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,20);
            viewport.GetComponent<Image>().color = new Color(0,0,0,.15f);
            history = Label(viewport.transform,"",Vector2.zero,new Vector2(630,380),18);
            history.alignment = TextAnchor.UpperLeft;
            history.verticalOverflow = VerticalWrapMode.Overflow;
            var content = history.rectTransform;
            content.anchorMin = content.anchorMax = new Vector2(.5f,1); content.pivot = new Vector2(.5f,1);
            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.horizontal = false;
            var field = new GameObject("Message",typeof(RectTransform),typeof(Image),typeof(InputField));
            field.transform.SetParent(panel.transform,false);
            field.GetComponent<RectTransform>().sizeDelta = new Vector2(470,52);
            field.GetComponent<RectTransform>().anchoredPosition = new Vector2(-90,-219);
            field.GetComponent<Image>().color = new Color(.22f,.28f,.33f);
            input = field.GetComponent<InputField>(); input.characterLimit = 200;
            input.textComponent = Label(field.transform,"",Vector2.zero,new Vector2(450,48),18);
            input.placeholder = Label(field.transform,"Сообщение, до 200 символов",Vector2.zero,new Vector2(450,48),16);
            input.placeholder.color = Color.gray;
            input.onValueChanged.AddListener(_ => owner.StayOnline());
            send = MakeButton(panel.transform,"Отправить",new Vector2(.5f,.5f),new Vector2(240,-219),new Vector2(170,52),Send);
            chat.Changed += Refresh;
            panel.SetActive(false);
        }

        private async void Send()
        {
            string body = input.text;
            if (await chat.SendAsync(body) && this != null && input.text == body) input.text = "";
        }
        private void Update()
        {
            if (IsOpen && (!owner.IsOnline || owner.IsInDepartureRoom || owner.IsBrowsingDepartures || Input.GetKeyDown(KeyCode.Escape))) SetOpen(false);
            root.SetActive(owner.IsOnline && !owner.IsInDepartureRoom && !owner.IsBrowsingDepartures);
            if (IsOpen) send.interactable = chat.CanSend && !string.IsNullOrWhiteSpace(input.text);
        }
        private void SetOpen(bool value)
        {
            if (value && (!owner.IsOnline || owner.IsInDepartureRoom || owner.IsBrowsingDepartures ||
                owner.GetComponent<LobbyFriendsUI>()?.IsOpen == true || owner.GetComponent<LobbyOnlineModeUI>()?.IsOpen == true)) return;
            panel.SetActive(value);
            var controls = ControlManager.Instance;
            if (controls != null && !controls.UseTouchControl)
            {
                if (value && !ownsCursor) { controls.CursorActive=true; ownsCursor=true; }
                else if (!value && ownsCursor) { controls.CursorActive=false; ownsCursor=false; }
            }
            if (value) { owner.StayOnline(); Refresh(); }
        }
        private void Refresh()
        {
            if (!IsOpen) return;
            status.text=chat.Status;
            var text = new StringBuilder();
            foreach (var message in chat.Messages)
            {
                string id = message.SenderId ?? "?";
                // Nickname mapping follows verified profiles; never trust message metadata as identity.
                string name = id.Length > 6 ? id.Substring(id.Length-6) : id;
                string body = message.Body ?? "";
                if (body.Length > 200) body = body.Substring(0,200);
                text.Append("Игрок ").Append(name).Append(": ").AppendLine(body);
            }
            history.text=text.ToString();
            history.rectTransform.sizeDelta=new Vector2(630,Mathf.Max(380,history.preferredHeight));
            send.interactable=chat.CanSend && !string.IsNullOrWhiteSpace(input.text);
        }
        private static Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize)
        {
            var label=LobbyOnlineModeUI.CreateText(parent,"Text",position,size,fontSize);
            label.text=text; label.supportRichText=false; return label;
        }
        private static Button MakeButton(Transform parent,string text,Vector2 anchor,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var button=LobbyOnlineModeUI.CreateButton(parent,text,anchor,position,size,action);
            button.GetComponentInChildren<Text>().text=text; return button;
        }
        private void OnDestroy()
        {
            if(chat!=null) chat.Changed-=Refresh;
            if(ownsCursor && ControlManager.Instance!=null) ControlManager.Instance.CursorActive=false;
            if(root!=null) Destroy(root);
        }
    }
}
