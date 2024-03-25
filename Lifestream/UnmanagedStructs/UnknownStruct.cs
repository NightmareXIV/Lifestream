using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.UnmanagedStructs
{
    [StructLayout(LayoutKind.Explicit, Size = 0x40)]
    internal unsafe struct UnknownStruct
    {
        [FieldOffset(4)] public byte unk_4;
        [FieldOffset(8)] public int SelectedItem;
    }
}
