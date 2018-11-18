using CommandLine;
using Ico.Codecs;
using Ico.Model;
using Microsoft.Extensions.FileSystemGlobbing;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Ico
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult(
                (CommandLineOptions opts) => Run(opts),
                _ => 1);
        }

        private static int Run(CommandLineOptions opts)
        {
            var context = new ParseContext
            {
                PngEncoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder
                {
                    CompressionLevel = 9,
                    ColorType = SixLabors.ImageSharp.Formats.Png.PngColorType.RgbWithAlpha,
                },

                MaskedImagePixelEmitOptions = opts.MaskedImagePixelEmitOptions,
                AllowPaletteTruncation = opts.AllowPaletteTruncation,
            };

            var matcher = new Matcher();

            foreach (var glob in opts.Inputs)
            {
                matcher.AddInclude(glob);
            }

            var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
            var files = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(cwd));
            if (!files.HasMatches)
            {
                System.Console.WriteLine("No files matched the inputs.");
                return 2;
            }

            foreach (var file in files.Files)
            {
                if (file.Path.EndsWith(".crushed.ico"))
                {
                    continue;
                }

                System.Console.WriteLine($"File: [{file.Path}]");

                context.FullPath = file.Path;
                context.DisplayedPath = file.Stem;
                context.GeneratedFrames = new List<IcoFrame>();
                DoFile(context, opts);
            }

            return 0;
        }

        private static void DoFile(ParseContext context, CommandLineOptions opts)
        {
            byte[] data = null;

            try
            {
                if (!opts.AllowGiantFiles)
                {
                    var length = new FileInfo(context.FullPath).Length;
                    if (length > FileFormatConstants.MaxIcoFileSize)
                    {
                        System.Console.WriteLine($"Skipping file \"{context.DisplayedPath}\", because it is unusually large ({length} bytes).  Re-run with --allow-giant-inputs to bypass this safety.");
                        return;
                    }
                }

                data = File.ReadAllBytes(context.FullPath);
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Exception {e} while reading file \"{context.DisplayedPath}\"");
                return;
            }

            try
            {
                DoFile(data, context, opts);
            }
            catch (InvalidIcoFileException e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private static void DoFile(byte[] data, ParseContext context, CommandLineOptions opts)
        {
            IcoDecoder.DoFile(data, context, frame => ProcessFrame(context, opts, frame));

            var outputPath = opts.OverwriteInputFiles
                ? context.FullPath
                : $"{context.FullPath}.crushed.ico";

            IcoEncoder.EmitIco(outputPath, context);
        }

        private static void ProcessFrame(ParseContext context, CommandLineOptions opts, IcoFrame source)
        {
            source.Encoding.ActualHeight = (uint)source.CookedData.Height;
            source.Encoding.ActualWidth = (uint)source.CookedData.Width;

            var frame = GetBestEncodingForFrame(context, opts, source);
            context.GeneratedFrames.Add(frame);

            if (opts.EmitBmpFilesForAllEncodings)
            {
                EmitAllPossibleEncodings(context, source);
            }
        }

        private static readonly BitmapEncoding[] _allEncodings = new BitmapEncoding[]
        {
            BitmapEncoding.Pixel_indexed1,
            BitmapEncoding.Pixel_indexed2,
            BitmapEncoding.Pixel_indexed4,
            BitmapEncoding.Pixel_indexed8,
            BitmapEncoding.Pixel_rgb15,
            BitmapEncoding.Pixel_rgb24,
            BitmapEncoding.Pixel_0rgb32,
            BitmapEncoding.Pixel_argb32
        };

        private static IcoFrame GetBestEncodingForFrame(ParseContext context, CommandLineOptions opts, IcoFrame source)
        {
            var result = new IcoFrameEncoding
            {
                ClaimedBitDepth = source.Encoding.ClaimedBitDepth,
                ClaimedHeight = source.Encoding.ClaimedHeight,
                ClaimedWidth = source.Encoding.ClaimedWidth,
                ActualHeight = (uint)source.CookedData.Height,
                ActualWidth = (uint)source.CookedData.Width,
            };

            byte[] png = null;
            byte[] bitmap = null;

            var policy = opts.BestFormatPolicy;

            if (policy == BestFormatPolicy.Inherited)
            {
                policy = BestFormatPolicy.PreserveSource;
            }

            if (source.Encoding.ClaimedHeight == 16 &&
                source.Encoding.ClaimedWidth == 16 &&
                source.Encoding.ClaimedBitDepth == 32 &&
                opts.BestFormatPolicy16x16x32 != BestFormatPolicy.Inherited)
            {
                policy = opts.BestFormatPolicy16x16x32;
            }

            if (source.Encoding.ClaimedHeight == 32 &&
                source.Encoding.ClaimedWidth == 32 &&
                source.Encoding.ClaimedBitDepth == 32 &&
                opts.BestFormatPolicy32x32x32 != BestFormatPolicy.Inherited)
            {
                policy = opts.BestFormatPolicy32x32x32;
            }

            if (policy != BestFormatPolicy.AlwaysBmp &&
                (source.Encoding.ClaimedBitDepth == 32 || opts.BestFormatPolicy == BestFormatPolicy.AlwaysPng))
            {
                png = EncodePng(source, context);

                if (opts.PngToolPath != null)
                {
                    png = ReprocessPngFile(png, context, opts);
                }
            }

            if (opts.BestFormatPolicy != BestFormatPolicy.AlwaysPng)
            {
                foreach (var encoding in _allEncodings)
                {
                    if (encoding == BitmapEncoding.Pixel_indexed2)
                    {
                        switch (opts.Emit2BitBitmaps)
                        {
                            case StrictnessPolicy.Compliant:
                                continue;
                            case StrictnessPolicy.PreserveSource:
                                if (source.Encoding.PixelFormat != BitmapEncoding.Pixel_indexed2)
                                {
                                    continue;
                                }

                                break;
                            case StrictnessPolicy.Loose:
                                break;
                        }
                    }

                    if (encoding == BitmapEncoding.Pixel_rgb24)
                    {
                        switch (opts.Emit24BitBitmaps)
                        {
                            case StrictnessPolicy.Compliant:
                                continue;
                            case StrictnessPolicy.PreserveSource:
                                if (source.Encoding.PixelFormat != BitmapEncoding.Pixel_rgb24)
                                {
                                    continue;
                                }

                                break;
                            case StrictnessPolicy.Loose:
                                break;
                        }
                    }

                    if (source.Encoding.PixelFormat != encoding)
                    {
                        if (!opts.AllowDownsample)
                        {
                            continue;
                        }

                        // Going to rgb15 can lose some color data, due to rounding.  Avoid unless the source
                        // was already in rgb15.
                        if (encoding == BitmapEncoding.Pixel_rgb15)
                        {
                            continue;
                        }

                        // Don't lose the per-pixel alpha channel, if one was in the source.
                        if (source.Encoding.PixelFormat == BitmapEncoding.Pixel_argb32 && BmpUtil.IsAlphaSignificant(source))
                        {
                            continue;
                        }
                    }

                    bitmap = BmpEncoder.EncodeBitmap(context, encoding, BmpEncoder.Dialect.Ico, source);
                    if (bitmap != null)
                    {
                        result.PixelFormat = encoding;
                        break;
                    }
                }
            }

            switch (opts.BestFormatPolicy)
            {
                case BestFormatPolicy.PreserveSource:
                    result.Type = source.Encoding.Type;
                    break;
                case BestFormatPolicy.MinimizeStorage:
                    result.Type = (bitmap.Length < ((png?.Length ?? 0) + opts.PngSizePenalty))
                        ? IcoEncodingType.Bitmap
                        : IcoEncodingType.Png;
                    break;
                case BestFormatPolicy.PngLargeImages:
                    result.Type = (source.CookedData.Width >= opts.LargeImagePixelThreshold)
                        ? IcoEncodingType.Bitmap
                        : IcoEncodingType.Png;
                    break;
                case BestFormatPolicy.AlwaysPng:
                    result.Type = IcoEncodingType.Png;
                    break;
                case BestFormatPolicy.AlwaysBmp:
                    result.Type = IcoEncodingType.Bitmap;
                    break;
                default:
                    break;
            }

            if (png == null && result.Type == IcoEncodingType.Png)
            {
                result.Type = IcoEncodingType.Bitmap;
            }

            var finalData = (result.Type == IcoEncodingType.Bitmap) ? bitmap : png;

            return new IcoFrame { Encoding = result, RawData = finalData };
        }

        private static byte[] ReprocessPngFile(byte[] png, ParseContext context, CommandLineOptions opts)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), "IcoCrush" + Guid.NewGuid().ToString("N") + ".png");
            File.WriteAllBytes(tempFilePath, png);

            var tokens = opts.PngToolPath.Split(' ').ToList();
            var exe = tokens[0];
            tokens.RemoveAt(0);
            tokens.Add(tempFilePath);

            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.Join(' ', tokens),
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            System.Console.WriteLine(info.FileName);
            System.Console.WriteLine(info.Arguments);
            using (var process = Process.Start(info))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"PNG process returned error code {process.ExitCode}.  Process: {info.FileName} {info.Arguments}");
                }
            }

            var result = File.ReadAllBytes(tempFilePath);
            File.Delete(tempFilePath);

            var outputEncoding = PngDecoder.GetPngFileEncoding(new Memory<byte>(result));

            if (outputEncoding.ColorType != PngColorType.RGBA)
            {
                System.Console.WriteLine($"Warning: After running the PNG through the external tool, the color type is: {outputEncoding.ColorType}.  ICO files require color type RGBA for embedded PNGs.");
            }
            else if (outputEncoding.BitsPerChannel != 8)
            {
                System.Console.WriteLine($"Warning: After running the PNG through the external tool, the color depth is: {outputEncoding.BitsPerChannel * 4}-bit.  ICO files require 32-bit color depth for embedded PNGs.");
            }

            return result;
        }

        private static void EmitAllPossibleEncodings(ParseContext context, IcoFrame source)
        {
            byte[] png;

            var pathPrefix = $"{context.FullPath}.{context.ImageDirectoryIndex:D2}.";

            using (var stream = new MemoryStream())
            {
                source.CookedData.SaveAsPng(stream, context.PngEncoder);

                stream.Flush();
                png = stream.GetBuffer();
                var pngPath = $"{pathPrefix}png";
                File.WriteAllBytes(pngPath, stream.GetBuffer());
            }

            var bitmap1 = BmpEncoder.EncodeBitmap(context, BitmapEncoding.Pixel_indexed1, BmpEncoder.Dialect.Bmp, source);
            if (bitmap1 != null)
            {
                var bmp1Path = $"{pathPrefix}01bpp.bmp";
                File.WriteAllBytes(bmp1Path, bitmap1);
            }

            var bitmap4 = BmpEncoder.EncodeBitmap(context, BitmapEncoding.Pixel_indexed4, BmpEncoder.Dialect.Bmp, source);
            if (bitmap4 != null)
            {
                var bmp4Path = $"{pathPrefix}04bpp.bmp";
                File.WriteAllBytes(bmp4Path, bitmap4);
            }

            var bitmap8 = BmpEncoder.EncodeBitmap(context, BitmapEncoding.Pixel_indexed8, BmpEncoder.Dialect.Bmp, source);
            if (bitmap8 != null)
            {
                var bmp8Path = $"{pathPrefix}08bpp.bmp";
                File.WriteAllBytes(bmp8Path, bitmap8);
            }

            var bitmap16 = BmpEncoder.EncodeBitmap(context, BitmapEncoding.Pixel_rgb15, BmpEncoder.Dialect.Bmp, source);
            if (bitmap16 != null)
            {
                var bmp16Path = $"{pathPrefix}16bpp.bmp";
                File.WriteAllBytes(bmp16Path, bitmap16);
            }

            var bitmap32 = BmpEncoder.EncodeBitmap(context, BitmapEncoding.Pixel_0rgb32, BmpEncoder.Dialect.Bmp, source);
            if (bitmap32 != null)
            {
                var bmp32Path = $"{pathPrefix}32bpp0.bmp";
                File.WriteAllBytes(bmp32Path, bitmap32);
            }

            var bitmap32a = BmpEncoder.EncodeBitmap(context, BitmapEncoding.Pixel_argb32, BmpEncoder.Dialect.Bmp, source);
            if (bitmap32a != null)
            {
                var bmp32Path = $"{pathPrefix}32Abpp.bmp";
                File.WriteAllBytes(bmp32Path, bitmap32a);
            }
        }

        private static byte[] EncodePng(IcoFrame source, ParseContext context)
        {
            using (var stream = new MemoryStream())
            {
                using (var masked = source.CookedData.Clone())
                {
                    for (var x = 0; x < masked.Width; x++)
                    {
                        for (var y = 0; y < masked.Height; y++)
                        {
                            if (source.Mask[x, y] && masked[x, y].A != 0)
                            {
                                var c = masked[x, y];
                                c.A = 0;
                                masked[x, y] = c;
                            }
                        }
                    }

                    masked.SaveAsPng(stream, context.PngEncoder);
                }

                stream.Flush();
                return stream.GetBuffer();
            }
        }
    }
}
