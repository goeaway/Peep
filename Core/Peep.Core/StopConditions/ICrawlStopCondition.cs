namespace Peep.Core.StopConditions
{
    public interface ICrawlStopCondition
    {
        bool Stop(CrawlResult progress);
    }
}
