using System;
using UnityEngine;

namespace ZomCity
{
    /// <summary>
    /// 最小事件总线（M0），用于开发期可观测性。
    /// 后续里程碑会继续扩展 Noise 和 Damage 等事件。
    /// </summary>
    public static class GameplayEventHub
    {
        public static event Action<TimeTamperedEvent> TimeTampered;
        public static event Action<CameraTargetLostEvent> CameraTargetLost;
        public static event Action<FireRequestedEvent> CombatFireRequested;
        public static event Action<FireConsumedEvent> CombatFireConsumed;
        public static event Action<FireBlockedEvent> CombatFireBlocked;

        public static void Publish(TimeTamperedEvent evt)
        {
            TimeTampered?.Invoke(evt);

            // EventViewer 完成前保留一条结构化日志作为兜底。
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

        public static void Publish(CameraTargetLostEvent evt)
        {
            CameraTargetLost?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId}");
            }
        }

        public static void Publish(FireRequestedEvent evt)
        {
            CombatFireRequested?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"screen=({evt.ScreenPosition.x:F1},{evt.ScreenPosition.y:F1})");
            }
        }

        public static void Publish(FireBlockedEvent evt)
        {
            CombatFireBlocked?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} reason={evt.Reason}");
            }
        }

        public static void Publish(FireConsumedEvent evt)
        {
            CombatFireConsumed?.Invoke(evt);

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"[{evt.EventId}] frame={evt.Frame} scene={evt.Scene} actor={evt.ActorId} " +
                    $"requestFrame={evt.RequestFrame} aim=({evt.AimPoint.x:F2},{evt.AimPoint.y:F2},{evt.AimPoint.z:F2}) hit={evt.HasHit}");
            }
        }
    }
}
