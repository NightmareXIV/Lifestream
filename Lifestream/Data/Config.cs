using BadgeOverlay;
using ECommons.Configuration;
using Lifestream.Enums;
using Lifestream.Tasks.Shortcuts;
using NightmareUI.Censoring;

namespace Lifestream.Data;

public class Config
{
    public bool Enable = true;
    public bool EnableLegacy = false;
    public CensorConfig CensorConfig = new();
    internal bool AllowClosingESC2 = false;
    public int ButtonWidth = 10;
    public int[] ButtonWidthArray = null;
    public int ButtonHeightAetheryte = 1;
    public int ButtonHeightWorld = 5;
    public bool FixedPosition = false;
    public Vector2 Offset = Vector2.Zero;
    public bool UseMapTeleport = true;
    public bool HideAddon = true;
    public HashSet<string> HideAddonList = [.. Utils.DefaultAddons];
    public BasePositionHorizontal PosHorizontal = BasePositionHorizontal.Middle;
    public BasePositionVertical PosVertical = BasePositionVertical.Middle;
    public bool ShowAethernetBOM = true;
    public bool ShowCosmicBOM = true;
    public bool ShowWorldVisitBOM = true;
    public bool ShowAethernet = true;
    public bool ShowWorldVisit = true;
    public bool ShowWorldVisitCurrentDc = true;
    public bool ShowMiniAetheryteOverlay = true;
    public HashSet<uint> Favorites = [];
    public HashSet<uint> Hidden = [];
    public Dictionary<uint, string> Renames = [];
    public WorldChangeAetheryte WorldChangeAetheryte = WorldChangeAetheryte.Uldah;
    public bool Firmament = true;
    public bool WalkToAetheryte = true;
    public bool LeavePartyBeforeWorldChange = true;
    public bool AllowDcTransfer = true;
    public bool LeavePartyBeforeLogout = true;
    public bool TeleportToGatewayBeforeLogout = true;
    public bool NoProgressBar = false;
    public Dictionary<string, int> ServiceAccounts = [];
    public bool DCReturnToGateway = false;
    public bool WorldVisitTPToAethernet = false;
    public string WorldVisitTPTarget = "";
    public bool WorldVisitTPOnlyCmd = true;
    public bool UseAutoRetainerAccounts = true;
    public bool SlowTeleport = false;
    public int SlowTeleportThrottle = 0;
    public bool WaitForScreenReady = true;
    public bool ShowWardsBOM = true;
    public bool ShowWards = true;
    internal bool ShowPlots = false;
    public List<AddressBookFolder> AddressBookFolders = [];
    public bool AddressNoPathing = false;
    public bool AddressApartmentNoEntry = false;
    public bool SingleBookMode = false;
    public List<MultiPath> MultiPathes = [];
    public string GameVersion = "";
    public Dictionary<uint, int> PublicInstances = [];
    public bool ShowInstanceSwitcherBOM = true;
    public bool ShowInstanceSwitcher = true;
    public bool InstanceSwitcherRepeat = true;
    public int InstanceButtonHeight = 10;
    public bool UseSprintPeloton = true;
    public bool UsePeloton = true;
    public bool EnableFlydownInstance = true;
    public bool DisplayChatTeleport = false;
    public bool DisplayPopupNotifications = true;
    public List<HousePathData> HousePathDatas = [];
    public List<HousePathData> CustomHousePathDatas = [];
    public bool EnterMyApartment = true;
    public HouseEnterMode HouseEnterMode = HouseEnterMode.None;
    public bool UseReturn = true;
    public uint PreferredInn = 0;
    public List<AutoPropertyData> PropertyPrio = [new(true, TaskPropertyShortcut.PropertyType.Home), new(true, TaskPropertyShortcut.PropertyType.FC), new(true, TaskPropertyShortcut.PropertyType.Apartment), new(true, TaskPropertyShortcut.PropertyType.Inn), new(false, TaskPropertyShortcut.PropertyType.Shared_Estate)];
    public Dictionary<ulong, List<AutoPropertyData>> PropertyPrioOverrides = [];
    public bool EnableDvcRetry = true;
    public int MaxDcvRetries = 3000;
    public bool DcvUseAlternativeWorld = true;
    public int DcvRetryInterval = 30;
    public bool RetryWorldVisit = true;
    public int RetryWorldVisitInterval = 30;
    public int RetryWorldVisitIntervalDelta = 10;
    public List<CustomAlias> CustomAliases = [];
    public bool UseGuestWorldTravel = false;
    public bool AllowDCTravelFromCharaSelect = true;
    public List<TravelBanInfo> TravelBans = [];
    public bool TerminateSelfPartyFinder = false;
    public Dictionary<ulong, string> CharaMap = [];
    public bool UseMount = true;
    public int Mount = 0;
    public bool WotsitIntegrationEnabled = true;
    public WotsitIntegrationIncludedItems WotsitIntegrationIncludes = new();
    public bool EnableDtrBar = false;
    public Dictionary<ulong, (int Territory, int Ward, int Plot)> PreferredSharedEstates = [];
    public bool LeftAlignButtons = false;
    public int LeftAlignPadding = 0;
    public LiCommandBehavior LiCommandBehavior = LiCommandBehavior.Return_to_Home_World;
    public bool EnableNotifications = true;
    public bool ProgressOverlayToTop = false;
    public bool AllowCustomOverrides = false;
    public bool DisableMapClickOtherTerritory = false;
    public bool EnableAutoCompletion = false;
    public bool AutoCompletionFixedWindow = false;
    public bool AutoCompletionWindowBottom = false;
    public bool AutoCompletionWindowRight = false;
    public Vector2 AutoCompletionWindowOffset = Vector2.Zero;
    public bool AutoDismount = true;
    public BadgeBarStyle BadgeBarStyle = new BadgeBarStyle
    {
        Dock = BadgeDock.Left,
        Sizing = BadgeSizing.Uniform,
        Anchor = BadgeAnchor.Center,
        ScreenMargin = 0f,
        TextRotation = VerticalTextRotation.Degrees270,
        Spacing = 15f,
        PaddingStart = 40f,
        PaddingEnd = 40f,
        UniformLength = 200f,
        Rounding = 0f,
    };
    public OverlayOptions OverlayOptions = new OverlayOptions
    {
        HoverOutDelay = 2f,
        PopoutGap = 2f,
        DefaultSelectableTrigger = TriggerMode.Hover,
        AlwaysOnTop = false,
        PopoutRounding = 0f,
    };
    public float BadgeFontSize = 2f;
    public float PopoutFontSize = 1.5f;
    public TriggerMode BadgeTriggerMode = TriggerMode.Hover;
    public bool AethernetPinned = false;
    public bool InstancePinned = false;
    public bool CosmicPinned = false;
    public bool WorldPinned = false;
    public bool WardPinned = false;
    public OtherDCShowMode OtherDCShowMode = OtherDCShowMode.Show;
    public float OverlayMinWidth = 300f;
}

public enum OtherDCShowMode
{
    Show,
    Shown_on_Shift,
    Hide,
}