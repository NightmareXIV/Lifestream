namespace Lifestream.Data;
[Serializable]
public class TravelBanInfo
{
    internal string ID = Guid.NewGuid().ToString();
    public bool IsEnabled = true;
    public string CharaName = "";
    public int CharaHomeWorld = 0;
    public List<int> BannedFrom = [];
    public List<int> BannedTo = [];
}
