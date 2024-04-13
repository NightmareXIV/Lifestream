namespace Lifestream.Game;

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
internal unsafe struct UnknownStruct
{
    [FieldOffset(4)] public byte unk_4;
    [FieldOffset(8)] public int SelectedItem;
}
