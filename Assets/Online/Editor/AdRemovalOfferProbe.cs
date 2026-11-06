using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadBoat.Online.Editor
{
    public sealed class AdRemovalPurchaseProbe : PurchasesProvider
    {
        public override void Initialize() { }
        public override void BuyPurchase(string id, Action<bool> done) => done(true);
        public override void ConsumePendingPurchases() { }
        public override PurchaseData GetPurchaseData(string id) => new PurchaseData(id,"","","99.50","RUB");
        public override bool PurchasesAvailable() => true;
    }
    public static class AdRemovalOfferProbe
    {
        public static string Result="not run";
        public static async void Run()
        {
            if(!EditorApplication.isPlaying || Result=="running") return;
            Result="running";
            var config=ScriptableObject.CreateInstance<AdRemovalSettings>();
            GameObject root=null; AdsManager ads=null; PurchasesManager purchases=null;
            using(AdRemovalPolicy.UseTemporarySettings(config))
            try
            {
                Require(LoadingManager.Instance==null,"isolated scene");
                root=new GameObject("Ad removal fake providers");var provider=root.AddComponent<SharedProbeAdsProvider>();
                ads=AdsManager.Instance;ads.AddProvider(provider);var offer=ads.GetComponent<AdRemovalOffer>();
                Require(offer!=null && !offer.Visible && !AdRemovalPolicy.Configured,"default disabled, no icon");
                int confirmed=0;ads.ShownAdClosed+=_=>confirmed++;
                ads.ShowInterstitialAd();provider.callback(false);await Task.Delay(30);
                Require(confirmed==0 && !offer.Visible,"failed close without open gives no offer");
                provider.onShow=()=>typeof(AdsProvider).GetMethod("NotifyOpened",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(provider,null);
                ads.ShowRewardedAd("probe",_=>{});provider.callback(false);await Task.Delay(30);
                Require(confirmed==1 && offer.Visible,"actually opened but unrewarded ad shows offer");
                offer.SetOpen(true);
                var buy=(Button)typeof(AdRemovalOffer).GetField("buy",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(offer);
                Require(offer.IsOpen && !buy.gameObject.activeSelf,"placeholder has no purchase button");
                ScreenCapture.CaptureScreenshot("Logs/AdRemovalOfferPreview.png"); await Task.Delay(300);
                Require(!AdRemovalPolicy.RecognizePurchase("arbitrary"),"unknown product never grants rights");
                offer.SetOpen(false);config.productId="test_auto";config.lifetimeAutomaticOnlyApproved=true;
                purchases=PurchasesManager.Instance;purchases.SetProvider(root.AddComponent<AdRemovalPurchaseProbe>());
                offer.SetOpen(true);
                Require(buy.interactable && buy.GetComponentInChildren<Text>().text.Contains("99.50"),"actual provider price");
                buy.onClick.Invoke();
                Require(AdRemovalPolicy.Owned && !offer.Visible && !offer.IsOpen,"purchase callback grants and hides");
                int before=provider.requests;bool callback=true;ads.ShowInterstitialAd(ok=>callback=ok);
                Require(provider.requests==before && !callback,"owned skips interstitial without reporting shown");
                ads.ShowRewardedAd("still_optional",_=>{});provider.callback(true);
                Require(provider.requests==before+1,"rewarded unchanged");
                Result="PASS: failed show suppressed, opened ad offer, placeholder without purchase, configured real price, successful fake purchase hides, auto skipped/rewarded remains";
            }
            catch(Exception e){Result="FAIL: "+e;Debug.LogError(Result);}
            finally
            {
                SharedAdProtection.End();if(ads) UnityEngine.Object.Destroy(ads.gameObject);
                if(purchases) UnityEngine.Object.Destroy(purchases.gameObject);if(root)UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(config);
            }
        }
        public static string RestoreModel()
        {
            var config=ScriptableObject.CreateInstance<AdRemovalSettings>();config.productId="restore_auto";config.lifetimeAutomaticOnlyApproved=true;
            try {using(AdRemovalPolicy.UseTemporarySettings(config)) {
                AdRemovalPolicy.RestoreProducts(new[]{"other"});Require(!AdRemovalPolicy.Owned,"unrelated purchase");
                AdRemovalPolicy.RestoreProducts(new[]{"restore_auto"});Require(AdRemovalPolicy.Owned,"all purchases restore entitlement");
                AdRemovalPolicy.RestoreProducts(new[]{"restore_auto"});Require(AdRemovalPolicy.Owned,"idempotent restore");
            }}finally{UnityEngine.Object.DestroyImmediate(config);}
            return "PASS: restore filters configured product, idempotent, temporary scope released";
        }
        private static void Require(bool ok,string reason){if(!ok)throw new Exception(reason);}
    }
}
