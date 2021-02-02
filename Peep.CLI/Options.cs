using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.CLI
{
    public class Options
    {
        [Option('d', "directory", Required = true, HelpText = "Set the directory the crawler will pick jobs from and leave results.")]
        public string ProcessDirectory { get; set; }
    }
}
