using UnityEngine;
using System;

// Данные о покупке (универсальная структура)
public class PurchaseData
{
    public string Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Price { get; private set; }
    public string CurrencyImageURL { get; private set; }

    public PurchaseData(string id, string title, string description, string price,string currencyImageURL)
    {
        Id = id;
        Title = title;
        Description = description;
        Price = price;
        CurrencyImageURL = currencyImageURL;
    }
}

// Главный менеджер покупок
public class PurchasesManager : MonoBehaviour
{
    private static PurchasesManager _instance;
    public static PurchasesManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("PurchasesManager");
                _instance = go.AddComponent<PurchasesManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [SerializeField] private MonoBehaviour activeProvider; // Активный провайдер в инспекторе
    private PurchasesProvider provider;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            bool canReplaceAutoCreated =
                _instance.provider == null &&
                _instance.activeProvider == null &&
                activeProvider != null;

            if (canReplaceAutoCreated)
            {
                Destroy(_instance.gameObject);
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            _instance = this;
        }
        //DontDestroyOnLoad(gameObject);

        // Проверка и инициализация провайдера
        if (activeProvider == null || !activeProvider.TryGetComponent(out provider))
        {
            Debug.LogError("No valid purchases provider assigned!");
            return;
        }

        provider.Initialize();
    }


    public void RestorePurchases()
    {
        if (provider == null)
        {
            Debug.LogWarning("Purchases provider not initialized!");
            return;
        }

        provider.ConsumePendingPurchases();
    }

    // Вызов покупки
    public void BuyPurchase(string purchaseId, Action<bool> onComplete)
    {
        if (provider == null)
        {
            Debug.LogError("Purchases provider not initialized!");
            onComplete?.Invoke(false);
            return;
        }

        bool releaseCursorAfterPurchase = false;
        if (ControlManager.Instance != null && !ControlManager.Instance.UseTouchControl)
        {
            ControlManager.Instance.CursorActive = true;
            releaseCursorAfterPurchase = true;
        }

        provider.BuyPurchase(
            purchaseId,
            success =>
            {
                try
                {
                    onComplete?.Invoke(success);
                }
                finally
                {
                    if (releaseCursorAfterPurchase && ControlManager.Instance != null)
                        ControlManager.Instance.CursorActive = false;
                }
            });
    }

    // Получение данных о покупке
    public PurchaseData GetPurchaseData(string purchaseId)
    {
        if (provider == null)
        {
            Debug.LogError("Purchases provider not initialized!");
            return null;
        }

        return provider.GetPurchaseData(purchaseId);
    }

    // Установка нового провайдера в рантайме (опционально)
    public void SetProvider(PurchasesProvider newProvider)
    {
        if (newProvider == null)
        {
            Debug.LogError("Cannot set null purchases provider!");
            return;
        }

        provider = newProvider;
        provider.Initialize();
        provider.ConsumePendingPurchases();
    }

    public bool PurchasesAvailable()
    {
        return provider != null && provider.PurchasesAvailable();
    }
}
