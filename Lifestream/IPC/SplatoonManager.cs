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
        if (Splatoon.IsConnected()) Reset();
    }

    private void Reset()
    {
        Cache = new();
    }

    private unsafe void ResetOnFrameChange()
    {
        var frame = CSFramework.Instance()->FrameCounter;
        if (frame != Frame)
        {
            Frame = frame;
            Reset();
        }
    }

    public void RenderPath(IReadOnlyList<Vector3> path, bool addPlayer = true)
    {
        Vector3? prev = null;
        if (path != null && path.Count > 0)
        {
            for (int i = 0; i < path.Count; i++)
            {
                var point = GetNextPoint();
                point.SetRefCoord(path[i]);
                var line = GetNextLine();
                line.SetRefCoord(path[i]);
                line.SetOffCoord(prev ?? Player.Object.Position);
                line.color = (prev != null ? ImGuiColors.DalamudYellow : ImGuiColors.HealerGreen).ToUint();
                if (prev != null || addPlayer)
                {
                    Splatoon.DisplayOnce(point);
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
        if (Cache.WaymarkLineCache.Count < Cache.WaymarkLinePos)
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

    public Element GetNextPoint()
    {
        ResetOnFrameChange();
        Element ret;
        if (Cache.WaymarkPointCache.Count < Cache.WaymarkPointPos)
        {
            ret = Cache.WaymarkPointCache[Cache.WaymarkPointPos];
        }
        else
        {
            ret = new Element(ElementType.CircleAtFixedCoordinates)
            {
                radius = 0f,
                thicc = 3f,
                color = ImGuiColors.DalamudRed.ToUint()
            };
            Cache.WaymarkPointCache.Add(ret);
        }
        Cache.WaymarkPointPos++;
        return ret;
    }
}
