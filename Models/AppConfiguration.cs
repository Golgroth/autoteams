namespace autoteams.Models
{
    public class AppConfiguration
    {
        public int SearchWaitTime { get; set; }
        public int LoginToPasswordWaitTimeMilis { get; set; }
        public ushort MaxLoadAttemptsForRefresh { get; set; }
        public int PageLoadedCheckIntervalMilis { get; set; }
        public bool OutputDebug { get; set; }
        public short SchedulerSyncOnSecond { get; set; }
        public bool HeadlessMode { get; set; }
        public bool AllowMicrophone { get; set; }
        public bool AllowWebcam { get; set; }
    }
}