using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.CLI
{
    public class Options
    {
        [Option(
            'd',
            "directory",
            HelpText = "Set the directory the crawler will pick jobs from and leave results. Defaults to \".\".")]
        public string ProcessDirectory { get; set; }
            = ".";

        [Option(
            'i',
            "interval",
            HelpText = "Set the interval between crawl progress updates to the log in seconds. Defaults to 60, set to 0 to disable progress updates."
            )]
        public int ProgressUpdateIntervalSeconds { get; set; }
            = 60;
    }
}
