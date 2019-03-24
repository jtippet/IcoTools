using CommandLine;
using Ico.Codecs;
using Ico.Model;
using System.Collections.Generic;

namespace Ico.Cat
{
    internal class CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "ICO file to start with. If the file doesn't exist, one will be created.")]
        public string InputFile { get; set; }

        [Option('s', "source-image", Required = true, HelpText = "Path to the file to add. PNG preferred, but BMP can work.")]
        public string SourceImagePath { get; set; }

        [Option('m', "mask-image", Required = false, HelpText = "Path to the bitmap mask file.")]
        public string MaskImagePath { get; set; }

        [Option('f', "frame-number", Required = false, HelpText = "Frame number to insert at (first frame is 1).")]
        public int? FrameIndex { get; set; }

        [Option('b', "claimed-bitdepth", Required = false, HelpText = "Specify the bit depth of the frame.")]
        public int? BitDepthOverride { get; set; }

        [Option('e', "encoding-type", Required = false, HelpText = "Specify the encoding to use (PNG or Bitmap).")]
        public string EncodingOverrideString { get; set; }

        internal IcoEncodingType? EncodingOverride { get; set; }

        [Option("bitmap-encoding", Required = false, HelpText = "Specify the precise type of bitmap encoding.")]
        public string BitmapEncodingString { get; set; }

        internal BitmapEncoding? BitmapEncodingOverride { get; set; }

        [Option("keep-raw-data", Required = false, HelpText = "Emit the exact source image, even if non-compliant.")]
        public bool KeepRawFrameData { get; set; } = false;
    }
}
