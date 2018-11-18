using CommandLine;
using System.Collections.Generic;

namespace Ico.Info
{
    internal class CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "File(s) to read.  Accepts wildcards *, **, and ?.")]
        public IEnumerable<string> Inputs { get; set; }
    }
}
