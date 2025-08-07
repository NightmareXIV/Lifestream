namespace Lifestream.Movement;

[StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
public unsafe struct CameraEx
{
    [FieldOffset(0x140)] public float DirH; // 0 is north, increases CW
    [FieldOffset(0x144)] public float DirV; // 0 is horizontal, positive is looking up, negative looking down
    [FieldOffset(0x148)] public float InputDeltaHAdjusted;
    [FieldOffset(0x14C)] public float InputDeltaVAdjusted;
    [FieldOffset(0x150)] public float InputDeltaH;
    [FieldOffset(0x154)] public float InputDeltaV;
    [FieldOffset(0x158)] public float DirVMin; // -85deg by default
    [FieldOffset(0x15C)] public float DirVMax; // +45deg by default
}
