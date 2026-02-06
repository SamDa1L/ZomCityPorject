using System;
using UnityEngine;

namespace ZomCity
{
    /// <summary>
    /// Minimal event hub (M0) for development-time observability.
    /// This will be expanded in later milestones (Noise/Damage/Pickup/...).
    /// </summary>
    public static class GameplayEventHub
    {
        public static event Action<TimeTamperedEvent> TimeTampered;

        public static void Publish(TimeTamperedEvent evt)
        {
            TimeTampered?.Invoke(evt);

            // Until the Viewer lands (M0.6), keep a single structured log line as a fallback.
            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} " +
                    $"fixedDT {evt.Observed.FixedDeltaTime:F6}->{evt.Authoritative.FixedDeltaTime:F6}, " +
                    $"maxDT {evt.Observed.MaximumDeltaTime:F6}->{evt.Authoritative.MaximumDeltaTime:F6}, " +
                    $"vSync {evt.Observed.VSyncCount}->{evt.Authoritative.VSyncCount}, " +
                    $"targetFPS {evt.Observed.TargetFrameRate}->{evt.Authoritative.TargetFrameRate}");
            }
        }
    }
}

