using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal unsafe class ReaderTelepotTown(AtkUnitBase* UnitBase, int BeginOffset = 0) : AtkReader(UnitBase, BeginOffset)
    {
        internal uint NumEntries => ReadUInt(0) ?? 0;
        internal uint CurrentDestination => ReadUInt(1) ?? 0;
        internal List<Data> DestinationData => Loop<Data>(3, 4, 20);
        internal List<Names> DestinationName => Loop<Names>(259, 1, 20);

        internal unsafe class Names(nint UnitBasePtr, int BeginOffset = 0) : AtkReader(UnitBasePtr, BeginOffset)
        {
            internal string Name => ReadString(0);
        }

        internal unsafe class Data(nint UnitBasePtr, int BeginOffset = 0) : AtkReader(UnitBasePtr, BeginOffset)
        {
            internal uint Type => ReadUInt(0).Value;
            internal uint State => ReadUInt(1).Value;
            internal uint IconID => ReadUInt(2).Value;
            internal uint CallbackData => ReadUInt(3).Value;
        }
    }
}
