public static class InventoryExpansionText
{
    public static string Get(BoostUI part)
    {
        if (part == BoostUI.Useble) return EggFeatureLocalization.Text("UI/Inventory/Slots", "слотов", "slots");
        if (part == BoostUI.Discription) return EggFeatureLocalization.Text("UI/Inventory/ExpandDescription",
            "Увеличивает вместимость вашей сумки. Предел: 27 слотов.", "Increases your bag capacity. Limit: 27 slots.");
        return EggFeatureLocalization.Text("UI/Inventory/ExpandTitle", "Вместительная сумка", "Larger bag");
    }
}
