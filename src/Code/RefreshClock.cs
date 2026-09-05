using System;
using UnityEngine;

namespace DistrictGroups
{
    // A named unit of paced work, one per thing that shouldn't share a frame with the others
    //
    // Cadence-paced work asks RefreshClock.IsDue and comes due at its phase's own
    // point inside the interval; work already marked by a dirty flag asks
    // RefreshClock.CanClean instead, which has no cadence to wait for. Every
    // phase gets a slot of the interval whether or not it uses one, so leave the
    // values contiguous from zero and add new phases at the end.
    internal enum RefreshPhase
    {
        // Group rows, and the district population sweep their totals are summed from
        Groups = 0,

        // The service buildings offered for assignment
        ServiceBuildings = 1,

        // The district and group captures every overlay subsystem reads
        OverlaySnapshot = 2,

        // The overlay's fill meshes and stripe textures
        OverlayFill = 3,

        // The overlay's baked group-name labels
        OverlayLabels = 4,
    }

    // The one cadence everything that re-reads aggregate or derived data shares,
    // and the one grant per frame that keeps paced work off each other's frames
    //
    // A phase's version is simply which interval-sized window of wall-clock time
    // it's in, so nothing has to tick the clock and no consumer keeps a timer of
    // its own: hold the version you last acted on and ask whether it's due.
    internal static class RefreshClock
    {
        // Never equal to a real version, so a consumer starting (or reset) here
        // always refreshes the next time it looks.
        internal const int kNeverRefreshed = -1;

        private static readonly int kPhaseCount = Enum.GetValues(typeof(RefreshPhase)).Length;

        // Which frame the grant was last spent in
        private static int s_ClaimedFrame = -1;

        // Whether the phase's slot of the interval has come around since lastVersion, spending the frame and storing the new version when it has
        internal static bool IsDue(RefreshPhase phase, ref int lastVersion)
        {
            int version = VersionOf(phase);
            if (version == lastVersion)
            {
                return false;
            }

            if (!TryClaimFrame(phase))
            {
                return false;
            }

            lastVersion = version;
            return true;
        }

        // Whether the phase may spend this frame cleaning the work its dirty flag says is needed
        internal static bool CanClean(RefreshPhase phase, bool isDirty)
        {
            if (!isDirty)
            {
                return false;
            }

            return TryClaimFrame(phase);
        }

        // Whether the phase may do its work this frame, spending the frame's one grant when it may
        private static bool TryClaimFrame(RefreshPhase claimant)
        {
            if (Time.frameCount == s_ClaimedFrame)
            {
                return false;
            }

            s_ClaimedFrame = Time.frameCount;
            return true;
        }

        // Which interval-sized window of wall-clock time the phase is currently in
        private static int VersionOf(RefreshPhase phase)
        {
            int interval = IntervalSeconds;
            float offset = interval * ((float)(int)phase / kPhaseCount);
            return (int)((Time.realtimeSinceStartup + offset) / interval);
        }

        // Clamped because the interval is a divisor above, and settings arrive
        // from a file the player can edit.
        private static int IntervalSeconds =>
            Mathf.Max(1, Mod.Settings?.RefreshRateSeconds ?? Setting.kDefaultRefreshRateSeconds);
    }
}
