﻿using CommandLine;
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

namespace Ico.Info
{
    internal class IcoInfo
    {
        private static ConsoleErrorReporter Reporter { get; } = new ConsoleErrorReporter();

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult((CommandLineOptions opts) => Run(opts), _ => 1);
        }

        private static int Run(CommandLineOptions opts)
        {
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
                };

                ExceptionWrapper.Try(() => DoPrintFileInfo(context, opts), context, Reporter);
            }

            Reporter.PrintHelpUrls();

            return 0;
        }

        private static void DoPrintFileInfo(ParseContext context, CommandLineOptions opts)
        {
            var length = new FileInfo(context.FullPath).Length;
            if (length > FileFormatConstants.MaxIcoFileSize)
            {
                Reporter.WarnLine(IcoErrorCode.FileTooLarge, $"Skipping file because it is unusually large ({length} bytes).", context.DisplayedPath);
                return;
            }

            System.Console.WriteLine($"File: {context.FullPath}");

            int frameNumber = 0;
            var data = File.ReadAllBytes(context.FullPath);
            IcoDecoder.DoFile(data, context, frame => AddFrame(frame, context, opts, ref frameNumber));

            System.Console.WriteLine();
        }

        private static void AddFrame(IcoFrame frame, ParseContext context, CommandLineOptions opts, ref int frameNumber)
        {
            frameNumber += 1;

            System.Console.WriteLine($"  Frame #{frameNumber}");
            System.Console.WriteLine($"    Encoding:      {(frame.Encoding.Type == IcoEncodingType.Bitmap ? "Bitmap" : "PNG")}");
            if (frame.Encoding.Type == IcoEncodingType.Bitmap)
                System.Console.WriteLine($"    Bitmap type:   {StringFromPixelFormat(frame.Encoding.PixelFormat)}");
            if (frame.Encoding.PaletteSize != 0)
                System.Console.WriteLine($"    Palette size:  {frame.Encoding.PaletteSize}");
            System.Console.WriteLine($"    Bytes on disk: {frame.TotalDiskUsage}");
            System.Console.WriteLine($"                   Actual     Claimed in ICO header");
            System.Console.WriteLine($"    Width:           {frame.Encoding.ActualWidth.ToString().PadLeft(4, ' ')}                      {frame.Encoding.ClaimedWidth.ToString().PadLeft(4, ' ')}");
            System.Console.WriteLine($"    Height:          {frame.Encoding.ActualHeight.ToString().PadLeft(4, ' ')}                      {frame.Encoding.ClaimedHeight.ToString().PadLeft(4, ' ')}");
            System.Console.WriteLine($"    Bit depth:       {frame.Encoding.ActualBitDepth.ToString().PadLeft(4, ' ')}                      {frame.Encoding.ClaimedBitDepth.ToString().PadLeft(4, ' ')}");

            System.Console.WriteLine();
        }

        private static string StringFromPixelFormat(BitmapEncoding pixelFormat)
        {
            switch (pixelFormat)
            {
                case BitmapEncoding.Pixel_indexed1: return "1-bit indexed";
                case BitmapEncoding.Pixel_indexed2: return "2-bit indexed";
                case BitmapEncoding.Pixel_indexed4: return "4-bit indexed";
                case BitmapEncoding.Pixel_indexed8: return "8-bit indexed";
                case BitmapEncoding.Pixel_rgb15: return "15-bit RGB";
                case BitmapEncoding.Pixel_rgb24: return "24-bit RGB";
                case BitmapEncoding.Pixel_0rgb32: return "32-bit 0RGB";
                case BitmapEncoding.Pixel_argb32: return "32-bit ARGB";
                default: return "[unknown]";
            }
        }
    }
}
