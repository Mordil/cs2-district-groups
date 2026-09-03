using UnityEngine;

namespace DistrictGroups
{
    // The one cadence everything that re-reads aggregate or derived data shares
    //
    // Version is simply which interval-sized window of wall-clock time we're in,
    // so nothing has to tick it and no consumer keeps a timer of its own: hold
    // the version you last acted on, compare, act, store the new one.
    internal static class RefreshClock
    {
        // Never equal to a real Version, so a consumer starting (or reset) here
        // always refreshes the next time it looks.
        internal const int kNeverRefreshed = -1;

        internal static int Version => (int)(Time.realtimeSinceStartup / IntervalSeconds);

        // Clamped because the interval is a divisor below, and settings arrive
        // from a file the player can edit.
        private static int IntervalSeconds =>
            Mathf.Max(1, Mod.Settings?.RefreshRateSeconds ?? Setting.kDefaultRefreshRateSeconds);
    }
}
