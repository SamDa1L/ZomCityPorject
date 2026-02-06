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
}

