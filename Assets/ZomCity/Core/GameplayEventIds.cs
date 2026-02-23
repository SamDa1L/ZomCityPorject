namespace ZomCity
{
    public static class GameplayEventIds
    {
        public const string TimeTampered = "EVT_Time_Tampered";
        public const string CameraTargetLost = "EVT_Camera_TargetLost";
        public const string CombatFireRequested = "EVT_Combat_FireRequested";
        public const string CombatFireConsumed = "EVT_Combat_FireConsumed";
        public const string CombatFireBlocked = "EVT_Combat_FireBlocked";
        public const string PlaneDrift = "EVT_Plane_Drift";
        public const string DataMissing = "EVT_Data_Missing";
        public const string PrefabLoadFailed = "EVT_Data_PrefabLoadFailed";
        public const string NoiseEmit = "EVT_Noise_Emit";
        public const string CombatDamage = "EVT_Combat_Damage";
        public const string LootPickup = "EVT_Loot_Pickup";
    }
}
