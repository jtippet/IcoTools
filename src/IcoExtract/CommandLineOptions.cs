using CommandLine;
using System.Collections.Generic;

namespace Ico.Extract
{
    internal enum OutputFileFormatPreference
    {
        AlwaysPng,
        AlwaysBmp,
        KeepSource,
    }

    internal class CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "File(s) to read.  Accepts wildcards *, **, and ?.")]
        public IEnumerable<string> Inputs { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Prints more junk to the screen")]
        public bool Verbose { get; set; } = false;

        [Option('o', "output-format", Required = false, HelpText = "The image format to emit")]
        public OutputFileFormatPreference OutputFormat { get; set; } = OutputFileFormatPreference.AlwaysPng;

        [Option('f', "force-overwrite", Required = false, HelpText = "Allow overwriting output files")]
        public bool ForceOverwrite { get; set; } = false;

    }
}
