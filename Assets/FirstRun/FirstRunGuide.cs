using System;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.FirstRun
{
    // One small HUD panel. Card choices use the game's existing localized card UI and FEEL.
    public sealed class FirstRunGuide : MonoBehaviour
    {
        public event Action ReturnToLobby;
        private Text label, buttonLabel;
        private GameObject panel, button;
        private float panelWidth;
        public static FirstRunGuide Create(Transform parent)
        {
            var root = new GameObject("First run guidance", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var guide = root.AddComponent<FirstRunGuide>(); guide.Build(); return guide;
        }
        private void Build()
        {
            panel = new GameObject("Objective", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -125); rect.sizeDelta = new Vector2(720, 112);
            var image = panel.GetComponent<Image>(); image.color = new Color(.035f, .09f, .13f, .94f); image.raycastTarget = false;
            label = Label("Hint", panel.transform, new Vector2(680, 100), 23);
            label.supportRichText = false;
            button = new GameObject("Return to lobby", typeof(RectTransform), typeof(Image), typeof(Button)); button.transform.SetParent(panel.transform, false);
            var br = (RectTransform)button.transform; br.anchorMin = br.anchorMax = new Vector2(.5f, 0); br.anchoredPosition = new Vector2(0, -38); br.sizeDelta = new Vector2(360, 56);
            button.GetComponent<Image>().color = new Color(.16f, .7f, .2f);
            button.GetComponent<Button>().onClick.AddListener(() => ReturnToLobby?.Invoke());
            buttonLabel = Label("Button text", button.transform, new Vector2(340, 52), 24);
            panel.SetActive(false);
        }
        private static Text Label(string name, Transform parent, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = size;
            var text = go.GetComponent<Text>(); text.font = Resources.Load<Font>("Fonts/RussoOne-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize; text.resizeTextForBestFit = true; text.resizeTextMinSize = 17; text.resizeTextMaxSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false;
            return text;
        }
        public void Show(FirstRunLesson stage, int bossHP, bool cardsOpen, int introStep = 0)
        {
            var canvas = GetComponent<Canvas>();
            float width = Mathf.Min(720, canvas.pixelRect.width / Mathf.Max(.01f, canvas.scaleFactor) - 32);
            if (Mathf.Abs(width - panelWidth) > 1)
            {
                panelWidth = width;
                ((RectTransform)panel.transform).sizeDelta = new Vector2(width, 112);
                label.rectTransform.sizeDelta = new Vector2(width - 40, 100);
                ((RectTransform)button.transform).sizeDelta = new Vector2(Mathf.Min(360, width - 32), 56);
                buttonLabel.rectTransform.sizeDelta = new Vector2(Mathf.Min(340, width - 48), 52);
            }
            panel.SetActive(!cardsOpen);
            button.SetActive(stage == FirstRunLesson.Completed);
            label.text = stage == FirstRunLesson.Intro ? FirstRunText.Intro(introStep) : FirstRunText.Hint(stage, bossHP);
            buttonLabel.text = EggFeatureLocalization.Text("FirstRun/ToLobby", "В лобби", "To the lobby");
        }
    }
    public static class FirstRunText
    {
        private static readonly string[] introRu = new[] { "Подбери слиток перед собой. Следуй стрелке: она показывает текущую цель.", "Отнеси слиток на точку продажи. Ценности превращаются в деньги.", "Забери мешок с выручкой у торговца.", "Купи уголь у отмеченной торговой точки: он пригодится лодке.", "Подбери купленный уголь. Предметы можно носить перед собой или убирать в сумку.", "Отнеси уголь в топку лодки, чтобы заправить её.", "Займи место у руля и начинай плавание. Впереди две короткие вылазки!" };
        private static readonly string[] introEn = new[] { "Pick up the ingot in front of you. Follow the arrow: it points to your current objective.", "Carry the ingot to the selling spot. Valuables turn into money.", "Collect the bag of coins from the merchant.", "Buy coal at the marked shop: your boat needs fuel.", "Pick up the coal you bought. Carry items in front of you or store them in your bag.", "Carry the coal into the boat furnace to refuel.", "Take the helm and start sailing. Two short shore trips await!" };
        public static string Intro(int step)
        {
            int i = Mathf.Clamp(step, 0, introRu.Length - 1);
            return EggFeatureLocalization.Text("FirstRun/Intro" + i, introRu[i], introEn[i]);
        }
        public static string Hint(FirstRunLesson stage, int hp)
        {
            string ru, en;
            switch (stage)
            {
                case FirstRunLesson.SailFirst: ru = "Первая вылазка! Плыви к дому у левого берега. Лодка подождёт, пока ты исследуешь остановку."; en = "Your first shore trip! Sail to the house on the left bank. The boat will wait while you explore."; break;
                case FirstRunLesson.FirstFight: ru = "Высадись у дома и победи охранника мечом. Добычу можно нести, убирать в сумку или закреплять на лодке."; en = "Go ashore and defeat the guard with your sword. Carry loot, store it in your bag or attach it to the boat."; break;
                case FirstRunLesson.Egg: ru = "Забери яйцо возле дома в сумку. Оно сохранится в коллекции: в лобби его можно поместить в гнездо и получить животное."; en = "Collect the egg by the house with your bag. It goes to your collection: place it in a lobby nest to hatch an animal."; break;
                case FirstRunLesson.FirstCard: ru = "Яйцо пригодится в лобби. А этот временный спутник поможет сейчас: немного больше скорости и опыта. Выбери первую карточку!"; en = "Keep the egg for the lobby. This temporary companion helps now with a little more speed and XP. Choose your first card!"; break;
                case FirstRunLesson.SailSecond: ru = "Вернись к рулю и плыви ко второму дому. Твоя карточка уже работает; спутник путешествует с тобой."; en = "Return to the helm and sail to the second house. Your card is already active; your companion travels with you."; break;
                case FirstRunLesson.Supplies: ru = "Возьми пистолет в сумку и подбери патроны. Выбери пистолет в быстром слоте и выстрели. Не забывай перезаряжаться!"; en = "Put the pistol in your bag and collect the ammo. Equip the pistol from a quick slot and fire. Remember to reload!"; break;
                case FirstRunLesson.SecondFight: ru = "Победи второго охранника. Выстрелы расходуют патроны; бочка и уголь пригодятся для заправки лодки."; en = "Defeat the second guard. Shots use ammunition; the barrel and coal can fuel your boat."; break;
                case FirstRunLesson.SecondCard: ru = "Ещё одна карточка! Выбери усиление для стрельбы: эффекты выбранных карточек работают вместе."; en = "Another card! Pick a shooting upgrade: the effects of your chosen cards work together."; break;
                case FirstRunLesson.SailBoss: ru = "Финальный короткий заплыв. Заправь лодку, возьми патроны и подготовься к маленькому дракону."; en = "One short sail to the finish. Refuel, take ammunition and get ready for the little dragon."; break;
                case FirstRunLesson.Boss: ru = "Мини-дракон: {hp} HP. Стреляй, когда он рядом, и уходи с места, куда летит огненный шар."; en = "Little dragon: {hp} HP. Shoot when it is near and move away from incoming fireballs."; break;
                case FirstRunLesson.Completed: ru = "Первый заплыв пройден! В лобби тебя ждут гнёзда для яиц, животные и профессии. Дальше — новые места и совместные приключения."; en = "First voyage complete! The lobby has egg nests, animals and professions. New places and co-op adventures await."; break;
                default: ru = ""; en = ""; break;
            }
            return EggFeatureLocalization.Text("FirstRun/" + stage, ru, en).Replace("{hp}", hp.ToString());
        }
    }
}
