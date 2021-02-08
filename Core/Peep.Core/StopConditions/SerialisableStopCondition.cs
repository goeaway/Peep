using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.StopConditions
{
    public class SerialisableStopCondition : ICrawlStopCondition
    {
        public object Value { get; set; }
        public SerialisableStopConditionType Type { get; set; }

        public bool Stop(CrawlProgress progress)
        {
            if(Value == null)
            {
                throw new InvalidOperationException("Value was null");
            }

            return Type switch
            {
                SerialisableStopConditionType.MaxCrawlCount => progress.CrawlCount >= Convert.ToInt32(Value),
                SerialisableStopConditionType.MaxDataCount => progress.DataCount >= Convert.ToInt32(Value),
                SerialisableStopConditionType.MaxDurationSeconds => progress.Duration.TotalSeconds >= Convert.ToInt32(Value),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
