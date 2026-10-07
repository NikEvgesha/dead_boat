using System;
using System.Text;
using System.Collections.Generic;
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
        private Button send, author, visibility;
        private ChatLocalVisibility localVisibility = ChatLocalVisibility.Session;
        private readonly List<string> authors = new();
        private string selectedAuthor;
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
            author = MakeButton(panel.transform,"Выбрать игрока ▸",new Vector2(.5f,.5f),new Vector2(-115,-269),new Vector2(420,36),NextAuthor);
            visibility = MakeButton(panel.transform,"Скрыть у себя",new Vector2(.5f,.5f),new Vector2(240,-269),new Vector2(170,36),ToggleVisibility);
            author.GetComponentInChildren<Text>().fontSize = 17;
            visibility.GetComponentInChildren<Text>().fontSize = 15;
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
            if (value) { owner.StayOnline(); Refresh(); chat.RefreshHistory(); }
        }
        private void Refresh()
        {
            if (!IsOpen) return;
            status.text=chat.Status;
            var text = new StringBuilder();
            foreach (var message in chat.Messages)
            {
                if (localVisibility.IsHidden(message.SenderId)) continue;
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
            RefreshAuthors();
        }

        private void RefreshAuthors()
        {
            authors.Clear();
            foreach (var message in chat.Messages)
                if (!string.IsNullOrEmpty(message.SenderId) && !authors.Contains(message.SenderId)) authors.Add(message.SenderId);
            // Keep hidden players selectable even after their messages leave the 50-message buffer.
            foreach (var sender in localVisibility.HiddenSenders)
                if (!authors.Contains(sender)) authors.Add(sender);
            authors.Sort(StringComparer.Ordinal);
            if (!authors.Contains(selectedAuthor)) selectedAuthor = authors.Count > 0 ? authors[0] : null;
            author.interactable = authors.Count > 0;
            visibility.interactable = selectedAuthor != null;
            string shortName = selectedAuthor == null ? "Выбрать игрока" : "Игрок " +
                (selectedAuthor.Length > 6 ? selectedAuthor.Substring(selectedAuthor.Length - 6) : selectedAuthor);
            author.GetComponentInChildren<Text>().text = shortName + " ▸";
            visibility.GetComponentInChildren<Text>().text = localVisibility.IsHidden(selectedAuthor) ? "Показать у себя" : "Скрыть у себя";
        }

        private void NextAuthor()
        {
            if (authors.Count == 0) return;
            selectedAuthor = authors[(authors.IndexOf(selectedAuthor) + 1) % authors.Count];
            RefreshAuthors(); owner.StayOnline();
        }

        private void ToggleVisibility()
        {
            if (selectedAuthor == null) return;
            bool accepted = localVisibility.SetHidden(selectedAuthor, !localVisibility.IsHidden(selectedAuthor));
            Refresh();
            if (!accepted) status.text = "Скрыто слишком много игроков. Сначала включите сообщения одного из них";
            owner.StayOnline();
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
