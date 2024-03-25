using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream;
public unsafe class ReaderLobbyDKTWorldList(AtkUnitBase* UnitBase, int BeginOffset = 0) : AtkReader(UnitBase, BeginOffset)
{
    public string Source => ReadString(3);
    public string Destination => ReadString(4);
    public List<RegionInfo> Regions => Loop<RegionInfo>(8, 2 + 8 * 4, 4);

    public unsafe class RegionInfo(nint UnitBasePtr, int BeginOffset = 0) : AtkReader(UnitBasePtr, BeginOffset)
    {
        public string RegionTitle => ReadString(0);
        public List<DataCenterInfo> DataCenters => Loop<DataCenterInfo>(BeginOffset + 1, 8, 4);

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
