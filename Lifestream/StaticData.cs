using ECommons.Configuration;

namespace Lifestream;

public class StaticData : IEzConfig
{
    public Dictionary<uint, uint> Callback = [];
    public Dictionary<uint, Vector3> CustomPositions = [];
    public Dictionary<uint, uint> SortOrder = [];
}
