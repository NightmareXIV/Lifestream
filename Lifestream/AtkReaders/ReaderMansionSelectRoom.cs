using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Lifestream.AtkReaders;
public unsafe class ReaderMansionSelectRoom(AtkUnitBase* UnitBase, int BeginOffset = 0) : AtkReader(UnitBase, BeginOffset)
{
    public uint LoadStatus => ReadUInt(0) ?? 0;
    public bool IsLoaded => LoadStatus == 4;
    public int Section => ReadInt(1) ?? -1;
    public uint ExistingSectionsCount => ReadUInt(5) ?? 0;
    public uint SectionRoomsCount => ReadUInt(41) ?? 0;
    public List<RoomInfo> Rooms => Loop<RoomInfo>(42, 12, 15);

    public class RoomInfo(nint UnitBasePtr, int BeginOffset = 0) : AtkReader(UnitBasePtr, BeginOffset)
    {
        public uint AccessState => ReadUInt(0) ?? 0;
        public int IconID => ReadInt(1) ?? 0;

        public string RoomNumber => ReadString(3);
        public string Owner => ReadString(4);
    }
}
