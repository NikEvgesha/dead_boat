public enum ItemType
{
    Any,
    Weapon,
    Heal,
    Fuel,
    Treasure,
    Trash,
    Ammo
}


public enum CurrencyType
{
    Coins,
    Gems,
    Real
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
    Dead,
    Ammo,
    Reward
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
    Zomby
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

public enum WeaponType 
{ 
    Pistol,
    Rifle,
    Shotgun 
}