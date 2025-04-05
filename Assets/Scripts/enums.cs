public enum ItemType
{
    Any,
    Weapon,
    Heal,
    Fuel,
    Treasure,
    Trash
}


public enum CurrencyType
{
    Coins,
    Gems
}

public enum PlayerStat
{
    Health,
    Stamina
}


public enum ItemTag
{
    Fuel,
    Trash,
    Valuable,
    Weapon,
    Medicine,
    Dead
}
public enum Tag
{
    GravityPlatform,
    Sun,
    Decoration,
    Location,
    Item,
    Water,
    Inventory,
}
public enum Layer
{
    Default = 0,
    Ground= 6,
    Player = 7,
    Pickable = 8,
    SpawnLocations= 9,
    WorldCanvas = 10,
    Inventory = 11,
}


public enum LocalizationKeyType
{
    Settings,
    Item,
    Tag
}

public enum ItemSize
{
    Any,
    Small,
    Medium,
    Large
}


public enum ItemStatus
{
    Free,
    Grabbed,
    Attached,
    InInventory
}


public enum StoreType
{
    Items,
    Enemies
}