// One local player, one selected weapon. Game-time clocks; no scene/network objects.
public sealed class RangedCardState
{
    private int weapon, reloadAttempt, pendingReload;
    private bool emptyReload;
    private double stationarySince = double.NaN, aimAfter, reloadUntil;
    public bool AimReady(double now) => weapon != 0 && !double.IsNaN(stationarySince) && now - stationarySince >= .75 && now >= aimAfter;
    public bool ReloadReady(double now) => weapon != 0 && reloadUntil > now;
    public void Reset(double now)
    {
        weapon = pendingReload = 0; stationarySince = double.NaN; reloadUntil = 0;
        aimAfter = now + 3; // Recovery cannot bypass an in-flight cooldown.
    }
    private void Select(int token)
    {
        if (weapon == token) return;
        weapon = token; stationarySince = double.NaN; reloadUntil = 0; pendingReload = 0;
    }
    public void Observe(int token, double now, bool stationary, bool allowed, bool hasAim)
    {
        Select(token);
        if (!allowed) { Interrupt(token); return; }
        if (!stationary || !hasAim || pendingReload != 0) stationarySince = double.NaN;
        else if (double.IsNaN(stationarySince)) stationarySince = now;
    }
    public void Interrupt(int token)
    {
        if (weapon != token) return;
        weapon = pendingReload = 0; stationarySince = double.NaN; reloadUntil = 0;
        // Switching, menus and ads never shorten aimAfter.
    }
    public int BeginReload(int token, bool empty, bool hasCard)
    {
        Select(token); stationarySince = double.NaN; reloadUntil = 0;
        emptyReload = empty && hasCard; pendingReload = ++reloadAttempt;
        return pendingReload;
    }
    public void CompleteReload(int token, int attempt, double now, bool receivedAmmo)
    {
        if (weapon != token || attempt == 0 || pendingReload != attempt) return;
        pendingReload = 0;
        if (emptyReload && receivedAmmo) reloadUntil = now + 3;
    }
    public float TakeShot(int token, double now, int aimRank, int reloadRank)
    {
        Select(token);
        float bonus = 0;
        if (aimRank > 0 && AimReady(now)) { bonus += CombatCardRules.AimBonus(aimRank); aimAfter = now + 3; }
        if (reloadRank > 0 && ReloadReady(now)) bonus += CombatCardRules.PatientBonus(reloadRank);
        stationarySince = double.NaN; reloadUntil = 0; pendingReload = 0;
        return bonus; // Consumed before raycasts, including misses and shotguns.
    }
}
