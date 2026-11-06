using UnityEngine;

[CreateAssetMenu(menuName = "Dead Boat/Ad removal settings")]
public sealed class AdRemovalSettings : ScriptableObject
{
    [Tooltip("Empty until a real platform product exists. Suggested key: remove_ads_auto")]
    public string productId = "";
    [Tooltip("Enable only after owner approves permanent removal of automatic interstitials. Rewarded stays optional.")]
    public bool lifetimeAutomaticOnlyApproved;
}
