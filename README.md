# IcoTools
Tools to cope with the ICO file format.

You can dismantle, recombine, and optimize your icons.
These tools use a pure C# parser and generator for icons, so they run on any operating system.
In the UNIX tradition, each tool does one simple thing.

## Example uses:
 * Combine IcoCat + IcoCrush to make optimized ICO files from a collection of source PNG files.
 * Nab all the cool bitmaps from someone's icon using IcoExtract.
 * Integrate IcoNag into your build toolchain, to ensure nobody commits a broken icon.
 * Remove obsolete bitdepths from your icons with IcoCut.
 * Verify all your icons meet a minimum standard of sizes using IcoInfo.

## Dependencies
The only runtime dependency is dotnet core 2.1.
Otherwise, these tools are platform-agnostic, and can run anywhere that dotnet core can run.
Most tools do use the excellent cross-platform ImageSharp library: https://github.com/SixLabors/ImageSharp

## Future work
* I need to organize and rationalize the command-line arguments.
* IcoCrush will, in some cases, emit nonportable ICO files.  There should be warnings when this happens.

# Tools included in this repository

## IcoInfo

IcoInfo prints information about the frames in an ICO file.
For example:

    dotnet icoinfo.dll -i my_cool_icon.ico
    File: my_cool_icon.ico
      Frame #1
        Encoding:      PNG
                       Actual     Claimed in ICO header
        Width:            256                       256
        Height:           256                       256
        Bit depth:         32                        32
    
      Frame #2
        Encoding:      Bitmap
        Bitmap type:   32-bit ARGB
                       Actual     Claimed in ICO header
        Width:             32                        32
        Height:            32                        32
        Bit depth:         32                        32
    
      Frame #3
        Encoding:      Bitmap
        Bitmap type:   4-bit RGB
                       Actual     Claimed in ICO header
        Width:             16                        16
        Height:            16                        16
        Bit depth:          4                         4

Note that due to the vagaries of the ICO file format, the width and height of a frame are encoded twice.
The first is in the ICO header, and would typically be used when selecting which frame to display.
The second is in the bitmap or PNG data, and describes the actual size of the image.
Hopefully these two are the same, but they aren't always.
In any case, this tool prints both sizes, so you can decide for yourself what to do.

## IcoExtract

IcoExtract saves each frame as a separate file.
For example:

    dotnet icoextract.dll -i my_cool_icon.ico -v
    Writing frame to my_cool_icon.ico.00.256x256x32.png...
    Writing frame to my_cool_icon.ico.01.64x64x32.png...
    Writing frame to my_cool_icon.ico.04.32x32x32.png...
    Writing frame to my_cool_icon.ico.07.16x16x4.png...

Note that, by default, bitmap frames are always converted to PNG, which means things don't quite round-trip.
You can override this behavior with `--output-format KeepSource`, but that poses a new problem: the bitmaps that appear in ICOs have nonstandard transparency features.
So most (all?) tools that think they understand BMP files won't understand the BMPs that are extracted from ICOs.

## IcoNag

IcoNag is a linting tool that looks for common errors in .ICO files.
For example:

    dotnet iconag.dll -i my_cool_icon.ico
    my_cool_icon.ico(2): Warning ICO221: Non-black image pixels masked out.
    my_cool_icon.ico(6): Warning ICO240: PNG-encoded image with bit depth 4 (expected 32).

Since most of the warning messages are both terse and technical, this github repository contains a warning explainer for humans who haven't memorized the entire ICO file specification.
For example, here's the reference for the first warning in the previous example here: https://github.com/jtippet/IcoTools/wiki/ICO221

## IcoCut

IcoCut can remove frames from an .ICO file.
You can either remove all frames matching a particular size or bit depth, or you can remove specific frames by number.
For example, you can remove all frames of height or width 20px or 24px:

    dotnet icocut.dll -i my_cool_icon.ico -s 20 24
    my_cool_icon.ico(2): Removed 2 frame(s).

## IcoCat

IcoCat can add a frame to an .ICO file.
You can provide the new image from either a PNG or a BMP.
You can also optionally provide an explicit image mask.
If you don't provide a mask, one will be inferred from the image's transparency.
For example:

    dotnet icocat.dll -i my_cool_icon.ico -s new_layer.png

## IcoCrush

IcoCrush is an optimization tool that can reduce the size of your .ICO files.
Always back up your icons before running this tool, since it's always possible the tool will lose data.

It's designed to work in conjunction with your favorite PNG optimizer (pngcrush, pngopt, pingo, etc.)

    dotnet IcoCrush.dll -i my_cool_icon.ico --write-in-place --preferred-format MinimizeStorage --png-tool "c:\my\cool\png\optimizer.exe --blah blah"

Note most PNG optimizers will try to reduce the color depth of the PNG.
WIC only supports BGRA32 PNG images in the ICO container, so you'll have to convince your optimizer to emit more colors than might be neccessary.
(For example, if you're using pngcrush, use the "-c 6 -noreduce" options.)

# Contributing

Bug reports, feature requests, and patches are welcome.
