using UnityEngine;

namespace WakaSleepWindowAlign
{
    // Computes the number of in-game hours from the current world time to the
    // next dawn (configured to match Singularity / Advanced Sky Manager). Used
    // by the radial "Sleep" command transpile to replace the hardcoded 8h.
    public static class DawnHelper
    {
        public const int DawnHour = 5;

        public static int GetHoursUntilDawn()
        {
            var gm = GameManager.Instance;
            var world = (gm != null) ? gm.World : null;
            if (world == null) return 8;

            int worldHour = (int)(world.worldTime % 24000UL / 1000UL);
            int hours;
            if (worldHour < DawnHour)
            {
                hours = DawnHour - worldHour;
            }
            else
            {
                hours = (24 - worldHour) + DawnHour;
            }
            return Mathf.Clamp(hours, 1, 12);
        }
    }
}
