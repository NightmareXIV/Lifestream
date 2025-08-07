using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Movement;
public unsafe sealed class MovementConfig
{
    public bool StopOnStuck = false;
    public float StuckTolerance = 0.05f;
    public int StuckTimeoutMs = 500;
    public bool CancelMoveOnUserInput = false;
    public bool AlignCameraToMovement = false;
}