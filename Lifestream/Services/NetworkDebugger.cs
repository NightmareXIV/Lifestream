using Dalamud.Game.Network;
using Dalamud.Memory;

namespace Lifestream.Services;
public unsafe class NetworkDebugger : IDisposable
{
    private NetworkDebugger()
    {
        Svc.GameNetwork.NetworkMessage += GameNetwork_NetworkMessage;
    }

    private void GameNetwork_NetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, Dalamud.Game.Network.NetworkMessageDirection direction)
    {
        if(direction == NetworkMessageDirection.ZoneDown && opCode == 0x18a)
        {
            var mem = MemoryHelper.ReadRaw(dataPtr, 40);
            var mem2 = MemoryHelper.ReadRaw(dataPtr + 40, 40);
            PluginLog.Information(mem.ToHexString() + "\n" + mem2.ToHexString());
        }
    }

    public void Dispose()
    {
        Svc.GameNetwork.NetworkMessage -= GameNetwork_NetworkMessage;
    }
}
