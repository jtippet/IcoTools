using CommandLine;
using Ico.Codecs;
using Ico.Console;
using Ico.Model;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.FileSystemGlobbing;
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
                };

                try
                {
                    DoCutFrames(context, opts);
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

        private static void DoCutFrames(ParseContext context, CommandLineOptions opts)
        {
            var length = new FileInfo(context.FullPath).Length;
            if (length > FileFormatConstants.MaxIcoFileSize)
            {
                Reporter.WarnLine($"Skipping file because it is unusually large ({length} bytes).", context.DisplayedPath);
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
                    Reporter.WarnLine($"Can't remove frame #{i} because the file only has {originalNumFrames}.", context.DisplayedPath);
                }
                else if (i < 1)
                {
                    Reporter.WarnLine($"#{i} is not a valid frame number.", context.DisplayedPath);
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
                Reporter.WarnLine($"There are no frames remaining; this file will be unreadable by most apps.", context.DisplayedPath);
            }

            IcoEncoder.EmitIco(context.FullPath, context);

            Reporter.InfoLine($"Removed {originalNumFrames - context.GeneratedFrames.Count} frame(s).", context.DisplayedPath);
        }
    }
}
