using Lifestream.Enums;
using Lifestream.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Data;
[Serializable]
public class HousePathData
{
    public ResidentialAetheryteKind ResidentialDistrict;
    public int Ward;
    public int Plot;
    public List<Vector3> PathToEntrance = [];
    public List<Vector3> PathToWorkshop = [];
    public bool IsPrivate;
    public ulong CID;
}
