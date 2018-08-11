using CommandLine;
using Ico.Codecs;
using System.Collections.Generic;

namespace Ico
{
    internal class CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "File(s) to compress.  Accepts wildcards *, **, and ?.")]
        public IEnumerable<string> Inputs { get; set; }

        [Option('n', "dry-run", Required = false, HelpText = "Don't actually edit any files on disk.")]
        public bool DryRun { get; set; } = false;

        [Option('w', "write-in-place", Required = false, HelpText = "Overwrite the input file(s).")]
        public bool OverwriteInputFiles { get; set; } = false;

        [Option("allow-giant-inputs", Required = false, Hidden = true, HelpText = "Bypass the safety that blocks reading files >10MB.")]
        public bool AllowGiantFiles { get; set; } = false;

        [Option("emit-masked-image-pixel", Required = false, Hidden = true, HelpText = "Save a couple bytes by skipping a palette entry for masked pixels.")]
        public StrictnessPolicy MaskedImagePixelEmitOptions { get; set; } = StrictnessPolicy.PreserveSource;

        [Option("allow-2bit-bitmaps", Required = false, Hidden = true, HelpText = "Save several bytes by emitting non-compliant 2-bit bitmaps.")]
        public StrictnessPolicy Emit2BitBitmaps { get; set; } = StrictnessPolicy.PreserveSource;

        [Option("allow-24bit-bitmaps", Required = false, Hidden = true, HelpText = "Save several bytes by emitting non-compliant 24-bit bitmaps.")]
        public StrictnessPolicy Emit24BitBitmaps { get; set; } = StrictnessPolicy.PreserveSource;

        [Option("preferred-format", Required = false, Hidden = false, HelpText = "Policy to decide which is the optimal encoding format.")]
        public BestFormatPolicy BestFormatPolicy { get; set; } = BestFormatPolicy.PreserveSource;

        [Option("preferred-format-32x32x32", Required = false, Hidden = false, HelpText = "Policy to decide which is the optimal encoding format for the 32x32x32bpp frame.")]
        public BestFormatPolicy BestFormatPolicy32x32x32 { get; set; } = BestFormatPolicy.Inherited;

        [Option("large-image-pixel-threshold", Required = false, Hidden = true, HelpText = "Determines how many pixels (width) are required to classify an image as large.")]
        public int LargeImagePixelThreshold { get; set; } = 128;

        [Option("png-size-penalty", Required = false, Hidden = true, HelpText = "Add a small size penalty to PNG to account for its worse decoding speed.")]
        public int PngSizePenalty { get; set; } = 0;

        [Option("allow-downsample", Required = false, Hidden = true, HelpText = "Allow a bitmap to be encoded at a smaller bitdepth, if it can be done losslessly.")]
        public bool AllowDownsample { get; set; } = true;

        [Option("allow-downscale", Required = false, Hidden = true, HelpText = "Allow a bitmap to be encoded at a smaller pixel size, if it can be done losslessly.")]
        public bool AllowDownscale { get; set; } = true;

        [Option("allow-truncated-palette", Required = false, Hidden = true, HelpText = "Save a few bytes by reducing the palette size.")]
        public StrictnessPolicy AllowPaletteTruncation { get; set; } = StrictnessPolicy.PreserveSource;

        [Option("emit-bmp-all-encodings", Required = false, Hidden = true, HelpText = "Emit BMPs for each possible encoding of each frame.")]
        public bool EmitBmpFilesForAllEncodings { get; set; } = false;

        [Option("png-tool", Required = false, Hidden = false, HelpText = "Tool to munge PNG files.")]
        public string PngToolPath { get; set; }
    }
}
