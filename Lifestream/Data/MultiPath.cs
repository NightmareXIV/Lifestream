namespace Lifestream.Data;
[Serializable]
public class MultiPath
{
    internal Guid GUID = Guid.NewGuid();
    public string Name = "";
    public List<MultiPathEntry> Entries = [];
}
