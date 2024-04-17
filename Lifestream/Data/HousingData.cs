using ECommons.Configuration;
using ECommons.ExcelServices.TerritoryEnumeration;
using Lifestream.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Data;
public class HousingData : IEzConfig
{
    public Dictionary<uint, List<PlotInfo>> Data = [];
}
