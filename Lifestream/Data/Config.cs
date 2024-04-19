using ECommons.Configuration;
using Lifestream.Enums;

namespace Lifestream.Data;

public class Config : IEzConfig
{
    public bool Enable = true;
    internal bool AllowClosingESC2 = false;
    public int ButtonWidth = 10;
    public int ButtonHeightAetheryte = 1;
    public int ButtonHeightWorld = 5;
    public bool FixedPosition = false;
    public Vector2 Offset = Vector2.Zero;
    public bool UseMapTeleport = true;
    public bool HideAddon = true;
    public BasePositionHorizontal PosHorizontal = BasePositionHorizontal.Middle;
    public BasePositionVertical PosVertical = BasePositionVertical.Middle;
    public bool ShowAethernet = true;
    public bool ShowWorldVisit = true;
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
    public bool ShowWards = true;
    public List<AddressBookFolder> AddressBookFolders = [];
}
