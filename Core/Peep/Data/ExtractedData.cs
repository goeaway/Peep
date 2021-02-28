using System;
using System.Collections.Generic;

namespace Peep.Data
{
    public class ExtractedData : Dictionary<Uri, IEnumerable<string>>
    {
        public ExtractedData()
        {
            
        }

        public ExtractedData(IDictionary<Uri, IEnumerable<string>> data)
        {
            foreach (var (key, value) in data)
            {
                Add(key, value);
            }
        }
    }
}