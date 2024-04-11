using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.UnmanagedStructs;

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
internal unsafe struct InputData
{
    [FieldOffset(0)] internal fixed byte RawDump[0x40];
    [FieldOffset(8)] internal nint* unk_8;
    [FieldOffset(16)] internal int unk_16;
    [FieldOffset(24)] internal byte unk_24;

    internal UnknownStruct* unk_8s => (UnknownStruct*)*unk_8;

    internal readonly Span<byte> RawDumpSpan
    {
        get
        {
            fixed (byte* ptr = RawDump)
            {
                return new Span<byte>(ptr, sizeof(InputData));
            }
        }
    }
}
