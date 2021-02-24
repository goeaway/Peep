namespace Peep.Core.API.Options
{
    public class CachingOptions
    {
        public const string Key = "Caching";
        
        public string Hostname { get; set; }
        public int Port { get; set; }
    }
}