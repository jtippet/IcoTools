using CommandLine;
using Ico.Codecs;
using Ico.Console;
using Ico.Host;
using Ico.Model;
using Ico.Validation;
using Microsoft.DotNet.Cli.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace Ico
{
    internal class IcoNag
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

            foreach (var warning in opts.DisabledWarnings)
            {
                var munged = warning;
                if (munged.StartsWith("ico", StringComparison.InvariantCultureIgnoreCase))
                    munged = warning.Substring(3);

                int code;
                if (!int.TryParse(munged, out code) ||
                    null == Enum.GetName(typeof(IcoErrorCode), code))
                {
                    Reporter.ErrorLine(IcoErrorCode.NoError, $"Unknown warning number \"{warning}\"");
                    return 1;
                }

                Reporter.WarningsToIgnore.Add((IcoErrorCode)code);
            }

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

                ExceptionWrapper.Try(() => DoLintFile(context, opts), context, Reporter);
            }

            Reporter.PrintHelpUrls();

            return 0;
        }

        private static void DoLintFile(ParseContext context, CommandLineOptions opts)
        {
            var length = new FileInfo(context.FullPath).Length;
            if (length > FileFormatConstants.MaxIcoFileSize)
            {
                Reporter.WarnLine(IcoErrorCode.FileTooLarge, $"Skipping file because it is unusually large ({length} bytes).", context.DisplayedPath);
                return;
            }

            var data = File.ReadAllBytes(context.FullPath);
            IcoDecoder.DoFile(data, context, frame => DoLintFrame(frame, context, opts));

            for (var i = 0; i < context.GeneratedFrames.Count; i++)
            {
                for (var j = i + 1; j < context.GeneratedFrames.Count; j++)
                {
                    var a = context.GeneratedFrames[i];
                    var b = context.GeneratedFrames[j];
                    CheckDuplicateFrames(a, i, b, j, context.DisplayedPath);
                }
            }
        }

        private static void CheckDuplicateFrames(IcoFrame a, int aIndex, IcoFrame b, int bIndex, string fileName)
        {
            if (a.Encoding.ClaimedBitDepth != b.Encoding.ClaimedBitDepth ||
                a.Encoding.ActualBitDepth != b.Encoding.ActualBitDepth)
            {
                return;
            }

            if (a.Encoding.ClaimedHeight != b.Encoding.ClaimedHeight ||
                a.Encoding.ClaimedWidth != b.Encoding.ClaimedWidth)
            {
                return;
            }

            Reporter.WarnLine(IcoErrorCode.DuplicateFrameTypes,
                $"Icon file contains redundant frames.  Frames #{aIndex} and #{bIndex} have the same height, width, and color depth " +
                $"({a.Encoding.ClaimedHeight}x{a.Encoding.ClaimedWidth}@{a.Encoding.ClaimedBitDepth}).  Everyone will ignore the latter frame.",
                fileName);
        }

        private static void DoLintFrame(IcoFrame frame, ParseContext context, CommandLineOptions opts)
        {
            context.GeneratedFrames.Add(frame);
        }
    }
}
