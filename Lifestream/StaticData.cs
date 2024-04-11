using ECommons.Configuration;

namespace Lifestream;

public class StaticData : IEzConfig
{
    public Dictionary<uint, uint> Callback = new();
    public Dictionary<uint, Vector3> CustomPositions = new();
    public Dictionary<uint, uint> SortOrder = new();
}
