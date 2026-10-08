using UnityEngine;

/// <summary>
/// Utility class for determining the octant index of a 2D vector input,
/// typically used for radial direction calculations in UI or game controls.
/// </summary>
[System.Serializable]
public class RadialDirectionUtility
{
    private int _lastOctant = -1;

    // Index mapping: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
    public int GetOctantIndex(Vector2 input, float deadzoneSqr = 0.36f, float outerThresholdSqr = 0.64f)
    {
        float sqrMag = input.sqrMagnitude;

        // INNER DEADZONE: Stick is centered; clear active octant
        if (sqrMag < deadzoneSqr)
        {
            _lastOctant = -1;
            return -1;
        }

        // OUTER RING: Active engagement zone; calculate direction freely
        if (sqrMag >= outerThresholdSqr)
        {
            float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            _lastOctant = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;
            return _lastOctant;
        }

        // FREEZE ZONE: Stick is collapsing back to center
        return _lastOctant;
    }
}