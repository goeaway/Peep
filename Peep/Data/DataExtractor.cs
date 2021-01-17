using Peep.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Peep.Data
{
    public class DataExtractor : IDataExtractor
    {
        private const string URI_REGEX_PATTERN = "<a.*?href=[\"'](.*?)[\"']";
        private readonly Regex _uriRegex = new Regex(URI_REGEX_PATTERN);
        private Regex _customRegex;
        private bool _extractData;
        private string _extractGroupName;

        public void LoadCustomRegexPattern(string regexPattern) => LoadCustomRegexPattern(regexPattern, "data");

        public void LoadCustomRegexPattern(string regexPattern, string extractGroupName)
        {
            _customRegex = new Regex(regexPattern ?? "");
            _extractData = !string.IsNullOrWhiteSpace(regexPattern);
            _extractGroupName = extractGroupName;
        }

        public IEnumerable<string> ExtractData(string html)
        {
            if (html == null || !_extractData)
            {
                yield break;
            }

            if (_customRegex == null)
            {
                throw new InvalidOperationException("Custom Regex not defined. Custom regex must be loaded before extracting data.");
            }

            var matches = _customRegex.Matches(html);

            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.ContainsKey("data"))
                {
                    yield return match.Groups["data"].Value;
                }
            }
        }

        public IEnumerable<Uri> ExtractURIs(Uri source, string html)
        {
            if (source == null || html == null)
            {
                yield break;
            }

            var matches = _uriRegex.Matches(html);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var value = match.Groups[1].Value;

                    if (!value.StartsWith("https://") && !value.StartsWith("http://") && !value.StartsWith(source.Host))
                    {
                        value = source.Scheme + "://" + source.Host + ":" + source.Port + (!value.StartsWith("/") ? "/" : "") + value;

                        if (!value.EndsWith("/"))
                        {
                            value += "/";
                        }
                    }

                    Uri uri;

                    try
                    {
                        uri = new Uri(value);
                    }
                    catch (UriFormatException)
                    {
                        continue;
                    }

                    yield return uri;
                }
            }
        }
    }
}
