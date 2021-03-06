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
using System.Linq;
using System.Text;

namespace Ico.Cut
{
    internal class IcoCut
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

                ExceptionWrapper.Try(() => DoCutFrames(context, opts), context, Reporter);
            }

            Reporter.PrintHelpUrls();

            return 0;
        }

        private static void DoCutFrames(ParseContext context, CommandLineOptions opts)
        {
            var length = new FileInfo(context.FullPath).Length;
            if (length > FileFormatConstants.MaxIcoFileSize)
            {
                Reporter.WarnLine(IcoErrorCode.FileTooLarge, $"Skipping file because it is unusually large ({length} bytes).", context.DisplayedPath);
                return;
            }

            var data = File.ReadAllBytes(context.FullPath);
            IcoDecoder.DoFile(data, context, frame => context.GeneratedFrames.Add(frame));

            var originalNumFrames = context.GeneratedFrames.Count;

            var indexes = from i in opts.FrameNumbersToDelete
                          orderby i descending
                          group i by i into g
                          select g.First();

            foreach (var i in indexes)
            {
                if (i > context.GeneratedFrames.Count)
                {
                    Reporter.WarnLine(IcoErrorCode.InvalidFrameIndex, $"Can't remove frame #{i} because the file only has {originalNumFrames}.", context.DisplayedPath);
                }
                else if (i < 1)
                {
                    Reporter.WarnLine(IcoErrorCode.InvalidFrameIndex, $"#{i} is not a valid frame number.", context.DisplayedPath);
                }
                else
                {
                    context.GeneratedFrames.RemoveAt(i - 1);
                }
            }

            if (opts.SizesToDelete != null)
            {
                context.GeneratedFrames.RemoveAll(f => opts.SizesToDelete.Any(s => s == f.Encoding.ClaimedHeight || s == f.Encoding.ClaimedWidth));
            }

            if (opts.BitDepthsToDelete != null)
            {
                context.GeneratedFrames.RemoveAll(f => opts.BitDepthsToDelete.Any(s => s == f.Encoding.ClaimedBitDepth));
            }

            if (!context.GeneratedFrames.Any())
            {
                Reporter.WarnLine(IcoErrorCode.ZeroFrames, $"There are no frames remaining; this file will be unreadable by most apps.", context.DisplayedPath);
            }

            IcoEncoder.EmitIco(context.FullPath, context);

            Reporter.InfoLine($"Removed {originalNumFrames - context.GeneratedFrames.Count} frame(s).", context.DisplayedPath);
        }
    }
}
