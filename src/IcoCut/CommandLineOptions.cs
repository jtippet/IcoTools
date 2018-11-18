using CommandLine;
using Ico.Codecs;
using System.Collections.Generic;

namespace Ico.Cut
{
    internal class CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "File(s) to edit.  Accepts wildcards *, **, and ?.")]
        public IEnumerable<string> Inputs { get; set; }

        [Option('d', "delete-frame", Required = false, HelpText = "Frame number to delete (counting from 1).")]
        public IEnumerable<int> FrameNumbersToDelete { get; set; }

        [Option('s', "delete-size", Required = false, HelpText = "Delete any frame with height or width of this size (example: 16).")]
        public IEnumerable<int> SizesToDelete { get; set; }

        [Option('b', "delete-bit-depth", Required = false, HelpText = "Delete any frame with this bit depth (example: 4).")]
        public IEnumerable<int> BitDepthsToDelete { get; set; }
    }
}
