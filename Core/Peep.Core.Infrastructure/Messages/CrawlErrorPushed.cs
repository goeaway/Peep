namespace Peep.Core.Infrastructure.Messages
{
    public class CrawlErrorPushed
    {
        public string JobId { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
    }
}