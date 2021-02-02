using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core
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
                SerialisableStopConditionType.MaxCrawlCount => progress.CrawlCount >= (Value as int?),
                SerialisableStopConditionType.MaxDataCount => progress.DataCount >= (Value as int?),
                SerialisableStopConditionType.MaxDurationSeconds => progress.Duration.TotalSeconds >= (Value as int?),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
