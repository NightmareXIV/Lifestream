namespace Lifestream.Systems;
public interface IAetheryte
{
    public Vector2 Position { get; set; }
    public uint TerritoryType { get; set; }
    public uint ID { get; set; }
    public string Name { get; set; }
}
