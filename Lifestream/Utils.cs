
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Memory;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.Interop;
using ECommons.MathHelpers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.GUI;
using Lifestream.Systems;
using Lifestream.Systems.Legacy;
using Lifestream.Tasks.CrossDC;
using Lumina.Excel.GeneratedSheets;
using NightmareUI;
using PInvoke;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CharaData = (string Name, ushort World);

namespace Lifestream;

internal static unsafe class Utils
{
    public static void ScreenToWorldSelector(string id, ref Vector3 point)
    {
        ref var isInWorldToScreen = ref Ref<bool>.Get($"{id}_screenToWorldSelector");
        if(isInWorldToScreen)
        {
            if(Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out var worldPos))
            {
                point = worldPos;
            }
            ImGui.BeginTooltip();
            ImGuiEx.Text($"Point: {point:F2}\nLeft-click to finish");
            ImGui.EndTooltip();
            if(IsKeyPressed(Keys.LButton))
            {
                isInWorldToScreen = false;
            }
        }
    }

    public static void BeginScreenToWorldSelection(string id, Vector3 point)
    {
        ref var isInWorldToScreen = ref Ref<bool>.Get($"{id}_screenToWorldSelector");
        if(Svc.GameGui.WorldToScreen(point, out var screenPos))
        {
            SetCursorTo((int)screenPos.X, (int)screenPos.Y);
        }
        isInWorldToScreen = true;
    }

    public static void ScreenToWorldSelector(string id, ref Vector2 point)
    {
        ref var isInWorldToScreen = ref Ref<bool>.Get($"{id}_screenToWorldSelector");
        if(isInWorldToScreen)
        {
            //PluginLog.Debug($"{ImGui.GetIO().MousePos}");
            if(Svc.GameGui.ScreenToWorld(ImGui.GetIO().MousePos, out var worldPos))
            {
                point = worldPos.ToVector2();
            }
            ImGui.BeginTooltip();
            ImGuiEx.Text($"Point: {point:F2}\nLeft-click to finish");
            ImGui.EndTooltip();
            if(IsKeyPressed(Keys.LButton))
            {
                isInWorldToScreen = false;
            }
        }
    }

    public static void BeginScreenToWorldSelection(string id, Vector2 point)
    {
        ref var isInWorldToScreen = ref Ref<bool>.Get($"{id}_screenToWorldSelector");
        if(Svc.GameGui.WorldToScreen(point.ToVector3(Player.Position.Y), out var screenPos))
        {
            SetCursorTo((int)screenPos.X, (int)screenPos.Y);
        }
        isInWorldToScreen = true;
    }

    public static void SetCursorTo(int x, int y)
    {
        for(var i = 0; i < 1000; i++)
        {
            if(WindowFunctions.TryFindGameWindow(out var hwnd))
            {
                var point = new POINT() { x = x, y = y };
                if(User32.ClientToScreen(hwnd, ref point))
                {
                    User32.SetCursorPos(point.x, point.y);
                }
                break;
            }
        }

    }

    public static void DrawVector2Selector(string id, ref Vector2 value)
    {
        ImGui.SetNextItemWidth(150f);
        ImGui.DragFloat2($"##vec{id}", ref value, 0.01f);
        ImGui.SameLine();
        if(ImGuiEx.IconButton(FontAwesomeIcon.MapPin, $"myPos{id}", enabled: Player.Interactable))
        {
            value = Player.Position.ToVector2();
        }
        ImGuiEx.Tooltip("To player positon");
        ImGui.SameLine();
        if(ImGuiEx.IconButton(FontAwesomeIcon.Crosshairs, $"target{id}", enabled: Svc.Targets.Target != null))
        {
            value = Svc.Targets.Target.Position.ToVector2();
        }
        ImGuiEx.Tooltip("To target positon");
        ImGui.SameLine();
        if(ImGuiEx.IconButton(FontAwesomeIcon.MousePointer, $"target{id}", enabled: Player.Interactable))
        {
            BeginScreenToWorldSelection(id, value);
        }
        ScreenToWorldSelector(id, ref value);
        ImGuiEx.Tooltip("Select with mouse");
    }

    public static void DrawVector3Selector(string id, ref Vector3 value)
    {
        ImGui.SetNextItemWidth(150f);
        ImGui.DragFloat3($"##vec{id}", ref value, 0.01f);
        ImGui.SameLine();
        if(ImGuiEx.IconButton(FontAwesomeIcon.MapPin, $"myPos{id}", enabled: Player.Interactable))
        {
            value = Player.Position;
        }
        ImGuiEx.Tooltip("To player positon");
        ImGui.SameLine();
        if(ImGuiEx.IconButton(FontAwesomeIcon.Crosshairs, $"target{id}", enabled: Svc.Targets.Target != null))
        {
            value = Svc.Targets.Target.Position;
        }
        ImGuiEx.Tooltip("To target positon");
        ImGui.SameLine();
        if(ImGuiEx.IconButton(FontAwesomeIcon.MousePointer, $"target{id}", enabled: Player.Interactable))
        {
            BeginScreenToWorldSelection(id, value);
        }
        ScreenToWorldSelector(id, ref value);
        ImGuiEx.Tooltip("Select with mouse");
    }

    public static IEnumerable<uint> GetAllRegisteredAethernetDestinations()
    {
        foreach(var x in P.DataStore.Aetherytes)
        {
            yield return x.Key.ID;
            foreach(var v in x.Value)
            {
                yield return v.ID;
            }
        }
    }

    public static WorldChangeAetheryte AdjustGateway(this WorldChangeAetheryte gateway)
    {
        if(!Svc.AetheryteList.Any(x => x.AetheryteId == (int)gateway))
        {
            foreach(var c in Enum.GetValues<WorldChangeAetheryte>())
            {
                if(Svc.AetheryteList.Any(x => x.AetheryteId == (int)c))
                {
                    DuoLog.Warning($"{gateway} is not unlocked, but {c} is, adjusting.");
                    gateway = c;
                    break;
                }
            }
        }
        return gateway;
    }

    public static bool IsAddonVisible(string name)
    {
        if(TryGetAddonByName<AtkUnitBase>(name, out var addon) && addon->IsVisible) return true;
        return false;
    }

    public static List<World> GetVisitableWorldsFrom(World source)
    {
        var ret = new List<World>();
        foreach(var x in ExcelWorldHelper.GetPublicWorlds(source.GetRegion()))
        {
            ret.Add(x);
        }
        foreach(var x in ExcelWorldHelper.GetPublicWorlds(ExcelWorldHelper.Region.OC))
        {
            if(!ret.Contains(x)) ret.Add(x);
        }
        return ret;
    }

    public static List<HousePathData> GetHousePathDatas()
    {
        if(P.DisableHousePathData) return [];
        return P.Config.HousePathDatas;
    }

    public static uint[] AethernetShards = [2000151, 2000153, 2000154, 2000155, 2000156, 2000157, 2003395, 2003396, 2003397, 2003398, 2003399, 2003400, 2003401, 2003402, 2003403, 2003404, 2003405, 2003406, 2003407, 2003408, 2003409, 2003995, 2003996, 2003997, 2003998, 2003999, 2004000, 2004968, 2004969, 2004970, 2004971, 2004972, 2004973, 2004974, 2004976, 2004977, 2004978, 2004979, 2004980, 2004981, 2004982, 2004983, 2004984, 2004985, 2004986, 2004987, 2004988, 2004989, 2007434, 2007435, 2007436, 2007437, 2007438, 2007439, 2007855, 2007856, 2007857, 2007858, 2007859, 2007860, 2007861, 2007862, 2007863, 2007864, 2007865, 2007866, 2007867, 2007868, 2007869, 2007870, 2009421, 2009432, 2009433, 2009562, 2009563, 2009564, 2009565, 2009615, 2009616, 2009617, 2009618, 2009713, 2009714, 2009715, 2009981, 2010135, 2011142, 2011162, 2011163, 2011241, 2011243, 2011373, 2011374, 2011384, 2011385, 2011386, 2011387, 2011388, 2011389, 2011573, 2011574, 2011575, 2011677, 2011678, 2011679, 2011680, 2011681, 2011682, 2011683, 2011684, 2011685, 2011686, 2011687, 2011688, 2011689, 2011690, 2011691, 2011692, 2012252, 2012253,];

    public static uint[] HousingAethernet = [MainCities.Limsa_Lominsa_Lower_Decks, MainCities.Uldah_Steps_of_Nald, MainCities.New_Gridania, MainCities.Foundation, MainCities.Kugane];

    public static HousePathData GetFCPathData() => Utils.GetHousePathDatas().FirstOrDefault(x => x.CID == Player.CID && !x.IsPrivate);
    public static HousePathData GetPrivatePathData() => Utils.GetHousePathDatas().FirstOrDefault(x => x.CID == Player.CID && x.IsPrivate);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="territory"></param>
    /// <param name="plot">Start with 0</param>
    /// <returns></returns>
    public static Vector3? GetPlotEntrance(uint territory, int plot)
    {
        if(P.ResidentialAethernet.HousingData.Data.TryGetValue(territory, out var data) && data.Count > plot && data[plot].Path.Count > 0) return data[plot].Path[^1];
        return null;
    }

    public static IGameObject GetNearestEntrance(out float Distance)
    {
        var currentDistance = float.MaxValue;
        IGameObject currentObject = null;

        foreach(var x in Svc.Objects)
        {
            if(x.IsTargetable && x.Name.ToString().EqualsIgnoreCaseAny([.. Lang.Entrance]))
            {
                var distance = Vector3.Distance(Svc.ClientState.LocalPlayer.Position, x.Position);
                if(distance < currentDistance)
                {
                    currentDistance = distance;
                    currentObject = x;
                }
            }
        }
        Distance = currentDistance;
        return currentObject;
    }

    public static void DisplayInfo(string s, bool? displayChat = null, bool? displayPopup = null)
    {
        if(displayChat ?? P.Config.DisplayChatTeleport) ChatPrinter.Green($"[Lifestream] {s}");
        if(displayPopup ?? P.Config.DisplayPopupNotifications) Notify.Info(s);
    }

    public static HouseEnterMode GetHouseEnterMode(this HousePathData data)
    {
        if(data != null && data.EnableHouseEnterModeOverride) return data.EnterModeOverride;
        return P.Config.HouseEnterMode;
    }

    public static bool IsBusy()
    {
        return P.TaskManager.IsBusy || P.followPath?.waypointsInternal.Count > 0;
    }

    public static bool TryGetWorldFromDataCenter(string s, out string world, out uint dataCenter)
    {
        foreach(var x in Svc.Data.GetExcelSheet<WorldDCGroupType>())
        {
            if(x.RowId == 0 || x.Name == "") continue;
            if(x.Name.ExtractText().StartsWith(s, StringComparison.OrdinalIgnoreCase))
            {
                var worlds = ExcelWorldHelper.GetPublicWorlds(x.RowId);
                if(worlds.Length > 0)
                {
                    world = worlds[Random.Shared.Next(worlds.Length)].Name;
                    dataCenter = x.RowId;
                    if(P.DataStore.Worlds.Contains(world) || P.DataStore.DCWorlds.Contains(world))
                    {
                        return true;
                    }
                }
            }
        }
        dataCenter = default;
        world = default;
        return false;
    }

    public static bool TryParseAddressBookEntry(string s, out AddressBookEntry entry, bool retry = false)
    {
        entry = null;
        {
            var regex = ReplaceAddressBookRegex(@"(%worlds)%delimiter(%city)%delimiter(W|ward)%shortDelimiter%numeric%delimiter(P|plot)%shortDelimiter%numeric");
            PluginLog.Debug($"Testing vs: {regex}");
            var result = Regex.Match(s, regex, RegexOptions.IgnoreCase);
            if(result.Success)
            {
                PluginLog.Debug($"→Success: {result.Groups.Values.Select(x => x.Value).Skip(1).Print()}");
                entry = BuildAddressBookEntry(result.Groups[1].Value, result.Groups[2].Value, result.Groups[4].Value, result.Groups[6].Value, false, false);
                if(entry != null) return true;
            }
        }
        {
            var regex = ReplaceAddressBookRegex(@"(%worlds)%delimiter(%city)%delimiter(W|ward)%shortDelimiter%numeric%optDelimiter(s|sub|subdivision)%delimiter(A|apartment)%shortDelimiter%numeric");
            PluginLog.Debug($"Testing vs: {regex}");
            var result = Regex.Match(s, regex, RegexOptions.IgnoreCase);
            if(result.Success)
            {
                PluginLog.Debug($"→Success: {result.Groups.Values.Select(x => x.Value).Skip(1).Print()}");
                entry = BuildAddressBookEntry(result.Groups[1].Value, result.Groups[2].Value, result.Groups[4].Value, result.Groups[7].Value, true, true);
                if(entry != null) return true;
            }
        }
        {
            var regex = ReplaceAddressBookRegex(@"(%worlds)%delimiter(%city)%delimiter(W|ward)%shortDelimiter%numeric%delimiter(A|apartment)%shortDelimiter%numeric%optDelimiter(s|sub|subdivision)");
            PluginLog.Debug($"Testing vs: {regex}");
            var result = Regex.Match(s, regex, RegexOptions.IgnoreCase);
            if(result.Success)
            {
                PluginLog.Debug($"→Success: {result.Groups.Values.Select(x => x.Value).Skip(1).Print()}");
                entry = BuildAddressBookEntry(result.Groups[1].Value, result.Groups[2].Value, result.Groups[4].Value, result.Groups[6].Value, true, true);
                if(entry != null) return true;
            }
        }
        {
            var regex = ReplaceAddressBookRegex(@"(%worlds)%delimiter(%city)%delimiter(W|ward)%shortDelimiter%numeric%delimiter(A|apartment)%shortDelimiter%numeric");
            PluginLog.Debug($"Testing vs: {regex}");
            var result = Regex.Match(s, regex, RegexOptions.IgnoreCase);
            if(result.Success)
            {
                PluginLog.Debug($"→Success: {result.Groups.Values.Select(x => x.Value).Skip(1).Print()}");
                entry = BuildAddressBookEntry(result.Groups[1].Value, result.Groups[2].Value, result.Groups[4].Value, result.Groups[6].Value, true, false);
                if(entry != null) return true;
            }
        }
        {
            var regex = ReplaceAddressBookRegex(@"(%worlds)%delimiter(%city)%delimiter(W|ward|)%shortDelimiter%numeric%delimiter(P|plot|)%shortDelimiter%numeric");
            PluginLog.Debug($"Testing vs: {regex}");
            var result = Regex.Match(s, regex, RegexOptions.IgnoreCase);
            if(result.Success)
            {
                PluginLog.Debug($"→Success: {result.Groups.Values.Select(x => x.Value).Skip(1).Print()}");
                entry = BuildAddressBookEntry(result.Groups[1].Value, result.Groups[2].Value, result.Groups[4].Value, result.Groups[6].Value, false, false);
                if(entry != null) return true;
            }
        }
        if(!retry)
        {
            if(TryParseAddressBookEntry(Player.CurrentWorld + ", " + s, out entry, true))
            {
                return entry != null;
            }
        }
        return entry != null;
    }

    public static string ReplaceAddressBookRegex(string str)
    {
        var cities = "goblet|the goblet|lavender beds|the lavender beds|lb|empy|empyreum|shiro|shirogane|mist";
        var worlds = ExcelWorldHelper.GetPublicWorlds().Select(x => x.Name.ToString()).Join("|") + "|[a-z]{3,30}";
        return str.Replace("%worlds", worlds)
            .Replace("%delimiter", @"[\s\.\,\-\(\)\t]{1,10}")
            .Replace("%optDelimiter", @"[\s\.\,\-\(\)\t]{0,10}")
            .Replace("%city", cities)
            .Replace("%shortDelimiter", @"[\s\.\-\t]{0,3}")
            .Replace("%numeric", "([0-9]{1,2})");
    }

    public static AddressBookEntry BuildAddressBookEntry(string worldStr, string cityStr, string wardNum, string plotApartmentNum, bool isApartment, bool isSubdivision)
    {
        var world = ExcelWorldHelper.Get(worldStr, true);
        if(world == null)
        {
            foreach(var x in ExcelWorldHelper.GetPublicWorlds())
            {
                if(x.Name.ToString().StartsWith(worldStr, StringComparison.OrdinalIgnoreCase))
                {
                    world = x;
                    break;
                }
            }
        }
        if(world == null) return null;
        var city = ParseResidentialAetheryteKind(cityStr);
        if(city == null) return null;
        if(int.TryParse(wardNum, out var ward) && int.TryParse(plotApartmentNum, out var plot))
        {
            var entry = new AddressBookEntry()
            {
                City = city.Value,
                World = (int)world.RowId,
                PropertyType = isApartment ? PropertyType.Apartment : PropertyType.House,
                Ward = ward,
                Apartment = plot,
                Plot = plot,
                ApartmentSubdivision = isSubdivision,
            };
            return entry;
        }
        return null;
    }

    public static ResidentialAetheryteKind? ParseResidentialAetheryteKind(string s)
    {
        if(s.ContainsAny(StringComparison.OrdinalIgnoreCase, "mist"))
        {
            return ResidentialAetheryteKind.Limsa;
        }
        if(s.ContainsAny(StringComparison.OrdinalIgnoreCase, "goblet"))
        {
            return ResidentialAetheryteKind.Uldah;
        }
        if(s.ContainsAny(StringComparison.OrdinalIgnoreCase, "empy"))
        {
            return ResidentialAetheryteKind.Foundation;
        }
        if(s.ContainsAny(StringComparison.OrdinalIgnoreCase, "shiro"))
        {
            return ResidentialAetheryteKind.Kugane;
        }
        if(s.ContainsAny(StringComparison.OrdinalIgnoreCase, "lavender", "beds", "lb"))
        {
            return ResidentialAetheryteKind.Gridania;
        }
        return null;
    }

    public static bool IsTeleporterInstalled()
    {
        return Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "TeleporterPlugin" && x.IsLoaded);
    }

    public static bool IsHere(this AddressBookEntry entry)
    {
        var h = HousingManager.Instance();
        if(h == null) return false;
        if(h->GetCurrentWard() != entry.Ward - 1) return false;
        if(Utils.GetResidentialAetheryteByTerritoryType(Svc.ClientState.TerritoryType) != entry.City) return false;
        if(entry.PropertyType == PropertyType.House)
        {
            return h->GetCurrentPlot() == entry.Plot - 1;
        }
        if(entry.PropertyType == PropertyType.Apartment)
        {
            if(entry.ApartmentSubdivision && h->GetCurrentDivision() != 2) return false;
            return entry.Apartment == h->GetCurrentRoom();
        }
        return false;
    }

    public static bool IsQuickTravelAvailable(this AddressBookEntry entry)
    {
        if(P.ResidentialAethernet.HousingData.Data.SafeSelect(entry.City.GetResidentialTerritory())?.SafeSelect(entry.Ward - 1)?.AethernetID.EqualsAny(ResidentialAethernet.StartingAetherytes) != false) return false;
        var h = HousingManager.Instance();
        return h != null && entry.City.GetResidentialTerritory() == Svc.ClientState.TerritoryType && Player.Available && h->GetCurrentWard() == entry.Ward - 1 && P.ResidentialAethernet.ActiveAetheryte != null && entry.World == Player.Object.CurrentWorld.Id;
    }

    public static void GoTo(this AddressBookEntry entry)
    {
        if(!Player.Available)
        {
            Notify.Error($"Can not travel while character is not available");
            return;
        }
        if(!P.DataStore.DCWorlds.Contains(ExcelWorldHelper.GetName(entry.World)) && !P.DataStore.Worlds.Contains(ExcelWorldHelper.GetName(entry.World)))
        {
            Notify.Error($"Can not travel to {ExcelWorldHelper.GetName(entry.World)}");
            return;
        }
        if(entry.IsQuickTravelAvailable())
        {
            if(entry.PropertyType == PropertyType.House)
            {
                TaskTpAndGoToWard.EnqueueFromResidentialAetheryte(entry.City, entry.Plot - 1, false, default, false);
            }
            else if(entry.PropertyType == PropertyType.Apartment)
            {
                TaskTpAndGoToWard.EnqueueFromResidentialAetheryte(entry.City, entry.Apartment - 1, true, entry.ApartmentSubdivision, false);
            }
        }
        else
        {
            if(entry.PropertyType == PropertyType.House)
            {
                TaskTpAndGoToWard.Enqueue(ExcelWorldHelper.GetName(entry.World), entry.City, entry.Ward, entry.Plot - 1, false, default);
            }
            else if(entry.PropertyType == PropertyType.Apartment)
            {
                TaskTpAndGoToWard.Enqueue(ExcelWorldHelper.GetName(entry.World), entry.City, entry.Ward, entry.Apartment - 1, true, entry.ApartmentSubdivision);
            }
        }
    }

    public static string FancyDigits(this int n)
    {
        return n.ToString().ReplaceByChar(Lang.Digits.Normal, Lang.Digits.GameFont);
    }

    public static void SaveGeneratedHousingData()
    {
        EzConfig.SaveConfiguration(P.ResidentialAethernet.HousingData, "GeneratedHousingData.json", false);
    }

    public static float CalculatePathDistance(Vector3[] vectors)
    {
        var distance = 0f;
        for(var i = 0; i < vectors.Length - 1; i++)
        {
            distance += Vector3.Distance(vectors[i], vectors[i + 1]);
        }
        return distance;
    }

    public static bool? WaitForScreen() => IsScreenReady();

    public static bool ResidentialAetheryteEnumSelector(string name, ref ResidentialAetheryteKind refConfigField)
    {
        var ret = false;
        var names = TabAddressBook.ResidentialNames;
        if(ImGui.BeginCombo(name, names.SafeSelect(refConfigField) ?? $"{refConfigField}"))
        {
            var values = Enum.GetValues<ResidentialAetheryteKind>();
            foreach(var x in values)
            {
                var equals = x == refConfigField;
                if(x.RenderIcon(ImGui.CalcTextSize("A").Y)) ImGui.SameLine(0, 1);
                if(ImGui.Selectable(names.SafeSelect(x) ?? $"{x}", equals))
                {
                    ret = true;
                    refConfigField = x;
                }
                if(ImGui.IsWindowAppearing() && equals) ImGui.SetScrollHereY();
            }
            ImGui.EndCombo();
        }
        return ret;
    }

    public static string GetAutoName(this AddressBookEntry entry)
    {
        var builder = new StringBuilder();
        builder.Append(ExcelWorldHelper.GetName(entry.World));
        builder.Append(", ");
        builder.Append(TabAddressBook.ResidentialNames.SafeSelect(entry.City) ?? "???");
        builder.Append(", Ward ");
        builder.Append(entry.Ward);
        if(entry.PropertyType == PropertyType.House)
        {
            builder.Append(", Plot ");
            builder.Append(entry.Plot);
        }
        if(entry.PropertyType == PropertyType.Apartment)
        {
            builder.Append(", Apartment ");
            builder.Append(entry.Apartment);
            if(entry.ApartmentSubdivision)
            {
                builder.Append(" (subdivision)");
            }
        }
        return builder.ToString();
    }

    public static bool RenderIcon(this ResidentialAetheryteKind residentialAetheryte, float? size = null)
    {
        return NuiTools.RenderResidentialIcon(residentialAetheryte.GetResidentialTerritory(), size);
    }

    internal static void TryNotify(string s)
    {
        P.NotificationMasterApi.DisplayTrayNotification(P.Name, s);
    }

    internal static string GetDataCenterName(string world)
    {
        return GetDataCenter(world).Name.ToString();
    }

    internal static WorldDCGroupType GetDataCenter(string world)
    {
        return Svc.Data.GetExcelSheet<World>().First(x => x.Name == world).DataCenter.Value;
    }

    internal static int Minutes(this int min)
    {
        return min * 60 * 1000;
    }
    internal static int Seconds(this int sec)
    {
        return sec * 1000;
    }

    internal static bool CanAutoLogin()
    {
        return !Svc.ClientState.IsLoggedIn
            && !Svc.Condition.Any()
            && TryGetAddonByName<AtkUnitBase>("_TitleMenu", out var title)
            && IsAddonReady(title)
            && title->UldManager.NodeListCount > 3
            && title->UldManager.NodeList[3]->Color.A == 0xFF
            && !TryGetAddonByName<AtkUnitBase>("TitleDCWorldMap", out _)
            && !TryGetAddonByName<AtkUnitBase>("TitleConnect", out _);
    }

    internal static Dictionary<WorldChangeAetheryte, uint> WCATerritories = new()
    {
        [WorldChangeAetheryte.Uldah] = 130,
        [WorldChangeAetheryte.Limsa] = 129,
        [WorldChangeAetheryte.Gridania] = 132
    };

    internal static bool TryGetCharacterIndex(string name, uint world, out int index)
    {
        index = GetCharacterNames().IndexOf((name, (ushort)world));
        return index >= 0;
    }

    internal static List<CharaData> GetCharacterNames()
    {
        List<CharaData> ret = [];
        /*var data = CSFramework.Instance()->UIModule->GetRaptureAtkModule()->AtkModule.GetStringArrayData(1);
        if (data != null)
        {
            for (int i = 60; i < data->AtkArrayData.Size; i++)
            {
                if (data->StringArray[i] == null) break;
                var item = data->StringArray[i];
                if (item != null)
                {
                    var str = MemoryHelper.ReadSeStringNullTerminated((nint)item).ExtractText();
                    if (str == "") break;
                    ret.Add(str);
                }
            }
        }*/
        var agent = AgentLobby.Instance();
        //if (agent->AgentInterface.IsAgentActive())
        {
            var charaSpan = agent->LobbyData.CharaSelectEntries.AsSpan();
            for(var i = 0; i < charaSpan.Length; i++)
            {
                var s = charaSpan[i];
                ret.Add(($"{s.Value->Name.Read()}", s.Value->HomeWorldId));
            }
        }
        return ret;
    }

    internal static string GetInnNameFromTerritory(uint tt)
    {
        if(tt == 0) return "Autodetect";
        var t = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(tt);
        if(t != null)
        {
            var inn = Svc.Data.GetExcelSheet<TerritoryType>().FirstOrDefault(x => x.PlaceNameRegion.Value?.RowId == t.PlaceNameRegion.Value.RowId && x.TerritoryIntendedUse == (int)TerritoryIntendedUseEnum.Inn);
            if(inn != null)
            {
                return inn.PlaceNameZone.Value.Name;
            }
        }
        return "???";
    }

    internal static IGameObject GetReachableMasterAetheryte(bool littleDistance = false) => GetReachableAetheryte(x => Utils.TryGetTinyAetheryteFromIGameObject(x, out var ae) && ae.Value.IsAetheryte, littleDistance);

    internal static IGameObject GetReachableWorldChangeAetheryte(bool littleDistance = false) => GetReachableAetheryte(x => Utils.TryGetTinyAetheryteFromIGameObject(x, out var ae) && ae?.IsWorldChangeAetheryte() == true, littleDistance);

    internal static IGameObject GetReachableResidentialAetheryte(bool littleDistance = false) => GetReachableAetheryte(x => Utils.TryGetTinyAetheryteFromIGameObject(x, out var ae) && ae?.IsResidentialAetheryte() == true, littleDistance);

    internal static IGameObject GetReachableAetheryte(Predicate<IGameObject> predicate, bool littleDistance = false)
    {
        if(!Player.Available) return null;
        var a = Svc.Objects.OrderBy(x => Vector3.DistanceSquared(Player.Object.Position, x.Position)).FirstOrDefault(x => predicate(x));
        if(a != null && a.IsTargetable && Vector3.Distance(a.Position, Player.Object.Position) < (littleDistance ? 13f : 30f))
        {
            return a;
        }
        return null;
    }

    internal static bool IsDisallowedToChangeWorld()
    {
        return Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld]
            || Svc.Condition[ConditionFlag.ReadyingVisitOtherWorld]
            || Svc.Condition[ConditionFlag.InDutyQueue]
            || Svc.Condition[ConditionFlag.BoundByDuty95]
            || Svc.Party.Length > 0
            ;
    }

    internal static bool IsDisallowedToUseAethernet()
    {
        return Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld] || Svc.Condition[ConditionFlag.Jumping];
    }

    internal static string[] DefaultAddons = [
        "Inventory",
        "InventoryLarge",
        "AreaMap",
        "InventoryRetainerLarge",
        "Currency",
        "Bank",
        "RetainerTask",
        "RetainerList",
        "SelectYesNo",
        "SelectString",
        "SystemMenu",
        "MountNoteBook",
        "FriendList",
        "BlackList",
        "Character",
        "ItemSearch",
        "MonsterNote",
        "GatheringNote",
        "RecipeNote",
        "ContentsFinder",
        "LookingForGroup",
        "Journal",
        "ContentsInfo",
        "SocialList",
        "Macro",
        "ConfigKeybind",
        "GSInfoGeneral",
        "ContentsInfo",
        "PartyMemberList",
        "ContentsFinderConfirm",
        "SelectString"
    ];
    internal static bool IsAddonsVisible(IEnumerable<string> addons)
    {
        foreach(var x in addons)
        {
            if(TryGetAddonByName<AtkUnitBase>(x, out var a) && a->IsVisible) return true;
        }
        return false;
    }
    internal static bool IsMapActive()
    {
        if(TryGetAddonByName<AtkUnitBase>("AreaMap", out var map) && IsAddonReady(map) && map->IsVisible)
        {
            return true;
        }
        return false;
    }

    internal static AetheryteUseState CanUseAetheryte()
    {
        if(P.TaskManager.IsBusy || IsOccupied() || IsDisallowedToUseAethernet()) return AetheryteUseState.None;
        if(P.DataStore.Territories.Contains(P.Territory) && P.ActiveAetheryte != null) return AetheryteUseState.Normal;
        if(P.ResidentialAethernet.IsInResidentialZone() && P.ResidentialAethernet.ActiveAetheryte != null) return AetheryteUseState.Residential;
        return AetheryteUseState.None;
    }

    internal static TinyAetheryte GetMaster()
    {
        return P.ActiveAetheryte.Value.IsAetheryte ? P.ActiveAetheryte.Value : P.DataStore.GetMaster(P.ActiveAetheryte.Value);
    }

    internal static bool IsWorldChangeAetheryte(this TinyAetheryte t)
    {
        return t.ID.EqualsAny<uint>(2, 8, 9);
    }

    internal static bool IsResidentialAetheryte(this TinyAetheryte t)
    {
        return t.ID.EqualsAny<uint>(2, 8, 9, 70, 111);
    }

    private static Dictionary<ResidentialAetheryteKind, uint> TerritoryForResidentialAetheryte = new()
    {
        [ResidentialAetheryteKind.Uldah] = MainCities.Uldah_Steps_of_Nald,
        [ResidentialAetheryteKind.Gridania] = MainCities.New_Gridania,
        [ResidentialAetheryteKind.Limsa] = MainCities.Limsa_Lominsa_Lower_Decks,
        [ResidentialAetheryteKind.Kugane] = MainCities.Kugane,
        [ResidentialAetheryteKind.Foundation] = MainCities.Foundation,
    };
    private static Dictionary<ResidentialAetheryteKind, uint> ResidentialTerritoryForResidentialAetheryte = new()
    {
        [ResidentialAetheryteKind.Uldah] = ResidentalAreas.The_Goblet,
        [ResidentialAetheryteKind.Gridania] = ResidentalAreas.The_Lavender_Beds,
        [ResidentialAetheryteKind.Limsa] = ResidentalAreas.Mist,
        [ResidentialAetheryteKind.Kugane] = ResidentalAreas.Shirogane,
        [ResidentialAetheryteKind.Foundation] = ResidentalAreas.Empyreum,
    };
    private static Dictionary<WorldChangeAetheryte, uint> TerritoryForWorldChangeAetheryte = new()
    {
        [WorldChangeAetheryte.Uldah] = MainCities.Uldah_Steps_of_Nald,
        [WorldChangeAetheryte.Gridania] = MainCities.New_Gridania,
        [WorldChangeAetheryte.Limsa] = MainCities.Limsa_Lominsa_Lower_Decks,
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="r"></param>
    /// <returns>Aetheryte city (Limsa, Uldah, etc)</returns>
    internal static uint GetTerritory(this ResidentialAetheryteKind r)
    {
        return TerritoryForResidentialAetheryte[r];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="r"></param>
    /// <returns>Residential TerritoryType id (mist, goblet, etc)</returns>
    internal static uint GetResidentialTerritory(this ResidentialAetheryteKind r)
    {
        return ResidentialTerritoryForResidentialAetheryte[r];
    }
    internal static string GetName(this ResidentialAetheryteKind r)
    {
        return ExcelTerritoryHelper.Get(r.GetResidentialTerritory()).PlaceName.Value?.Name;
    }

    internal static uint GetTerritory(this WorldChangeAetheryte r)
    {
        return TerritoryForWorldChangeAetheryte[r];
    }

    internal static ResidentialAetheryteKind? GetResidentialAetheryteByTerritoryType(uint territoryType)
    {
        var t = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(territoryType);
        if(t == null) return null;
        if(t.PlaceNameRegion.Row == 2402) return ResidentialAetheryteKind.Kugane;
        if(t.PlaceNameRegion.Row == 25) return ResidentialAetheryteKind.Foundation;
        if(t.PlaceNameRegion.Row == 23) return ResidentialAetheryteKind.Gridania;
        if(t.PlaceNameRegion.Row == 24) return ResidentialAetheryteKind.Uldah;
        if(t.PlaceNameRegion.Row == 22) return ResidentialAetheryteKind.Limsa;
        return null;
    }

    internal static WorldChangeAetheryte? GetWorldChangeAetheryteByTerritoryType(uint territoryType)
    {
        var c = TerritoryForWorldChangeAetheryte.FindKeysByValue(territoryType);
        return c.Any() ? c.First() : null;
    }

    internal static bool TryGetTinyAetheryteFromIGameObject(IGameObject a, out TinyAetheryte? t, uint? TerritoryType = null)
    {
        TerritoryType ??= Svc.ClientState.TerritoryType;
        if(a == null)
        {
            t = default;
            return false;
        }
        if(a.ObjectKind == ObjectKind.Aetheryte)
        {
            var pos2 = a.Position.ToVector2();
            foreach(var x in P.DataStore.Aetherytes)
            {
                if(x.Key.TerritoryType == TerritoryType && Vector2.Distance(x.Key.Position, pos2) < 10)
                {
                    t = x.Key;
                    return true;
                }
                foreach(var l in x.Value)
                {
                    if(l.TerritoryType == TerritoryType && Vector2.Distance(l.Position, pos2) < 10)
                    {
                        t = l;
                        return true;
                    }
                }
            }
        }
        t = default;
        return false;
    }

    internal static float ConvertMapMarkerToMapCoordinate(int pos, float scale)
    {
        var num = scale / 100f;
        var rawPosition = (int)((float)(pos - 1024.0) / num * 1000f);
        return ConvertRawPositionToMapCoordinate(rawPosition, scale);
    }

    internal static float ConvertMapMarkerToRawPosition(int pos, float scale)
    {
        var num = scale / 100f;
        var rawPosition = ((float)(pos - 1024.0) / num);
        return rawPosition;
    }

    internal static float ConvertRawPositionToMapCoordinate(int pos, float scale)
    {
        var num = scale / 100f;
        return (float)((pos / 1000f * num + 1024.0) / 2048.0 * 41.0 / num + 1.0);
    }

    internal static AtkUnitBase* GetSpecificYesno(params string[] s) => GetSpecificYesno(false, s);

    internal static AtkUnitBase* GetSpecificYesno(bool contains, params string[] s)
    {
        for(var i = 1; i < 100; i++)
        {
            try
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", i);
                if(addon == null) return null;
                if(IsAddonReady(addon))
                {
                    var textNode = addon->UldManager.NodeList[15]->GetAsAtkTextNode();
                    var text = MemoryHelper.ReadSeString(&textNode->NodeText).ExtractText().Replace(" ", "");
                    if(contains ?
                        text.ContainsAny(s.Select(x => x.Replace(" ", "")))
                        : text.EqualsAny(s.Select(x => x.Replace(" ", "")))
                        )
                    {
                        PluginLog.Verbose($"SelectYesno {s.Print()} addon {i}");
                        return addon;
                    }
                }
            }
            catch(Exception e)
            {
                e.Log();
                return null;
            }
        }
        return null;
    }

    internal static string[] GetAvailableWorldDestinations()
    {
        if(TryGetAddonByName<AtkUnitBase>("WorldTravelSelect", out var addon) && IsAddonReady(addon))
        {
            List<string> arr = [];
            for(var i = 3; i <= 9; i++)
            {
                var item = addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];
                var text = MemoryHelper.ReadSeString(&item->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode()->NodeText).ExtractText();
                if(text == "") break;
                arr.Add(text);
            }
            return [.. arr];
        }
        return Array.Empty<string>();
    }

    internal static string[] GetAvailableAethernetDestinations()
    {
        if(TryGetAddonByName<AtkUnitBase>("TelepotTown", out var addon) && IsAddonReady(addon))
        {
            List<string> arr = [];
            for(var i = 1; i <= 52; i++)
            {
                var item = addon->UldManager.NodeList[16]->GetAsAtkComponentNode()->Component->UldManager.NodeList[i];
                var text = MemoryHelper.ReadSeString(&item->GetAsAtkComponentNode()->Component->UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText).ExtractText().Trim();
                if(text == "") break;
                arr.Add(text);
            }
            return [.. arr];
        }
        return Array.Empty<string>();
    }

    internal static IGameObject GetValidAetheryte()
    {
        foreach(var x in Svc.Objects)
        {
            if(x.IsAetheryte())
            {
                var d2d = Vector2.Distance(Svc.ClientState.LocalPlayer.Position.ToVector2(), x.Position.ToVector2());
                var d3d = Vector3.Distance(Svc.ClientState.LocalPlayer.Position, x.Position);
                if(P.ResidentialAethernet.IsInResidentialZone() && d3d > 4.6f) continue;

                if(d2d < 11f
                    && d3d < 15f
                    && x.IsVPosValid()
                    && x.IsTargetable)
                {
                    return x;
                }
            }
        }
        return null;
    }

    public static bool IsAetheryte(this IGameObject obj)
    {
        if(obj.ObjectKind == ObjectKind.Aetheryte) return true;
        return Utils.AethernetShards.Contains(obj.DataId);
    }

    internal static bool IsVPosValid(this IGameObject x)
    {
        /*if(x.Name.ToString() == Lang.AethernetShard)
        {
            return MathF.Abs(Player.Object.Position.Y - x.Position.Y) < 0.965;
        }*/
        return true;
    }

    internal static bool TrySelectSpecificEntry(string text, Func<bool> Throttle)
    {
        return TrySelectSpecificEntry(new string[] { text }, Throttle);
    }

    internal static bool TrySelectSpecificEntry(IEnumerable<string> text, Func<bool> Throttle)
    {
        if(TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && IsAddonReady(&addon->AtkUnitBase))
        {
            var entry = GetEntries(addon).FirstOrDefault(x => x.EqualsAny(text));
            if(entry != null)
            {
                var index = GetEntries(addon).IndexOf(entry);
                if(index >= 0 && IsSelectItemEnabled(addon, index) && Throttle())
                {
                    new SelectStringMaster(addon).Entries[index].Select();
                    PluginLog.Debug($"TrySelectSpecificEntry: selecting {entry}/{index} as requested by {text.Print()}");
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool IsSelectItemEnabled(AddonSelectString* addon, int index)
    {
        var step1 = (AtkTextNode*)addon->AtkUnitBase
                    .UldManager.NodeList[2]
                    ->GetComponent()->UldManager.NodeList[index + 1]
                    ->GetComponent()->UldManager.NodeList[3];
        return GenericHelpers.IsSelectItemEnabled(step1);
    }

    internal static List<string> GetEntries(AddonSelectString* addon)
    {
        var list = new List<string>();
        for(var i = 0; i < addon->PopupMenu.PopupMenu.EntryCount; i++)
        {
            list.Add(MemoryHelper.ReadSeStringNullTerminated((nint)addon->PopupMenu.PopupMenu.EntryNames[i]).ExtractText().Trim());
        }
        //PluginLog.Debug($"{list.Print()}");
        return list;
    }

    internal static int GetServiceAccount(string name, uint world) => GetServiceAccount($"{name}@{ExcelWorldHelper.GetName(world)}");

    internal static int GetServiceAccount(string nameWithWorld)
    {
        if(P.AutoRetainerApi?.Ready == true && P.Config.UseAutoRetainerAccounts)
        {
            var chars = P.AutoRetainerApi.GetRegisteredCharacters();
            foreach(var c in chars)
            {
                var data = P.AutoRetainerApi.GetOfflineCharacterData(c);
                if(data != null)
                {
                    var name = $"{data.Name}@{data.World}";
                    if(nameWithWorld == name && data.ServiceAccount > -1)
                    {
                        return data.ServiceAccount;
                    }
                }
            }
        }
        if(P.Config.ServiceAccounts.TryGetValue(nameWithWorld, out var ret))
        {
            if(ret > -1) return ret;
        }
        return 0;
    }
}
