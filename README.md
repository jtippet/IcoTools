# IcoTools
Tools to cope with the .ICO file format.

You'll need to install dotnet core 2.1, if you haven't already got it.

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
    my_cool_icon.ico(2): Warning: Non-black image pixels masked out.
    my_cool_icon.ico(6): Warning: PNG-encoded image with bit depth 4 (expected 32).

## IcoCrush

IcoCrush is an optimization tool that can reduce the size of your .ICO files.
Always back up your icons before running this tool, since it's always possible the tool will lose data.

It's designed to work in conjunction with your favorite PNG optimizer (pngcrush, pngopt, pingo, etc.)

    dotnet IcoCrush.dll -i my_cool_icon.ico --write-in-place --preferred-format MinimizeStorage --png-tool "c:\my\cool\png\optimizer.exe --blah blah"

Note most PNG optimizers will try to reduce the color depth of the PNG.
WIC only supports BGRA32 PNG images in the ICO container, so you'll have to convince your optimizer to emit more colors than might be neccessary.
(For example, if you're using pngcrush, use the "-c 6 -noreduce" options.)
