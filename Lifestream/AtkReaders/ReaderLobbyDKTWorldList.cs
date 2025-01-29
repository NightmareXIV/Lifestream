using ECommons.ExcelServices;
using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Lifestream.AtkReaders;
public unsafe class ReaderLobbyDKTWorldList(AtkUnitBase* UnitBase, int BeginOffset = 0) : AtkReader(UnitBase, BeginOffset)
{
    public string Source => ReadString(3);
    public string Destination => ReadString(4);
    public List<RegionInfo> Regions => Loop<RegionInfo>(14, 2 + 8 * 4, 4);
    public string SelectedDataCenter => ReadString(151);
    public List<WorldInfo> Worlds => Loop<WorldInfo>(154, 8, GetNumWorlds());

    public int GetNumWorlds()
    {
        var dc = ExcelWorldHelper.GetDataCenters().FirstOrNull(x => x.Name.GetText() == SelectedDataCenter);
        if(dc == null) return 0;
        var worlds = ExcelWorldHelper.GetPublicWorlds(dc.Value.RowId);
        return worlds.Count(x => x.IsPublic());
    }

    public unsafe class RegionInfo(nint UnitBasePtr, int BeginOffset = 0) : AtkReader(UnitBasePtr, BeginOffset)
    {
        private int DcInfoOffset = BeginOffset + 1;
        public string RegionTitle => ReadString(0);
        public List<DataCenterInfo> DataCenters => Loop<DataCenterInfo>(DcInfoOffset, 8, 4);

        public unsafe class DataCenterInfo(nint UnitBasePtr, int BeginOffset = 0) : AtkReader(UnitBasePtr, BeginOffset)
        {
            public uint Id => ReadUInt(0) ?? 0;
            public string Name => ReadString(1);
            public bool? Unk2 => ReadBool(2);
            public bool? Unk3 => ReadBool(3);
            public uint Unk4 => ReadUInt(4) ?? 0;
        }
    }

    public unsafe class WorldInfo(nint UnitBasePtr, int BeginOffset = 0) : AtkReader(UnitBasePtr, BeginOffset)
    {
        public string WorldName => ReadString(0);
        public bool IsAvailable => ReadUInt(6) == 0;
    }
}
