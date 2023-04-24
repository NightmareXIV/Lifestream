using Dalamud.Game.ClientState.Aetherytes;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal struct TinyAetheryte : IEquatable<TinyAetheryte>
    {
        internal Vector2 Position;
        internal uint TerritoryType;
        internal uint ID;
        internal uint Group;
        internal string Name;
        internal bool IsAetheryte;
        private Aetheryte Ref { get; init; }

        public TinyAetheryte(Vector2 position, uint territoryType, uint iD, uint group)
        {
            Ref = Svc.Data.GetExcelSheet<Aetheryte>().GetRow(iD);
            Position = position;
            TerritoryType = territoryType;
            ID = iD;
            Group = group;
            Name = Ref.AethernetName.Value.Name.ToString();
            IsAetheryte = Ref.IsAetheryte;
        }

        public override bool Equals(object obj)
        {
            return obj is TinyAetheryte aetheryte && Equals(aetheryte);
        }

        public bool Equals(TinyAetheryte other)
        {
            return Position.Equals(other.Position) &&
                   TerritoryType == other.TerritoryType &&
                   ID == other.ID &&
                   Group == other.Group;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, TerritoryType, ID, Group);
        }

        public static bool operator ==(TinyAetheryte left, TinyAetheryte right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TinyAetheryte left, TinyAetheryte right)
        {
            return !(left == right);
        }
    }
}
