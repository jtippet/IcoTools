﻿using CommandLine;
using Ico.Codecs;
using Ico.Console;
using Ico.Model;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.FileSystemGlobbing;
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

            var matcher = new Matcher();

            foreach (var glob in opts.Inputs)
            {
                matcher.AddInclude(glob);
            }

            var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
            var files = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(cwd));
            if (!files.HasMatches)
            {
                Reporter.ErrorLine("No files matched the inputs.");
                return 2;
            }

            foreach (var file in files.Files)
            {
                Reporter.VerboseLine($"Loading file: [{file.Path}]");

                var context = new ParseContext
                {
                    DisplayedPath = file.Stem,
                    FullPath = file.Path,
                    GeneratedFrames = new List<IcoFrame>(),
                    Reporter = Reporter,
                    PngEncoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder
                    {
                        CompressionLevel = 9,
                        ColorType = SixLabors.ImageSharp.Formats.Png.PngColorType.RgbWithAlpha,
                    },
                };

                try
                {
                    DoExtractFile(context, opts);
                }
                catch (InvalidIcoFileException e)
                {
                    var frame = (e.Context?.ImageDirectoryIndex);

                    if (frame != null)
                    {
                        Reporter.ErrorLine(e.Message, e.Context.DisplayedPath, e.Context.ImageDirectoryIndex.Value);
                    }
                    else
                    {
                        Reporter.ErrorLine(e.Message, file.Stem);
                    }
                }
                catch (Exception e)
                {
                    Reporter.ErrorLine(e.ToString(), file.Stem);
                }
            }

            return 0;
        }

        private static void DoExtractFile(ParseContext context, CommandLineOptions opts)
        {
            var length = new FileInfo(context.FullPath).Length;
            if (length > FileFormatConstants.MaxIcoFileSize)
            {
                Reporter.WarnLine($"Skipping file because it is unusually large ({length} bytes).", context.DisplayedPath);
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
                Reporter.WarnLine($"Not overwriting existing file {outputFilename}.  Use --force-overwrite to change behavior.");
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
                    using (var stream = new MemoryStream())
                    {
                        frame.CookedData.SaveAsPng(stream, context.PngEncoder);

                        stream.Flush();
                        var png = stream.GetBuffer();
                        File.WriteAllBytes(outputFilename, stream.GetBuffer());
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