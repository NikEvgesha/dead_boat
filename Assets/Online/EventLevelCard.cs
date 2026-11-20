using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online
{
    // The picker refreshes this view even while its row is hidden.
    public sealed class EventLevelCard : MonoBehaviour
    {
        private int levelId;
        private LevelData level;
        private bool forCrew;
        private Button button;
        private Text title, description, schedule;
        private EventWindow displayedState;
        private ScheduledRunEvent displayed;
        private string displayedLanguage;

        public static EventLevelCard Create(Transform parent, int id, LevelData level, Font font,
            bool forCrew, Action<RunEventContent> select)
        {
            var root = new GameObject("Event map card", typeof(RectTransform), typeof(EventLevelCard));
            root.transform.SetParent(parent, false);
            root.transform.SetAsFirstSibling();
            var card = root.GetComponent<EventLevelCard>();
            card.levelId = id;
            card.level = level;
            card.forCrew = forCrew;
            card.Build(font);
            card.button.onClick.AddListener(() =>
            {
                if (card.button.IsInteractable() && level.Unlocked &&
                    RunEventCalendar.Current.IsOpenFor(id, card.displayed.Content, forCrew))
                    select(card.displayed.Content);
                else
                    card.Refresh();
            });
            card.Refresh();
            return card;
        }

        public void Refresh()
        {
            var state = RunEventCalendar.Current.GetWindow(levelId, out var window);
            bool visible = state == EventWindow.Open || state == EventWindow.Upcoming;
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
            if (!visible) { displayedState = state; return; }

            button.interactable = level.Unlocked && state == EventWindow.Open && (!forCrew || window.CrewReady);
            var localization = LocalizationManager.Instance;
            if (localization == null) return;
            string language = localization.CurrentLanguage;
            if (state == displayedState && window.Content.Matches(displayed.Content) &&
                window.StartUtc == displayed.StartUtc && window.EndUtc == displayed.EndUtc &&
                window.CrewReady == displayed.CrewReady && displayedLanguage == language) return;
            displayedState = state;
            displayed = window;
            displayedLanguage = language;

            string Translate(string key) => localization.LocalizationData.GetTranslation(key, language);
            title.text = level.Title;
            description.text = Translate("Level/" + level.Name + "Description");
            bool upcoming = state == EventWindow.Upcoming;
            var date = upcoming ? window.StartUtc.Value : window.EndUtc.Value;
            string format = language == "Ru" ? "dd.MM.yyyy HH:mm" : "dd MMM yyyy HH:mm";
            string timestamp = date.ToString(format, CultureInfo.InvariantCulture);
            schedule.text = string.Format(Translate(upcoming ? "Level/EventOpens" : "Level/EventUntil"), timestamp);
            if (forCrew && !window.CrewReady) schedule.text += "  ·  " + Translate("Level/EventSolo");
        }

        private void Build(Font font)
        {
            var surface = Rect(transform, "Map", forCrew ? Vector2.zero : new Vector2(.025f, .10f),
                forCrew ? Vector2.one : new Vector2(.975f, .94f));
            var background = surface.gameObject.AddComponent<Image>();
            background.sprite = Resources.Load<Sprite>("OnlineUI/OnlineFrame");
            background.type = Image.Type.Sliced;
            button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var art = Rect(surface, "Winter route", forCrew ? new Vector2(.025f, .08f) : new Vector2(.06f, .39f),
                forCrew ? new Vector2(.22f, .92f) : new Vector2(.94f, .81f)).gameObject.AddComponent<Image>();
            art.sprite = level.IMG;
            art.preserveAspect = true;
            art.raycastTarget = false;
            if (forCrew)
            {
                ((RectTransform)transform).sizeDelta = new Vector2(710, 132);
                title = Label(surface, font, "Title", .245f, .61f, .97f, .97f, 27);
                description = Label(surface, font, "Description", .245f, .31f, .97f, .62f, 21);
                schedule = Label(surface, font, "Schedule", .245f, .04f, .97f, .31f, 18);
            }
            else
            {
                var shape = surface.gameObject.AddComponent<AspectRatioFitter>();
                shape.aspectRatio = .54f;
                shape.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                title = Label(surface, font, "Title", .045f, .82f, .955f, .98f, 48);
                description = Label(surface, font, "Description", .065f, .17f, .935f, .39f, 35);
                schedule = Label(surface, font, "Schedule", .05f, .025f, .95f, .16f, 29);
            }
            schedule.color = new Color32(168, 234, 244, 255);
        }

        private Text Label(Transform parent, Font font, string name, float left, float bottom,
            float right, float top, int size)
        {
            var label = Rect(parent, name, new Vector2(left, bottom), new Vector2(right, top)).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = label.resizeTextMaxSize = size;
            label.resizeTextMinSize = 12;
            label.resizeTextForBestFit = true;
            label.alignment = forCrew ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
