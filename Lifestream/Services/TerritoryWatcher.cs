using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.LazyDataHelpers;

namespace Lifestream.Services;
public static class TerritoryWatcher
{
    public static uint LastHousingOutdoorTerritory = 0;
    public static void Initialize()
    {
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        if(Player.Available)
        {
            ClientState_TerritoryChanged(Svc.ClientState.TerritoryType);
            if(Utils.IsInsideHouse() || Utils.IsInsideWorkshop() || Utils.IsInsidePrivateChambers())
            {
                DuoLog.Warning($"Lifestream was loaded or updated while being inside house. Please re-enter house to ensure data reliability.");
            }
        }
        Purgatory.Add(() =>
        {
            Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        });
    }

    public static bool IsDataReliable() => LastHousingOutdoorTerritory != 0;

    private static void ClientState_TerritoryChanged(ushort obj)
    {
        if(Utils.IsTerritoryResidentialDistrict(obj))
        {
            LastHousingOutdoorTerritory = obj;
            PluginLog.Debug($"Last residential territory: {ExcelTerritoryHelper.GetName(obj)}");
        }
    }

    public static ushort GetRealTerritoryType()
    {
        if(Svc.ClientState.TerritoryType.EqualsAny<ushort>(Houses.Private_Cottage_Empyreum, Houses.Private_Cottage_Mist, Houses.Private_Cottage_Shirogane, Houses.Private_Cottage_The_Goblet, Houses.Private_Cottage_The_Lavender_Beds, 1249))
        {
            return LastHousingOutdoorTerritory switch
            {
                ResidentalAreas.Mist => Houses.Private_Cottage_Mist,
                ResidentalAreas.The_Lavender_Beds => Houses.Private_Cottage_The_Lavender_Beds,
                ResidentalAreas.The_Goblet => Houses.Private_Cottage_The_Goblet,
                ResidentalAreas.Shirogane => Houses.Private_Cottage_Shirogane,
                ResidentalAreas.Empyreum => Houses.Private_Cottage_Empyreum,
                _ => Svc.ClientState.TerritoryType
            };
        }
        if(Svc.ClientState.TerritoryType.EqualsAny<ushort>(Houses.Private_House_Empyreum, Houses.Private_House_Mist, Houses.Private_House_Shirogane, Houses.Private_House_The_Goblet, Houses.Private_House_The_Lavender_Beds, 1250))
        {
            return LastHousingOutdoorTerritory switch
            {
                ResidentalAreas.Mist => Houses.Private_House_Mist,
                ResidentalAreas.The_Lavender_Beds => Houses.Private_House_The_Lavender_Beds,
                ResidentalAreas.The_Goblet => Houses.Private_House_The_Goblet,
                ResidentalAreas.Shirogane => Houses.Private_House_Shirogane,
                ResidentalAreas.Empyreum => Houses.Private_House_Empyreum,
                _ => Svc.ClientState.TerritoryType
            };
        }
        if(Svc.ClientState.TerritoryType.EqualsAny<ushort>(Houses.Private_Mansion_Empyreum, Houses.Private_Mansion_Mist, Houses.Private_Mansion_Shirogane, Houses.Private_Mansion_The_Goblet, Houses.Private_Mansion_The_Lavender_Beds, 1251))
        {
            return LastHousingOutdoorTerritory switch
            {
                ResidentalAreas.Mist => Houses.Private_Mansion_Mist,
                ResidentalAreas.The_Lavender_Beds => Houses.Private_Mansion_The_Lavender_Beds,
                ResidentalAreas.The_Goblet => Houses.Private_Mansion_The_Goblet,
                ResidentalAreas.Shirogane => Houses.Private_Mansion_Shirogane,
                ResidentalAreas.Empyreum => Houses.Private_Mansion_Empyreum,
                _ => Svc.ClientState.TerritoryType
            };
        }
        return Svc.ClientState.TerritoryType;
    }
}
