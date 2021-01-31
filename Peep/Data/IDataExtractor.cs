using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Data
{
    public interface IDataExtractor
    {
        void LoadCustomRegexPattern(string pattern);
        void LoadCustomRegexPattern(string regexPattern, string extractGroupName);
        void LoadCustomRegexPattern(string regexPattern, IEnumerable<string> extractGroupNames);
        IEnumerable<string> ExtractData(string html);
        IEnumerable<Uri> ExtractURIs(Uri source, string html);
    }
}
