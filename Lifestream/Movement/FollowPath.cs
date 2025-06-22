using ECommons.GameHelpers;

namespace Lifestream.Movement;

public class FollowPath : IDisposable
{
    public bool MovementAllowed = true;
    public bool AlignCamera = false;
    public bool IgnoreDeltaY = true;
    public float Tolerance = 0.25f;
    public List<Vector3> waypointsInternal = [];
    public IReadOnlyList<Vector3> Waypoints => waypointsInternal;
    public int MaxWaypoints = 0;

    private OverrideCamera _camera = new();
    private OverrideMovement _movement = new();
    private long TimeoutAt = 0;

    public FollowPath()
    {
    }

    public void Dispose()
    {
        _camera.Dispose();
        _movement.Dispose();
    }

    public void UpdateTimeout(int seconds) => TimeoutAt = Environment.TickCount64 + seconds * 1000;


    public unsafe void Update()
    {
        if(!Player.Available)
            return;

        while(waypointsInternal.Count > 0)
        {
            if(waypointsInternal.Count > MaxWaypoints) MaxWaypoints = waypointsInternal.Count;
            if(TimeoutAt == 0) TimeoutAt = Environment.TickCount64 + 30000;
            if(S.Ipc.VnavmeshIPC.IsRunning())
            {
                waypointsInternal.Clear();
                DuoLog.Error($"Detected vnavmesh movement, Lifestream will abort all tasks now.");
                break;
            }
            if(Environment.TickCount64 > TimeoutAt)
            {
                waypointsInternal.Clear();
                DuoLog.Error($"Lifestream movement has timed out.");
                break;
            }
            var toNext = waypointsInternal[0] - Player.Object.Position;
            if(IgnoreDeltaY)
                toNext.Y = 0;
            if(toNext.LengthSquared() > Tolerance * Tolerance)
                break;
            waypointsInternal.RemoveAt(0);
            TimeoutAt = 0;
        }

        if(waypointsInternal.Count == 0)
        {
            _movement.Enabled = _camera.Enabled = false;
            _camera.SpeedH = _camera.SpeedV = default;
            _movement.DesiredPosition = Player.Object.Position;
            MaxWaypoints = 0;
        }
        else
        {
            OverrideAFK.ResetTimers();
            _movement.Enabled = MovementAllowed;
            _movement.DesiredPosition = waypointsInternal[0];
            _camera.Enabled = AlignCamera;
            _camera.SpeedH = _camera.SpeedV = 360.Degrees();
            _camera.DesiredAzimuth = Angle.FromDirectionXZ(_movement.DesiredPosition - Player.Object.Position) + 180.Degrees();
            _camera.DesiredAltitude = -30.Degrees();
        }
    }

    public void Stop() => waypointsInternal.Clear();

    public void RemoveFirst() => waypointsInternal.RemoveAt(0);

    public void Move(List<Vector3> waypoints, bool ignoreDeltaY)
    {
        TimeoutAt = 0;
        waypointsInternal = [.. waypoints];
        IgnoreDeltaY = ignoreDeltaY;
    }
}
