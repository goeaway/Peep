using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Abstractions
{
    public interface IDataExtractor
    {
        void LoadCustomRegexPattern(string pattern);
        IEnumerable<string> ExtractData(string html);
        IEnumerable<Uri> ExtractURIs(Uri source, string html);
    }
}
