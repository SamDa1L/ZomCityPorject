namespace ZomCity
{
    /// <summary>
    /// A compact snapshot of the engine-level time / frame pacing knobs we freeze in M0.
    /// </summary>
    public struct TimeSettingsSnapshot
    {
        public float FixedDeltaTime;
        public float MaximumDeltaTime;
        public int VSyncCount;
        public int TargetFrameRate;
    }
}

