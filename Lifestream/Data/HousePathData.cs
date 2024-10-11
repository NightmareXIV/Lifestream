using Lifestream.Enums;

namespace Lifestream.Data;
[Serializable]
public class HousePathData
{
    public ResidentialAetheryteKind ResidentialDistrict;
    public int Ward;
    public int Plot;
    public List<Vector3> PathToEntrance = [];
    public List<Vector3> PathToWorkshop = [];
    public bool IsPrivate;
    public ulong CID;
    public bool EnableHouseEnterModeOverride = false;
    public HouseEnterMode EnterModeOverride = HouseEnterMode.None;
}
