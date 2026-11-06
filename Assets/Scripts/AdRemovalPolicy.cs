using System;
using System.Linq;
using MirraGames.SDK;
using UnityEngine;

public static class AdRemovalPolicy
{
    public const string SuggestedProductId = "remove_ads_auto";
    public static event Action Changed;
    private static AdRemovalSettings settings;
    public static AdRemovalSettings Settings => settings ? settings : settings = Resources.Load<AdRemovalSettings>("Monetization/AdRemovalSettings");
    public static bool Configured => Settings && Settings.lifetimeAutomaticOnlyApproved && !string.IsNullOrWhiteSpace(Settings.productId);
    public static string ProductId => Configured ? Settings.productId : "";
    private static string Key => "DeadBoat.AdRemoval.Auto.v1." + ProductId;
    public static bool Owned
    {
        get
        {
            if (!Configured) return false;
#if UNITY_EDITOR
            if (temporary) return temporaryOwned;
#endif
            return MirraSDK.IsInitialized && MirraSDK.Data.GetBool(Key, false);
        }
    }
    public static bool RecognizePurchase(string id)
    {
        if (!Configured || id != ProductId) return false;
#if UNITY_EDITOR
        if (temporary) { temporaryOwned = true; Changed?.Invoke(); return true; }
#endif
        if (!MirraSDK.IsInitialized) return false;
        MirraSDK.Data.SetBool(Key, true); MirraSDK.Data.Save(); Changed?.Invoke(); return true;
    }
    public static void RestoreProducts(string[] ids)
    {
        if (Configured && ids != null && ids.Contains(ProductId)) RecognizePurchase(ProductId);
    }
#if UNITY_EDITOR
    private static bool temporary, temporaryOwned;
    public static IDisposable UseTemporarySettings(AdRemovalSettings testSettings)
    {
        if (temporary) throw new InvalidOperationException("Ad removal test already active");
        var previous = settings; temporary = true; temporaryOwned = false; settings = testSettings;
        return new Scope(() => { temporary = temporaryOwned = false; settings = previous; Changed?.Invoke(); });
    }
    private sealed class Scope : IDisposable { private Action end; public Scope(Action action) {end=action;} public void Dispose() {var action=end;end=null;action?.Invoke();} }
#endif
}
