using UnityEngine;

namespace POKE.PMDCollab
{
    /// <summary>
    /// Standard 8-direction ordering used by PMDCollab Sprite sheets (top-to-bottom rows 0..7).
    /// Row 0: Down (South)
    /// Row 1: DownRight (SouthEast)
    /// Row 2: Right (East)
    /// Row 3: UpRight (NorthEast)
    /// Row 4: Up (North)
    /// Row 5: UpLeft (NorthWest)
    /// Row 6: Left (West)
    /// Row 7: DownLeft (SouthWest)
    /// </summary>
    public enum PMDDirection
    {
        Down = 0,
        DownRight = 1,
        Right = 2,
        UpRight = 3,
        Up = 4,
        UpLeft = 5,
        Left = 6,
        DownLeft = 7
    }

    public static class PMDDirectionUtility
    {
        public static PMDDirection FromVector2(Vector2 dir, PMDDirection fallback = PMDDirection.Down)
        {
            if (dir.sqrMagnitude < 0.001f)
                return fallback;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0f)
                angle += 360f;

            // 0 deg = Right (2), 90 deg = Up (4), 180 deg = Left (6), 270 deg = Down (0)
            int sector = Mathf.RoundToInt(angle / 45f) % 8;
            return sector switch
            {
                0 => PMDDirection.Right,
                1 => PMDDirection.UpRight,
                2 => PMDDirection.Up,
                3 => PMDDirection.UpLeft,
                4 => PMDDirection.Left,
                5 => PMDDirection.DownLeft,
                6 => PMDDirection.Down,
                7 => PMDDirection.DownRight,
                _ => fallback
            };
        }

        public static Vector2 ToVector2(this PMDDirection dir)
        {
            return dir switch
            {
                PMDDirection.Down => new Vector2(0f, -1f),
                PMDDirection.DownRight => new Vector2(0.7071f, -0.7071f),
                PMDDirection.Right => new Vector2(1f, 0f),
                PMDDirection.UpRight => new Vector2(0.7071f, 0.7071f),
                PMDDirection.Up => new Vector2(0f, 1f),
                PMDDirection.UpLeft => new Vector2(-0.7071f, 0.7071f),
                PMDDirection.Left => new Vector2(-1f, 0f),
                PMDDirection.DownLeft => new Vector2(-0.7071f, -0.7071f),
                _ => Vector2.down
            };
        }
    }
}

