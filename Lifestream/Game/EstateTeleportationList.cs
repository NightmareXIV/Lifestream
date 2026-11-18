using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lifestream.Game;

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public unsafe struct EstateTeleportationList
{
    [FieldOffset(0x00)]
    public ulong ContentId;

    [FieldOffset(0x08)]
    public HouseId FreeCompanyHouseId;

    [FieldOffset(0x10)]
    public HouseId PrivateHouseId;

    [FieldOffset(0x18)]
    public HouseId ApartmentHouseId;

    [FieldOffset(0x20)]
    public byte FreeCompanyFlag;

    [FieldOffset(0x21)]
    public byte PrivateFlag;

    [FieldOffset(0x22)]
    public byte ApartmentFlag;
}
