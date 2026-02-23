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

    public struct PlaneDriftEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
        public float ObservedZ;
        public float CorrectedZ;
        public float MinAllowedZ;
        public float MaxAllowedZ;
    }


    public struct DataMissingEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
        public string DataDomain;
        public string DataId;
        public string DataVersion;
        public string SaveVersion;
        public string Reason;
    }

    public struct PrefabLoadFailedEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
        public string PrefabKey;
        public string DataVersion;
        public string SaveVersion;
        public string Reason;
    }

    public struct NoiseEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
        public string WeaponId;
        public float Radius;
        public Vector3 SourcePosition;
        public string Reason;
    }

    public struct DamageEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
        public string WeaponId;
        public string TargetId;
        public float Amount;
        public Vector3 HitPoint;
        public Vector3 SourcePosition;
    }

    public struct PickupEvent
    {
        public string EventId;
        public int Frame;
        public string Scene;
        public string ActorId;
        public string ItemId;
        public int Quantity;
        public string Source;
    }

}
