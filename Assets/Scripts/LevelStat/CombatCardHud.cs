using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

// Presentation snapshots only: no gameplay callbacks, scene scans, networking or per-hit instantiation.
public sealed class CombatCardHud : MonoBehaviour
{
    private sealed class Row
    {
        public RectTransform Rect;
        public Image Back;
        public Text Text;
        public MMF_Player Pulse;
        private int rank = -1, state = -1;
        private double value;
        private string language;
        public void Render(BoostType type, int nextRank, int nextState, double nextValue, string nextLanguage)
        {
            if (rank == nextRank && state == nextState && value == nextValue && language == nextLanguage) return;
            rank = nextRank; state = nextState; value = nextValue; language = nextLanguage;
            Text.text = CombatCardStatusText.Label(type, rank, state, value);
            Back.color = Color.Lerp(new Color(.06f, .08f, .12f, .8f), new Color(.65f, .3f, .12f, .85f), state == 1 ? .4f : 0);
        }
        public void Play() { Pulse.StopFeedbacks(); Rect.localScale = Vector3.one; CardFeelFeedback.Play(Pulse); }
        public void Stop() { Pulse.StopFeedbacks(); Rect.localScale = Vector3.one; }
    }
    private static readonly BoostType[] Types = { BoostType.BloodPact, BoostType.BattleRhythm, BoostType.OnTheEdge, BoostType.SteadyAim, BoostType.PatientReload };
    private RectTransform root;
    private Row[] rows;
    private double nextPoll, lastHealTotal, pendingHeal, shownHeal, nextHealCue, healUntil, thirdUntil;
    private int epoch, thirdRevision;
    private bool edgeWasActive, aimWasReady, reloadWasReady;
    public static void Attach(LevelUpUi ui)
    {
        if (ui == null || ui.GetComponent<CombatCardHud>() != null) return;
        var hud = ui.gameObject.AddComponent<CombatCardHud>(); hud.Initialize(ui.transform);
    }
    private void Initialize(Transform host)
    {
        var go = new GameObject("Combat card status", typeof(RectTransform), typeof(CanvasGroup));
        root = (RectTransform)go.transform; root.SetParent(host, false);
        root.anchorMin = root.anchorMax = root.pivot = Vector2.one;
        root.anchoredPosition = new Vector2(-24, -232); root.sizeDelta = new Vector2(280, 310);
        var group = go.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
        var sample = host.GetComponentInChildren<Text>(true);
        var font = sample != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rows = new Row[Types.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            var panel = new GameObject(Types[i].ToString(), typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform; rect.SetParent(root, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1); rect.sizeDelta = new Vector2(280, 56);
            var back = panel.GetComponent<Image>(); back.raycastTarget = false;
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconObject.transform; iconRect.SetParent(rect, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f); iconRect.anchoredPosition = new Vector2(24, 0); iconRect.sizeDelta = Vector2.one * 32;
            var icon = iconObject.GetComponent<Image>(); icon.raycastTarget = false; icon.preserveAspect = true;
            var card = Resources.Load<BoostItem>("BoostItem_" + Types[i]); icon.sprite = card != null ? card.BoostIcon : null;
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = (RectTransform)labelObject.transform; labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(46, 3); labelRect.offsetMax = new Vector2(-5, -3);
            var text = labelObject.GetComponent<Text>(); text.font = font; text.fontSize = 17; text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            var pulse = CardFeelFeedback.NewPlayer(rect, "FEEL status pulse");
            CardFeelFeedback.AddScale(pulse, rect, Vector3.one, .22f, new Keyframe(0, 1), new Keyframe(.3f, 1.035f), new Keyframe(1, 1));
            CardFeelFeedback.InitializePlayer(pulse);
            rows[i] = new Row { Rect = rect, Back = back, Text = text, Pulse = pulse };
        }
        ResetSnapshot(); root.gameObject.SetActive(false);
    }
    private void ResetSnapshot()
    {
        epoch = CombatCards.PresentationEpoch; thirdRevision = CombatCards.ThirdHitRevision;
        lastHealTotal = CombatCards.HealingTotal; pendingHeal = shownHeal = 0;
        healUntil = thirdUntil = nextHealCue = 0; edgeWasActive = aimWasReady = reloadWasReady = false;
    }
    private void Update()
    {
        if (root == null || Time.unscaledTimeAsDouble < nextPoll) return;
        nextPoll = Time.unscaledTimeAsDouble + .1;
        Refresh();
    }
    private void Refresh()
    {
        if (epoch != CombatCards.PresentationEpoch) { ResetSnapshot(); foreach (var row in rows) row.Stop(); }
        var stats = LevelStatManager.Instance != null ? LevelStatManager.Instance.Stats : default;
        var player = PlayerStatsManager.Instance;
        bool visible = LoadingManager.Instance != null && LoadingManager.Instance.CurrentLocation == Location.Game &&
            player != null && player.InGame && player.Health > 0 && Time.timeScale > 0 &&
            (PauseManager.Instance == null || !PauseManager.Instance.IsPaused) && !DeadBoat.Online.SharedAdProtection.IsShowing &&
            !DeadBoat.Online.SharedLocalGameplay.Blocked;
        double now = Time.timeAsDouble;
        double healed = System.Math.Max(0, CombatCards.HealingTotal - lastHealTotal); lastHealTotal = CombatCards.HealingTotal;
        bool third = CombatCards.ThirdHitRevision != thirdRevision; thirdRevision = CombatCards.ThirdHitRevision;
        bool edge = stats.OnTheEdgeRank > 0 && player != null && CombatCardRules.EdgeActive(player.Health, player.MaxHealth);
        bool aim = stats.SteadyAimRank > 0 && CombatCards.Ranged.AimReady(now);
        bool reload = stats.PatientReloadRank > 0 && CombatCards.Ranged.ReloadReady(now);
        bool aimCue = aim && !aimWasReady, reloadCue = reload && !reloadWasReady;
        aimWasReady = aim; reloadWasReady = reload;
        bool healCue = false, thirdCue = false, edgeCue = false;
        if (!visible) { pendingHeal = shownHeal = 0; healUntil = thirdUntil = 0; }
        else
        {
            pendingHeal += healed;
            if (pendingHeal > 0 && now >= nextHealCue)
            {
                shownHeal = pendingHeal; pendingHeal = 0; healUntil = now + .8; nextHealCue = now + .3;
                healCue = true;
            }
            if (third) { thirdUntil = now + .65; thirdCue = true; }
            if (edge && !edgeWasActive && stats.OnTheEdgeRank > 0) edgeCue = true;
        }
        edgeWasActive = edge;
        string language = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : "Ru";
        int count = 0;
        for (int i = 0; i < rows.Length; i++)
        {
            int rank = CombatCardRules.Rank(stats, Types[i]); var row = rows[i];
            bool owned = rank > 0;
            if (row.Rect.gameObject.activeSelf != owned) row.Rect.gameObject.SetActive(owned);
            if (!owned) continue;
            var pos = new Vector2(0, -count++ * 62);
            if (row.Rect.anchoredPosition != pos) row.Rect.anchoredPosition = pos;
            int state = i == 0 ? (now < healUntil ? 1 : 0) : i == 1 ? (now < thirdUntil ? 1 : 0) : i == 2 ? (edge ? 1 : 0) : i == 3 ? (aim ? 1 : 0) : reload ? 1 : 0;
            double value = i == 0 ? shownHeal : i == 1 && state == 0 ? CombatCards.ComboHits : 0;
            row.Render(Types[i], rank, state, value, language);
        }
        visible &= count > 0;
        if (root.gameObject.activeSelf != visible)
        {
            if (!visible) foreach (var row in rows) row.Stop();
            root.gameObject.SetActive(visible);
        }
        if (visible)
        {
            if (healCue && stats.BloodPactRank > 0) rows[0].Play();
            if (thirdCue && stats.BattleRhythmRank > 0) rows[1].Play();
            if (edgeCue && stats.OnTheEdgeRank > 0) rows[2].Play();
            if (aimCue) rows[3].Play();
            if (reloadCue) rows[4].Play();
        }
    }
    private void OnDisable()
    {
        if (root == null) return;
        foreach (var row in rows) row.Stop(); root.gameObject.SetActive(false); ResetSnapshot();
    }
}
