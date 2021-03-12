using System;
using System.Collections.Generic;

namespace Peep.Data
{
    public class ExtractedData : Dictionary<Uri, IEnumerable<string>>
    {
        public ExtractedData() {}

        public ExtractedData(IDictionary<Uri, IEnumerable<string>> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }
            
            foreach (var (key, value) in dictionary)
            {
                Add(key, value);
            }
        }

    }
}