using System;

namespace TreadShred.Progression
{
    /// <summary>
    /// D-041: the mission list opens on the mission to play next, already selected with its play arrow, and scrolls
    /// that row into view.
    /// </summary>
    public static class MissionList
    {
        /// <summary>The first unlocked mission not yet cleared at the selected tier; mission 1 when there is none.</summary>
        public static int NextToPlay(int missionCount, Func<int, bool> unlocked, Func<int, bool> cleared)
        {
            for (var mission = 1; mission <= missionCount; mission++)
            {
                if (unlocked(mission) && !cleared(mission))
                    return mission;
            }
            return 1;
        }

        /// <summary>
        /// The vertical scroll position (1 is the top, 0 the bottom) that shows a row in the middle of the view. It moves
        /// in whole rows, so every row stays aligned as it is at the top and none straddles the edge of a tall screen,
        /// and it holds at either end.
        /// </summary>
        public static float ScrollPosition(int rowIndex, float rowPitch, float contentHeight, float viewHeight)
        {
            var travel = contentHeight - viewHeight;
            if (travel <= 0f || rowPitch <= 0f)
                return 1f;
            var rowsAbove = (int)(viewHeight / rowPitch) / 2;
            var lastStep = (int)(travel / rowPitch);
            var step = Math.Min(lastStep, Math.Max(0, rowIndex - rowsAbove));
            return 1f - step * rowPitch / travel;
        }
    }
}
