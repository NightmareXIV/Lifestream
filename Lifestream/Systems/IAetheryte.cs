namespace Lifestream.Systems;
public interface IAetheryte
{
    Vector2 Position { get; set; }
    uint TerritoryType { get; set; }
    uint ID { get; set; }
    string Name { get; set; }
}
