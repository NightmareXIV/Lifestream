using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Lifestream.AtkReaders;
public unsafe class ReaderLobbyDKTWorldList(AtkUnitBase* UnitBase, int BeginOffset = 0) : AtkReader(UnitBase, BeginOffset)
{
    public string Source => ReadString(3);
    public string Destination => ReadString(4);
    public List<RegionInfo> Regions => Loop<RegionInfo>(8, 2 + 8 * 4, 4);
    public string SelectedDataCenter => ReadString(145);

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
}
