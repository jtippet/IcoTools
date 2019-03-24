using CommandLine;
using Ico.Codecs;
using Ico.Console;
using Ico.Host;
using Ico.Model;
using Ico.Validation;
using Microsoft.DotNet.Cli.Utils;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ico.Extract
{
    internal class IcoExtract
    {
        private static ConsoleErrorReporter Reporter { get; } = new ConsoleErrorReporter();

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult((CommandLineOptions opts) => Run(opts), _ => 1);
        }

        private static int Run(CommandLineOptions opts)
        {
            CommandContext.SetVerbose(opts.Verbose);

            var files = FileGlobExpander.Expand(opts.Inputs, Reporter);
            if (files == null)
                return 2;

            foreach (var file in files)
            {
                Reporter.VerboseLine($"Loading file: [{file.FullName}]");

                var context = new ParseContext
                {
                    DisplayedPath = file.Name,
                    FullPath = file.FullName,
                    GeneratedFrames = new List<IcoFrame>(),
                    Reporter = Reporter,
                    PngEncoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder
                    {
                        CompressionLevel = 9,
                        ColorType = SixLabors.ImageSharp.Formats.Png.PngColorType.RgbWithAlpha,
                    },
                };

                ExceptionWrapper.Try(() => DoExtractFile(context, opts), context, Reporter);
            }

            return 0;
        }

        private static void DoExtractFile(ParseContext context, CommandLineOptions opts)
        {
            var length = new FileInfo(context.FullPath).Length;
            if (length > FileFormatConstants.MaxIcoFileSize)
            {
                Reporter.WarnLine(IcoErrorCode.FileTooLarge, $"Skipping file because it is unusually large ({length} bytes).", context.DisplayedPath);
                return;
            }

            var data = File.ReadAllBytes(context.FullPath);
            IcoDecoder.DoFile(data, context, frame => AddFrame(frame, context, opts));
        }

        private static void AddFrame(IcoFrame frame, ParseContext context, CommandLineOptions opts)
        {
            var outputEncoding = GetOutputEncoding(frame, opts);
            var extension = outputEncoding == IcoEncodingType.Bitmap ? "bmp" : "png";
            var outputFilename = $"{context.FullPath}.{context.ImageDirectoryIndex:D2}.{frame.Encoding.ActualWidth}x{frame.Encoding.ActualHeight}x{frame.Encoding.ActualBitDepth}.{extension}";

            if (File.Exists(outputFilename) && opts.ForceOverwrite == false)
            {
                Reporter.WarnLine(IcoErrorCode.FileExists, $"Not overwriting existing file {outputFilename}.  Use --force-overwrite to change behavior.");
                return;
            }

            Reporter.VerboseLine($"Writing frame to {outputFilename}...");

            switch (outputEncoding)
            {
                case IcoEncodingType.Bitmap:
                    var bitmap = BmpEncoder.EncodeBitmap(context, frame.Encoding.PixelFormat, BmpEncoder.Dialect.Bmp, frame);
                    File.WriteAllBytes(outputFilename, bitmap);
                    break;

                case IcoEncodingType.Png:
                    if (frame.Encoding.Type == IcoEncodingType.Png)
                    {
                        File.WriteAllBytes(outputFilename, frame.RawData);
                    }
                    else
                    {
                        using (var stream = new MemoryStream())
                        {
                            frame.CookedData.SaveAsPng(stream, context.PngEncoder);

                            stream.Flush();
                            var png = stream.GetBuffer();
                            File.WriteAllBytes(outputFilename, stream.GetBuffer());
                        }
                    }
                    break;
            }
        }

        private static IcoEncodingType GetOutputEncoding(IcoFrame frame, CommandLineOptions opts)
        {
            switch (opts.OutputFormat)
            {
                case OutputFileFormatPreference.AlwaysPng:
                    return IcoEncodingType.Png;
                case OutputFileFormatPreference.AlwaysBmp:
                    return IcoEncodingType.Bitmap;
                case OutputFileFormatPreference.KeepSource:
                    return frame.Encoding.Type;
                default:
                    throw new ArgumentException(nameof(opts.OutputFormat));
            }
        }
    }
}
