
//#if MIRRA_SDK_ENABLED
using System;
using UnityEngine;
using MirraGames.SDK;  // пространство имён MirraSDK
using MirraGames.SDK.Common;
using System.Globalization;

public class MirraSDKLocalizationProvider : LocalizationProvider
{
    // событие при смене языка, передаём код в lowercase, например "en", "ru"
    public override event Action<string> OnSwitchLang;

    // хранит последний известный язык
    private string lastLangCode = null;

    private void OnEnable()
    {
        // Ждём, пока провайдеры локализации не инициализируются
        MirraSDK.Language.WaitForProviders(() =>
        {
            // устанавливаем начальное значение
            lastLangCode = MirraSDK.Language.Current.ToString().ToLowerInvariant();
        });
    }

    private void Update()
    {
        // проверяем, что система локализации уже готова
        if (!MirraSDK.Language.IsInitialized) return;

        // получаем текущий язык
        string current = MirraSDK.Language.Current.ToString().ToLowerInvariant();

        // если изменилось — уведомляем подписчиков
        if (lastLangCode != null && current != lastLangCode)
        {
            lastLangCode = current;
            OnSwitchLang?.Invoke(current);
        }
    }

    // Возвращаем код текущего языка
    public override string GetCurrentLanguage()
    {
        return MirraSDK.Language.Current.ToString().ToLowerInvariant();
    }

    // Переключаем язык по коду (например "en", "ru", "ja")
    public override void SwitchLanguage(string langCode)
    {
        // Пробуем спарсить строку в enum LanguageType
        if (Enum.TryParse(
            /* текст */    CultureInfo.InvariantCulture.TextInfo.ToTitleCase(langCode),
            /* тип */      out LanguageType lang))
        {
            MirraSDK.Language.Current = lang;
        }
        else
        {
            Debug.LogWarning($"MirraSDKLocalizationProvider: неверный код языка «{langCode}»");
        }
    }

    private void OnDisable()
    {
        // очищаем, чтобы не было утечек
        lastLangCode = null;
    }
}
//#endif
