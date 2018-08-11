# IcoTools
Tools to cope with the .ICO file format.

You'll need to install dotnet core 2.1, if you haven't already got it.

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
