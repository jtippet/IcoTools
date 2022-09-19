using CommandLine;
using Ico.Binary;
using Ico.Codecs;
using Ico.Console;
using Ico.Model;
using Ico.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Ico.Cat
{
    internal class IcoCat
    {
        private static ConsoleErrorReporter Reporter { get; } = new ConsoleErrorReporter();

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult((CommandLineOptions opts) => Run(opts), _ => 1);
        }

        private static int Run(CommandLineOptions opts)
        {
            if (opts.EncodingOverrideString != null)
            {
                switch (opts.EncodingOverrideString.ToLowerInvariant())
                {
                    case "bitmap":
                    case "bmp":
                        opts.EncodingOverride = IcoEncodingType.Bitmap;
                        break;
                    case "png":
                        break;
                    default:
                        Reporter.ErrorLine(IcoErrorCode.UnsupportedCodec, $"Invalid encoding type: {opts.EncodingOverrideString}");
                        return 1;
                }
            }

            if (opts.BitmapEncodingString != null)
            {
                switch (opts.BitmapEncodingString.ToLowerInvariant())
                {
                    case "indexed1":
                        opts.BitmapEncodingOverride = BitmapEncoding.Pixel_indexed1;
                        break;
                    case "indexed2":
                        opts.BitmapEncodingOverride = BitmapEncoding.Pixel_indexed2;
                        break;
                    case "indexed4":
                        opts.BitmapEncodingOverride = BitmapEncoding.Pixel_indexed4;
                        break;
                    case "indexed8":
                        opts.BitmapEncodingOverride = BitmapEncoding.Pixel_indexed8;
                        break;
                    case "rgb555":
                        opts.BitmapEncodingOverride = BitmapEncoding.Pixel_rgb15;
                        break;
                    case "rgb888":
                        opts.BitmapEncodingOverride = BitmapEncoding.Pixel_rgb24;
                        break;
                    case "0rgb8888":
                        opts.BitmapEncodingOverride = BitmapEncoding.Pixel_0rgb32;
                        break;
                    case "argb8888":
                        opts.BitmapEncodingOverride = BitmapEncoding.Pixel_argb32;
                        break;
                    default:
                        Reporter.ErrorLine(IcoErrorCode.UnsupportedBitmapEncoding, $"Invalid bitamp encoding: {opts.BitmapEncodingString}");
                        return 1;
                }
            }

            var context = new ParseContext
            {
                DisplayedPath = opts.InputFile,
                FullPath = opts.InputFile,
                GeneratedFrames = new List<IcoFrame>(),
                Reporter = Reporter,
                PngEncoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder
                {
                    CompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel.Level9,
                    ColorType = SixLabors.ImageSharp.Formats.Png.PngColorType.RgbWithAlpha,
                },
            };

            if (File.Exists(opts.InputFile))
            {
                var data = File.ReadAllBytes(opts.InputFile);
                IcoDecoder.DoFile(data, context, frame => context.GeneratedFrames.Add(frame));
            }

            var newFrame = GenerateFrame(opts, context);

            if (opts.FrameIndex.HasValue)
            {
                if (opts.FrameIndex.Value < 1 || opts.FrameIndex.Value - 1 > context.GeneratedFrames.Count)
                {
                    Reporter.ErrorLine(IcoErrorCode.InvalidFrameIndex, $"Cannot insert frame at #{opts.FrameIndex.Value}. The icon has {context.GeneratedFrames.Count} frame(s).", opts.InputFile);
                    return 1;
                }

                context.GeneratedFrames.Insert(opts.FrameIndex.Value - 1, newFrame);
            }
            else
            {
                context.GeneratedFrames.Add(newFrame);
            }

            IcoEncoder.EmitIco(opts.InputFile, context);

            Reporter.PrintHelpUrls();
            return 0;
        }

        private static IcoFrame GenerateFrame(CommandLineOptions opts, ParseContext context)
        {
            var frame = new IcoFrame
            {
                Encoding = new IcoFrameEncoding(),
            };

            var sourceFile = File.ReadAllBytes(opts.SourceImagePath);
            var reader = new ByteReader(sourceFile, Ico.Binary.ByteOrder.LittleEndian);
            var signature = reader.NextUint64();
            var sourceEncoding = PngDecoder.IsProbablyPngFile(ByteOrderConverter.To(Ico.Binary.ByteOrder.NetworkEndian, signature))
                ? IcoEncodingType.Png
                : IcoEncodingType.Bitmap;
            bool canSourceBePreserved = false;

            switch (sourceEncoding)
            {
                case IcoEncodingType.Bitmap:
                    ReadBmpFile(opts, context, frame, sourceFile);
                    break;
                case IcoEncodingType.Png:
                    ReadPngFile(opts, context, frame, sourceFile, out canSourceBePreserved);
                    break;
            }

            frame.Encoding.ActualHeight = (uint)frame.CookedData.Height;
            frame.Encoding.ClaimedHeight = (uint)frame.CookedData.Height;
            frame.Encoding.ActualWidth = (uint)frame.CookedData.Width;
            frame.Encoding.ClaimedWidth = (uint)frame.CookedData.Width;

            var outputEncoding = opts.EncodingOverride ?? sourceEncoding;

            if (outputEncoding != IcoEncodingType.Bitmap && opts.BitmapEncodingOverride.HasValue)
            {
                Reporter.WarnLine(IcoErrorCode.OnlySupportedOnBitmaps, "Ignoring bitmap encoding configuration because we're not emitting a bitmap frame");
            }

            if (outputEncoding != sourceEncoding)
                canSourceBePreserved = false;

            if (canSourceBePreserved || opts.KeepRawFrameData)
            {
                frame.RawData = sourceFile;
            }
            else
            {
                switch (outputEncoding)
                {
                    case IcoEncodingType.Bitmap:
                        WriteBmpFrame(opts, context, frame);
                        break;
                    case IcoEncodingType.Png:
                        WritePngFrame(opts, context, frame);
                        break;
                }
            }

            if (opts.BitDepthOverride.HasValue)
            {
                frame.Encoding.ClaimedBitDepth = (uint)opts.BitDepthOverride.Value;
            }

            return frame;
        }

        private static void ReadBmpFile(CommandLineOptions opts, ParseContext context, IcoFrame frame, byte[] sourceFile)
        {
            using (var stream = new MemoryStream(sourceFile))
            {
                var decoder = new SixLabors.ImageSharp.Formats.Bmp.BmpDecoder();
                frame.CookedData = decoder.Decode<Rgba32>(new Configuration(), stream, CancellationToken.None);
            }

            frame.Encoding.Type = IcoEncodingType.Bitmap;
        }

        private static void WriteBmpFrame(CommandLineOptions opts, ParseContext context, IcoFrame frame)
        {
            var encoding = opts.BitmapEncodingOverride ?? BmpUtil.GetIdealBitmapEncoding(frame.CookedData, hasIcoMask: true);

            if (opts.MaskImagePath != null)
            {
                var maskSource = File.ReadAllBytes(opts.MaskImagePath);
                using (var stream = new MemoryStream(maskSource))
                {
                    var decoder = new SixLabors.ImageSharp.Formats.Bmp.BmpDecoder();
                    var mask = decoder.Decode<Rgba32>(new Configuration(), stream, CancellationToken.None);
                    if (mask.Width != frame.CookedData.Width || mask.Height != frame.CookedData.Height)
                    {
                        Reporter.ErrorLine(
                            IcoErrorCode.BitmapMaskWrongDimensions,
                            $"The mask's dimentions {mask.Height}x{mask.Width} don't match "
                            + $"the frame dimensions {frame.CookedData.Height}x{frame.CookedData.Width}.",
                            opts.MaskImagePath);
                        throw new ArgumentException();
                    }

                    for (var x = 0; x < mask.Width; x++)
                    {
                        for (var y = 0; y < mask.Height; y++)
                        {
                            if (mask[x, y].PackedValue != 0xffffffff && mask[x, y].PackedValue != 0x000000ff)
                            {
                                Reporter.ErrorLine(
                                    IcoErrorCode.BitampMaskWrongColors,
                                    $"The mask must be comprised entirely of black and white pixels (where black means transparent).",
                                    opts.MaskImagePath);
                                throw new ArgumentException();
                            }
                        }
                    }

                    frame.Mask = BmpUtil.CreateMaskFromImage(mask, blackIsTransparent: true);
                }
            }
            else
            {
                frame.Mask = BmpUtil.CreateMaskFromImage(frame.CookedData, blackIsTransparent: false);
            }

            frame.RawData = BmpEncoder.EncodeBitmap(context, encoding, BmpEncoder.Dialect.Ico, frame);
            if (frame.RawData == null)
            {
                Reporter.ErrorLine(context.LastEncodeError, $"Cannot encode the source image as a bitmap of type: {encoding}. Try reducing the number of colors, or changing the bitmap encoding.", opts.SourceImagePath);
                throw new ArgumentException();
            }

            frame.Encoding.ClaimedBitDepth = (uint)BmpUtil.GetBitDepthForPixelFormat(encoding);
            frame.Encoding.ActualBitDepth = frame.Encoding.ClaimedBitDepth;
        }

        private static void ReadPngFile(CommandLineOptions opts, ParseContext context, IcoFrame frame, byte[] sourceFile, out bool canSourceBePreserved)
        {
            using (var stream = new MemoryStream(sourceFile))
            {
                var decoder = new SixLabors.ImageSharp.Formats.Png.PngDecoder();
                frame.CookedData = decoder.Decode<Rgba32>(new Configuration(), stream, CancellationToken.None);
            }

            var encoding = PngDecoder.GetPngFileEncoding(new Memory<byte>(sourceFile));

            frame.Encoding.Type = IcoEncodingType.Png;

            frame.Encoding.ClaimedBitDepth = 32;
            frame.Encoding.ActualBitDepth = 32;

            canSourceBePreserved = encoding.ColorType == PngColorType.RGBA && encoding.BitsPerChannel == 8;
        }

        private static void WritePngFrame(CommandLineOptions opts, ParseContext context, IcoFrame frame)
        {
            using (var output = new MemoryStream())
            {
                frame.CookedData.SaveAsPng(output, context.PngEncoder);

                output.Flush();
                frame.RawData = output.GetBuffer();
                frame.Encoding.ClaimedBitDepth = 32;
                frame.Encoding.ActualBitDepth = 32;
            }
        }
    }
}
