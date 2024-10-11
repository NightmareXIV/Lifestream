namespace Lifestream.Data;
[Serializable]
public class MultiPathEntry
{
    internal Guid GUID = Guid.NewGuid();
    public uint Territory;
    public bool Sprint = false;
    public List<Vector3> Points = [];
}
