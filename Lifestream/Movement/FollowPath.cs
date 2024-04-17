using Lifestream.IPC;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Lifestream.Movement;

public class FollowPath : IDisposable
{
    public bool MovementAllowed = true;
    public bool AlignCamera = false;
    public bool IgnoreDeltaY = false;
    public float Tolerance = 0.25f;
    public List<Vector3> Waypoints = new();
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

    public unsafe void Update()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null)
            return;

        while (Waypoints.Count > 0)
        {
            if(Waypoints.Count > MaxWaypoints) MaxWaypoints = Waypoints.Count;
            if (TimeoutAt == 0) TimeoutAt = Environment.TickCount64 + 30000;
            if (P.VnavmeshManager.IsRunning())
            {
                Waypoints.Clear();
                DuoLog.Error($"Detected vnavmesh movement, Lifestream will abort all tasks now.");
                break;
            }
            if(Environment.TickCount64 > TimeoutAt)
            {
                Waypoints.Clear();
                DuoLog.Error($"Lifestream movement has timed out.");
                break;
            }
            var toNext = Waypoints[0] - player.Position;
            if (IgnoreDeltaY)
                toNext.Y = 0;
            if (toNext.LengthSquared() > Tolerance * Tolerance)
                break;
            Waypoints.RemoveAt(0);
            TimeoutAt = 0;
        }

        if (Waypoints.Count == 0)
        {
            _movement.Enabled = _camera.Enabled = false;
            _camera.SpeedH = _camera.SpeedV = default;
            _movement.DesiredPosition = player.Position;
            MaxWaypoints = 0;
        }
        else
        {
            OverrideAFK.ResetTimers();
            _movement.Enabled = MovementAllowed;
            _movement.DesiredPosition = Waypoints[0];
            _camera.Enabled = AlignCamera;
            _camera.SpeedH = _camera.SpeedV = 360.Degrees();
            _camera.DesiredAzimuth = Angle.FromDirectionXZ(_movement.DesiredPosition - player.Position) + 180.Degrees();
            _camera.DesiredAltitude = -30.Degrees();
        }
    }

    public void Stop() => Waypoints.Clear();

    public void Move(List<Vector3> waypoints, bool ignoreDeltaY)
    {
        Waypoints = waypoints;
        IgnoreDeltaY = ignoreDeltaY;
    }
}
