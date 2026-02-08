using UnityEngine;

namespace ZomCity
{
    public struct TimeTamperedEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;

        public TimeSettingsSnapshot Observed;
        public TimeSettingsSnapshot Authoritative;
    }

    public struct CameraTargetLostEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
    }

    public struct FireRequestedEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
        public Vector2 ScreenPosition;
    }

    public struct FireConsumedEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;

        public int RequestFrame;
        public Vector2 ScreenPosition;
        public Vector3 AimPoint;

        public bool HasHit;
        public Vector3 HitPoint;
    }

    public struct FireBlockedEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
        public string Reason;
    }

}
