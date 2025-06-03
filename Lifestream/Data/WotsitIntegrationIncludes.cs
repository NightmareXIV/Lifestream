namespace Lifestream.Data;
public class WotsitIntegrationIncludedItems
{
    public bool WorldSelect = true;
    public bool PropertyAuto = false;
    // The built-in Wotsit integration for Teleporter usually ranks higher
    // than our custom entries, so we disable these by default.
    public bool PropertyPrivate = false;
    public bool PropertyFreeCompany = false;
    public bool PropertyApartment = false;
    // Inn does routing so it's better than the built-in Wotsit integration.
    public bool PropertyInn = true;
    public bool GrandCompany = true;
    // MarketBoard does routing so it's better than the built-in Wotsit
    // integration.
    public bool MarketBoard = true;
    public bool IslandSanctuary = true;

    public bool AetheryteAethernet = true;
    public bool AddressBook = true;
    public bool CustomAlias = true;
}
