using Dalamud.Plugin.Services;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Service = ECommons.DalamudServices.Svc;

namespace Lifestream.Movement;

public class FollowPath : IDisposable
{
    public bool MovementAllowed = true;
    public bool IgnoreDeltaY = false;
    public float Tolerance = 0.25f;
    public float DestinationTolerance = 0;
    private List<Vector3> WaypointsInternal = new();
    public IReadOnlyList<Vector3> Waypoints => WaypointsInternal;

    private IDalamudPluginInterface _dalamud;
    private OverrideCamera _camera = new();
    private OverrideMovement _movement = new();
    private DateTime _nextJump;

    private Vector3? posPreviousFrame;

    private int _millisecondsWithNoSignificantMovement = 0;

    public event Action<Vector3, bool, float>? OnStuck;

    public MovementConfig MovementConfig = new();

    // entries in dalamud shared data cache must be reference types, so we use an array
    private readonly bool[] _sharedPathIsRunning;

    private const string _sharedPathTag = "Lifestream.PathIsRunning";

    public int MaxWaypoints = 0;
    public static readonly string FollowPathTime = "FollowPathTime";

    public FollowPath()
    {
        _dalamud = Svc.PluginInterface;
        _sharedPathIsRunning = _dalamud.GetOrCreateData<bool[]>(_sharedPathTag, () => [false]);
    }

    public void Dispose()
    {
        UpdateSharedState(false);
        _dalamud.RelinquishData(_sharedPathTag);
        _camera.Dispose();
        _movement.Dispose();
    }

    private void UpdateSharedState(bool isRunning) => _sharedPathIsRunning[0] = isRunning;

    public void Update()
    {
        var player = Service.ClientState.LocalPlayer;
        if(player == null)
            return;

        while(WaypointsInternal.Count > 0)
        {
            if(WaypointsInternal.Count > MaxWaypoints) MaxWaypoints = WaypointsInternal.Count;
            if(EzThrottler.Check(FollowPathTime)) EzThrottler.Throttle(FollowPathTime, 60000, true);
            if(S.Ipc.VnavmeshIPC.IsRunning())
            {
                WaypointsInternal.Clear();
                DuoLog.Error($"Detected vnavmesh movement, Lifestream will abort all tasks now.");
                break;
            }
            if(EzThrottler.Check(FollowPathTime))
            {
                WaypointsInternal.Clear();
                DuoLog.Error($"Lifestream movement has timed out.");
                break;
            }

            var a = WaypointsInternal[0];
            var b = player.Position;
            var c = posPreviousFrame ?? b;

            if(DestinationTolerance > 0 && (b - WaypointsInternal[^1]).Length() <= DestinationTolerance)
            {
                WaypointsInternal.Clear();
                break;
            }

            if(IgnoreDeltaY)
            {
                a.Y = 0;
                b.Y = 0;
                c.Y = 0;
            }

            if(DistanceToLineSegment(a, b, c) > Tolerance)
                break;

            WaypointsInternal.RemoveAt(0);
            EzThrottler.Reset(FollowPathTime);
        }


        if(WaypointsInternal.Count == 0)
        {
            posPreviousFrame = player.Position;
            _movement.Enabled = _camera.Enabled = false;
            _camera.SpeedH = _camera.SpeedV = default;
            _movement.DesiredPosition = player.Position;
            UpdateSharedState(false);
            MaxWaypoints = 0;
            EzThrottler.Reset(FollowPathTime);
        }
        else
        {
            if(MovementConfig.StopOnStuck && posPreviousFrame.HasValue)
            {
                float delta = Svc.Framework.UpdateDelta.Milliseconds / 1000f;
                float distance = Vector3.Distance(player.Position, posPreviousFrame.Value) / delta;
                if(distance <= MovementConfig.StuckTolerance)
                {
                    _millisecondsWithNoSignificantMovement += Svc.Framework.UpdateDelta.Milliseconds;
                }
                else
                {
                    _millisecondsWithNoSignificantMovement = 0;
                }

                if(_millisecondsWithNoSignificantMovement >= MovementConfig.StuckTimeoutMs)
                {
                    var destination = WaypointsInternal[^1];
                    Stop();
                    OnStuck?.Invoke(destination, !IgnoreDeltaY, DestinationTolerance);
                    return;
                }
            }

            posPreviousFrame = player.Position;

            if(MovementConfig.CancelMoveOnUserInput && _movement.UserInput)
            {
                Stop();
                return;
            }

            OverrideAFK.ResetTimers();
            _movement.Enabled = MovementAllowed;
            _movement.DesiredPosition = WaypointsInternal[0];
            if(_movement.DesiredPosition.Y > player.Position.Y && !Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InFlight] && !Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Diving] && !IgnoreDeltaY) //Only do this bit if on a flying path
            {
                // walk->fly transition (TODO: reconsider?)
                if(Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted])
                    ExecuteJump(); // Spam jump to take off
                else
                {
                    _movement.Enabled = false; // Don't move, since it'll just run on the spot
                    return;
                }
            }

            _camera.Enabled = MovementConfig.AlignCameraToMovement;
            _camera.SpeedH = _camera.SpeedV = 360.Degrees();
            _camera.DesiredAzimuth = Angle.FromDirectionXZ(_movement.DesiredPosition - player.Position) + 180.Degrees();
            _camera.DesiredAltitude = -30.Degrees();
        }
    }

    private static float DistanceToLineSegment(Vector3 v, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        var av = v - a;

        if(ab.Length() == 0 || Vector3.Dot(av, ab) <= 0)
            return av.Length();

        var bv = v - b;
        if(Vector3.Dot(bv, ab) >= 0)
            return bv.Length();

        return Vector3.Cross(ab, av).Length() / ab.Length();
    }

    public void Stop()
    {
        UpdateSharedState(false);
        _millisecondsWithNoSignificantMovement = 0;
        WaypointsInternal.Clear();
    }

    private unsafe void ExecuteJump()
    {
        // Unable to jump while diving, prevents spamming error messages.
        if(Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Diving])
            return;

        if(DateTime.Now >= _nextJump)
        {
            ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2);
            _nextJump = DateTime.Now.AddMilliseconds(100);
        }
    }

    public void Move(List<Vector3> waypoints, bool ignoreDeltaY, float destTolerance = 0)
    {
        UpdateSharedState(true);
        WaypointsInternal = [..waypoints];
        IgnoreDeltaY = ignoreDeltaY;
        DestinationTolerance = destTolerance;
    }

    public void RemoveFirst() => WaypointsInternal.RemoveAt(0);
    public void UpdateTimeout(int seconds) => EzThrottler.Throttle(FollowPathTime, seconds * 1000, true);
}
