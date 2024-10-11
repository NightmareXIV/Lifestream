using ECommons.Configuration;

namespace Lifestream.Data;
public class HousingData : IEzConfig
{
    public Dictionary<uint, List<PlotInfo>> Data = [];
}
