namespace ZomCity
{
    /// <summary>
    /// M0 冻结时间与帧率步进参数的快照。
    /// 该结构用于比对、回写与事件上报。
    /// </summary>
    public struct TimeSettingsSnapshot
    {
        public float FixedDeltaTime;
        public float MaximumDeltaTime;
        public int VSyncCount;
        public int TargetFrameRate;
    }
}
