using ECommons.Configuration;

namespace Lifestream.Data;

public class StaticData : IEzConfig
{
    public Dictionary<uint, Vector3> CustomPositions = [];
    public Dictionary<uint, uint> SortOrder = [];
}
