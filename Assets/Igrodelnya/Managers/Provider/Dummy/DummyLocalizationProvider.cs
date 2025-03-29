using System;
using UnityEngine;

public class DummyLocalizationProvider : LocalizationProvider
{
    public override event Action<string> OnSwitchLang;

    private string currentLanguage = "English";

    public override string GetCurrentLanguage()
    {
        return currentLanguage;
    }

    public override void SwitchLanguage(string langCode)
    {
        currentLanguage = langCode;
        OnSwitchLang?.Invoke(langCode);
        Debug.Log($"DummyLocalizationProvider: язык переключен на {langCode}");
    }
}
