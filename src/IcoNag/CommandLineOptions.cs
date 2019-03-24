using CommandLine;
using System.Collections.Generic;

namespace Ico
{
    internal class CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "File(s) to read.  Accepts wildcards *, **, and ?.")]
        public IEnumerable<string> Inputs { get; set; }

        [Option('d', "disable-warning", Required = false, HelpText = "Warning numbers to ignore.")]
        public IEnumerable<string> DisabledWarnings { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Prints more junk to the screen")]
        public bool Verbose { get; set; }
    }
}
