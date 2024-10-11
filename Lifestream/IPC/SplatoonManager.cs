using ECommons.GameHelpers;
using ECommons.SplatoonAPI;

namespace Lifestream.IPC;
public class SplatoonManager
{
    private ulong Frame = 0;
    private SplatoonCache Cache = new();

    public SplatoonManager()
    {
        Splatoon.SetOnConnect(Reset);
        if(Splatoon.IsConnected()) Reset();
    }

    private void Reset()
    {
        Cache = new();
    }

    private unsafe void ResetOnFrameChange()
    {
        var frame = CSFramework.Instance()->FrameCounter;
        if(frame != Frame)
        {
            Frame = frame;
            Reset();
        }
    }

    public void RenderPath(IReadOnlyList<Vector3> path, bool addPlayer = true, bool addNumbers = false)
    {
        if(!Splatoon.IsConnected()) return;
        Vector3? prev = null;
        if(path != null && path.Count > 0)
        {
            for(var i = 0; i < path.Count; i++)
            {
                var point = GetNextPoint(addNumbers ? (i + 1).ToString() : "");
                point.SetRefCoord(path[i]);
                var line = GetNextLine();
                line.SetRefCoord(path[i]);
                line.SetOffCoord(prev ?? Player.Object.Position);
                line.color = (prev != null ? ImGuiColors.DalamudYellow : ImGuiColors.HealerGreen).ToUint();
                Splatoon.DisplayOnce(point);
                if(prev != null || addPlayer)
                {
                    Splatoon.DisplayOnce(line);
                }
                prev = path[i];
            }
        }
    }

    public Element GetNextLine()
    {
        ResetOnFrameChange();
        Element ret;
        if(Cache.WaymarkLineCache.Count < Cache.WaymarkLinePos)
        {
            ret = Cache.WaymarkLineCache[Cache.WaymarkLinePos];
        }
        else
        {
            ret = new Element(ElementType.LineBetweenTwoFixedCoordinates)
            {
                radius = 0f,
                thicc = 1f,
            };
            Cache.WaymarkLineCache.Add(ret);
        }
        Cache.WaymarkLinePos++;
        return ret;
    }

    public Element GetNextPoint(string overlay = "")
    {
        ResetOnFrameChange();
        Element ret;
        if(Cache.WaymarkPointCache.Count < Cache.WaymarkPointPos)
        {
            ret = Cache.WaymarkPointCache[Cache.WaymarkPointPos];
        }
        else
        {
            ret = new Element(ElementType.CircleAtFixedCoordinates)
            {
                radius = 0f,
                thicc = 3f,
                color = ImGuiColors.DalamudRed.ToUint(),
                overlayVOffset = 1f,
                overlayText = overlay,
            };
            Cache.WaymarkPointCache.Add(ret);
        }
        Cache.WaymarkPointPos++;
        return ret;
    }
}
