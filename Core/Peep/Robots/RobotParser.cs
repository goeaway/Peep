using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Peep.Robots
{
    public class RobotParser : IRobotParser
    {
        private readonly HttpClient _client;
        private readonly ConcurrentDictionary<string, IEnumerable<string>> _forbiddenPaths;

        public RobotParser(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _forbiddenPaths = new ConcurrentDictionary<string, IEnumerable<string>>();
        }

        public async Task<bool> UriForbidden(Uri uri, string userAgent)
        {
            if (!_forbiddenPaths.TryGetValue(uri.Host, out var forbidden))
            {
                try
                {
                    // make request to get the forbidden urls
                    var result = await _client
                        .GetStringAsync(
                            uri.Scheme + "://" + uri.Host + ":" + uri.Port + "/robots.txt");

                    if (result != null)
                    {
                        forbidden = ParseRobotsFile(result, userAgent);
                        _forbiddenPaths.TryAdd(uri.Host, forbidden);
                    }
                }
                catch (HttpRequestException)
                {
                    // ignore it and say it's not forbidden
                    return false;
                }
            }

            var testUrl = uri.PathAndQuery;
            foreach (var path in forbidden)
            {
                // if path contains a wildcard
                if (path.Contains("*"))
                {
                    // split path at the wildcard
                    // make sure the testUrl starts with the bit before the wildcard
                    // and make sure it ends with the bit after the wildcard
                    var split = path.Split("*");

                    if (split.Length != 2)
                    {
                        throw new FormatException("wildcard robot permission not in expected format");
                    }

                    var front = split[0];
                    var back = split[1];

                    if (testUrl.Length >= front.Length && testUrl.StartsWith(front) && testUrl.Contains(back))
                    {
                        return true;
                    }
                }
                else if (testUrl.Length >= path.Length && testUrl.StartsWith(path))
                {
                    return true;
                }

            }

            return false;
        }

        private IEnumerable<string> ParseRobotsFile(string fileText, string userAgent)
        {
            if (fileText == null)
            {
                return new List<string>();
            }

            if (userAgent == null)
            {
                userAgent = "";
            }

            // split into lines (removing empties)
            var splitResult = fileText.ToLower().Split(
                new string[] { Environment.NewLine },
                StringSplitOptions.RemoveEmptyEntries);

            var listenToThisLine = false;

            var returnList = new List<string>();

            // find the user agent lines
            foreach (var line in splitResult)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("user-agent"))
                {
                    // turn off/on listening to disallows if this user-agent block is relevant to us
                    listenToThisLine = trimmed.Contains("*") || trimmed.Replace("user-agent: ", "") == userAgent.ToLower();
                    // nothing more needed from this line, continue to next
                    continue;
                }

                if (listenToThisLine)
                {
                    // space to care about sitemaps in future

                    if (trimmed.StartsWith("disallow"))
                    {
                        var url = trimmed.Replace("disallow: ", "");

                        if (!url.Contains("disallow") && !url.Contains(" ") && url.Length != 0)
                        {
                            returnList.Add(url);
                        }
                    }
                }
            }

            return returnList;
        }
    }
}
