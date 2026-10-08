using System;
using MirraCloud.Core.Friends.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    // Pilot UI: intentionally separate from departure admission / Photon authentication.
    public sealed class LobbyFriendsUI : MonoBehaviour
    {
        private LobbyOnlineBootstrap owner;
        private MirraSocialService social;
        private LobbyFriendInvites invites;
        private GameObject root, panel, rows;
        private Text status, identity, inviteStatus;
        private InputField target, inviteCode;
        private bool ownsCursor;
        private int page;
        private const int PageSize = 20;
        internal bool IsOpen => panel != null && panel.activeSelf;

        public void Initialize(LobbyOnlineBootstrap bootstrap)
        {
            owner = bootstrap;
            social = MirraSocialService.Instance;
            invites = bootstrap.GetComponent<LobbyFriendInvites>();
            social.Changed += Refresh;
            invites.Changed += Refresh;
            root = new GameObject("Friends UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 1002;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            Button(root.transform, "Друзья", new Vector2(1, 1), new Vector2(-172, -91), new Vector2(330, 48), () => SetOpen(true));
            panel = new GameObject("Friends panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 610);
            panel.GetComponent<RectTransform>().anchorMin = panel.GetComponent<RectTransform>().anchorMax = new Vector2(.5f, .5f);
            panel.GetComponent<Image>().color = new Color(.12f, .18f, .22f, .98f);
            Label(panel.transform, "Друзья — тестовая версия", new Vector2(0, 263), new Vector2(630, 40), 26);
            Button(panel.transform, "×", new Vector2(.5f, .5f), new Vector2(315, 264), new Vector2(45, 40), () => SetOpen(false));
            status = Label(panel.transform, "", new Vector2(0, 216), new Vector2(650, 45), 17);
            identity = Label(panel.transform, "", new Vector2(-60, 170), new Vector2(530, 38), 17);
            Button(panel.transform, "Копировать ID", new Vector2(.5f, .5f), new Vector2(247, 170), new Vector2(160, 38),
                () => GUIUtility.systemCopyBuffer = social.FriendId ?? "");
            var input = new GameObject("Friend ID", typeof(RectTransform), typeof(Image), typeof(InputField));
            input.transform.SetParent(panel.transform, false);
            input.GetComponent<RectTransform>().sizeDelta = new Vector2(435, 42);
            input.GetComponent<RectTransform>().anchoredPosition = new Vector2(-108, 117);
            input.GetComponent<Image>().color = new Color(.22f, .28f, .33f);
            target = input.GetComponent<InputField>();
            target.textComponent = Label(input.transform, "", Vector2.zero, new Vector2(420, 40), 18);
            target.textComponent.supportRichText = false;
            var placeholder = Label(input.transform, "ID игрока Mirra", Vector2.zero, new Vector2(420, 40), 18);
            placeholder.color = Color.gray;
            target.placeholder = placeholder;
            target.characterLimit = 128;
            Button(panel.transform, "Добавить", new Vector2(.5f, .5f), new Vector2(225, 117), new Vector2(200, 42),
                () => { owner.StayOnline(); _ = social.ChangeFriendAsync(target.text, "send"); });
            var codeInput = new GameObject("Invite code", typeof(RectTransform), typeof(Image), typeof(InputField));
            codeInput.transform.SetParent(panel.transform, false);
            codeInput.GetComponent<RectTransform>().sizeDelta = new Vector2(385, 40);
            codeInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130, 65);
            codeInput.GetComponent<Image>().color = new Color(.22f, .28f, .33f);
            inviteCode = codeInput.GetComponent<InputField>();
            inviteCode.textComponent = Label(codeInput.transform, "", Vector2.zero, new Vector2(370, 38), 16);
            inviteCode.textComponent.supportRichText = false;
            var codePlaceholder = Label(codeInput.transform, "Код приглашения DB1:…", Vector2.zero, new Vector2(370, 38), 16);
            codePlaceholder.color = Color.gray;
            inviteCode.placeholder = codePlaceholder;
            inviteCode.characterLimit = 180;
            Button(panel.transform, "Войти", new Vector2(.5f, .5f), new Vector2(157, 65), new Vector2(118, 40),
                () => { owner.StayOnline(); _ = invites.JoinAsync(inviteCode.text); });
            Button(panel.transform, "Код", new Vector2(.5f, .5f), new Vector2(278, 65), new Vector2(95, 40),
                () => GUIUtility.systemCopyBuffer = invites.Code ?? "");
            inviteStatus = Label(panel.transform, "", new Vector2(0, 20), new Vector2(650, 49), 15);
            Button(panel.transform, "Обновить", new Vector2(.5f, .5f), new Vector2(0, -263), new Vector2(220, 42),
                () => { owner.StayOnline(); _ = social.RefreshAsync(); });
            Button(panel.transform, "←", new Vector2(.5f, .5f), new Vector2(-190, -263), new Vector2(70, 42),
                () => { page = Mathf.Max(0, page - 1); Refresh(); });
            Button(panel.transform, "→", new Vector2(.5f, .5f), new Vector2(190, -263), new Vector2(70, 42),
                () => { page++; Refresh(); });
            var viewport = new GameObject("Friends scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(panel.transform, false);
            viewport.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 230);
            viewport.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -121);
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, .12f);
            rows = new GameObject("Rows", typeof(RectTransform));
            rows.transform.SetParent(viewport.transform, false);
            var content = rows.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.horizontal = false;
            panel.SetActive(false);
        }

        private void Update()
        {
            if (panel.activeSelf && (owner.IsBrowsingDepartures || owner.IsInDepartureRoom)) SetOpen(false);
            if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
        }

        private void SetOpen(bool value)
        {
            if (value && (owner.IsBrowsingDepartures || owner.IsInDepartureRoom ||
                owner.GetComponent<LobbyOnlineModeUI>()?.IsOpen == true || owner.GetComponent<LobbyChatUI>()?.IsOpen == true)) return;
            panel.SetActive(value);
            var controls = ControlManager.Instance;
            if (controls != null && !controls.UseTouchControl)
            {
                if (value && !ownsCursor) { controls.CursorActive = true; ownsCursor = true; }
                else if (!value && ownsCursor) { controls.CursorActive = false; ownsCursor = false; }
            }
            if (value) { owner.StayOnline(); Refresh(); _ = social.RefreshAsync(); }
        }

        private void Refresh()
        {
            if (panel == null || !panel.activeSelf) return;
            status.text = social.Busy ? "Подождите… " + social.Status : social.Status;
            if (invites.IsInviting && !string.IsNullOrEmpty(invites.Code) &&
                inviteCode.text != invites.Code)
                inviteCode.text = invites.Code;
            inviteStatus.text = invites.Status;
            identity.text = "Ваш ID: " + (social.FriendId ?? "пока недоступен");
            foreach (Transform child in rows.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            int index = 0;
            foreach (var friend in social.Friends)
            {
                if (friend == null) continue;
                Row(ref index, friend.PlayerInfo?.Nickname ?? friend.PlayerId, friend.PlayerId,
                    "Пригласить", "invite", "Удалить", "remove");
            }
            foreach (var request in social.Incoming)
            {
                if (request == null || request.Status != FriendRequestStatus.Pending) continue;
                Row(ref index, "Входящий: " + request.SourcePlayerId, request.SourcePlayerId, "Принять", "accept", "Отклонить", "reject");
            }
            foreach (var request in social.Outgoing)
            {
                if (request == null || request.Status != FriendRequestStatus.Pending) continue;
                Row(ref index, "Исходящий: " + request.TargetPlayerId, request.TargetPlayerId, "Отменить", "revoke");
            }
            if (index == 0) Row(ref index, social.Ready ? "Друзей и заявок пока нет" : "Подключитесь к Mirra кнопкой «Обновить»", null, null, null);
            if (page > 0 && page * PageSize >= index) { page = Mathf.Max(0, (index - 1) / PageSize); Refresh(); return; }
            rows.GetComponent<RectTransform>().sizeDelta = new Vector2(0, Mathf.Max(320, Mathf.Min(PageSize, index - page * PageSize) * 54));
        }

        private void Row(ref int index, string text, string id, string caption, string action, string caption2 = null, string action2 = null)
        {
            int itemIndex = index++;
            if (itemIndex < page * PageSize || itemIndex >= (page + 1) * PageSize) return;
            var row = new GameObject("Friend row", typeof(RectTransform));
            row.transform.SetParent(rows.transform, false);
            var rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(640, 50); rect.anchoredPosition = new Vector2(0, -27 - (itemIndex % PageSize) * 54);
            Label(row.transform, text, new Vector2(-120, 0), new Vector2(390, 50), 16);
            if (id == null) return;
            var button = Button(row.transform, caption, new Vector2(.5f, .5f), new Vector2(120, 0), new Vector2(116, 40),
                () => { owner.StayOnline(); if (action == "invite") _ = invites.InviteAsync(id); else _ = social.ChangeFriendAsync(id, action); });
            button.interactable = !social.Busy && !invites.Busy;
            if (caption2 != null)
                Button(row.transform, caption2, new Vector2(.5f, .5f), new Vector2(250, 0), new Vector2(126, 40),
                    () => { owner.StayOnline(); _ = social.ChangeFriendAsync(id, action2); }).interactable = !social.Busy && !invites.Busy;
        }

        private static Button Button(Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var button = LobbyOnlineModeUI.CreateButton(parent, text, anchor, position, size, action);
            button.GetComponentInChildren<Text>().text = text;
            return button;
        }
        private static Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var label = LobbyOnlineModeUI.CreateText(parent, "Label", position, size, fontSize);
            label.text = text; label.supportRichText = false; return label;
        }
        private void OnDestroy()
        {
            if (social != null) social.Changed -= Refresh;
            if (invites != null) invites.Changed -= Refresh;
            if (ownsCursor && ControlManager.Instance != null) ControlManager.Instance.CursorActive = false;
            if (root != null) Destroy(root);
        }
    }
}
