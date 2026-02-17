using System;
using UnityEngine;
using MirraGames.SDK;
using MirraGames.SDK.Common;

public class MirraSDKPurchaseProvider : PurchasesProvider
{
    private bool isInitialized = false;

    public override void Initialize()
    {
        MirraSDK.WaitForProviders(() =>
        {
            isInitialized = true;
        });
    }

    public override void BuyPurchase(string purchaseId, Action<bool> onComplete)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("MirraSDK: Payments not initialized");
            onComplete?.Invoke(false);
            return;
        }

        MirraSDK.Payments.Purchase(
            purchaseId,
            onSuccess: () =>
            {
                Debug.Log($"MirraSDK: Purchase successful: {purchaseId}");
                onComplete?.Invoke(true);
            },
            onError: () =>
            {
                Debug.LogWarning($"MirraSDK: Purchase failed or closed: {purchaseId}");
                onComplete?.Invoke(false);
            }
        );

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
            foreach (var id in restoreData.PendingProducts)
            {
                restoreData.RestoreProduct(id, onProductRestore: () => {
                    GemsShop.Instance.OnPurchaseRestore(id);
                    Debug.Log($"Product '{id}' restored");
                });
            }
        });

        Debug.Log("MirraSDK: Restoring pending purchases");
    }

    public override PurchaseData GetPurchaseData(string purchaseId)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("MirraSDK: Payments not initialized");
            return null;
        }

        ProductData data = MirraSDK.Payments.GetProductData(purchaseId);
        if (data == null)
        {
            Debug.LogError($"MirraSDK: No product data for ID '{purchaseId}'");
            return null;
        }

        return new PurchaseData(
            data.Tag,
            "",
            "",
            data.PriceInteger.ToString(),
            data.Currency
        );
    }

    public override bool PurchasesAvailable()
    {
        if (!isInitialized || !MirraSDK.IsInitialized)
            return false;

        try
        {
            return MirraSDK.Platform.Current == PlatformType.YandexGames;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"MirraSDK: failed to resolve purchases availability ({exception.Message})");
            return false;
        }
    }

    private void OnDestroy()
    {
    }
}
