using System;
using UnityEngine;
using MirraGames.SDK;  // пространство имён SDK

//#if MIRRA_SDK_ENABLED
public class MirraSDKMetricaProvider : PurchasesProvider
{
    private bool isInitialized = false;
    private Action<bool> currentCallback;

    public override void Initialize()
    {
        MirraSDK.Payments.WaitForProviders(() =>
        {
            isInitialized = true;
            Debug.Log("MirraSDK: Payments initialized");
        });  // :contentReference[oaicite:0]{index=0}
    }

    public override void BuyPurchase(string purchaseId, Action<bool> onComplete)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("MirraSDK: Payments not initialized");
            onComplete?.Invoke(false);
            return;
        }

        currentCallback = onComplete;
        MirraSDK.Payments.Purchase(
            purchaseId,
            onSuccess: () =>
            {
                Debug.Log($"MirraSDK: Purchase successful: {purchaseId}");
                //Shop.Instance.OnRestorePurchases(purchaseId);
                currentCallback?.Invoke(true);
                currentCallback = null;
            },
            onError: () =>
            {
                Debug.LogWarning($"MirraSDK: Purchase failed or closed: {purchaseId}");
                currentCallback?.Invoke(false);
                currentCallback = null;
            }
        );  // :contentReference[oaicite:1]{index=1}

        Debug.Log($"MirraSDK: Purchase requested: {purchaseId}");
    }

    public override void ConsumePendingPurchases()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("MirraSDK: Payments not initialized");
            return;
        }

        MirraSDK.Payments.RestorePurchases((restoreData) =>
        {
            Debug.Log($"MirraSDK: Restored purchases: {string.Join(", ", restoreData.AllPurchases)}");
            Debug.Log($"MirraSDK: Pending products: {string.Join(", ", restoreData.PendingProducts)}");

            foreach (var id in restoreData.PendingProducts)
            {
                // Delegate Method: SupplyProduct(string, Action onSuccess, bool incrementSupply)
                MirraSDK.Payments.SupplyProduct(
                    id,
                    () =>
                    {
                        Debug.Log($"MirraSDK: Supplied product: {id}");
                        //Shop.Instance.OnRestorePurchases(id);
                    },
                    true
                );  // :contentReference[oaicite:2]{index=2}
            }
        });  // :contentReference[oaicite:3]{index=3}

        Debug.Log("MirraSDK: Restoring pending purchases");
    }

    public override PurchaseData GetPurchaseData(string purchaseId)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("MirraSDK: Payments not initialized");
            return null;
        }

        var data = MirraSDK.Payments.GetProductData(purchaseId);
        if (data == null)
        {
            Debug.LogError($"MirraSDK: No product data for ID «{purchaseId}»");
            return null;
        }

        return new PurchaseData(
            data.Tag,
            "",
            "",
            data.PriceInteger.ToString(),
            ""
        );  // :contentReference[oaicite:4]{index=4}
    }

    private void OnDestroy()
    {
        // Никаких глобальных событий не подписывали, всё в делегатах.
    }
}
//#endif
