namespace Peep.Core.API.Options
{
    public class MonitoringOptions
    {
        public const string Key = "Monitoring";

        public int MaxUnresponsiveTicks { get; set; }
            = 3;

        public double TickSeconds { get; set; }
            = 5;
    }
}