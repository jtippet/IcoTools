using Ico.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ico.Codecs
{
    public static class BmpUtil
    {
        public static bool IsAlphaSignificant(IcoFrame source)
        {
            if (IsAnyPartialTransparency(source.CookedData))
            {
                return true;
            }

            for (var x = 0; x < source.CookedData.Width; x++)
            {
                for (var y = 0; y < source.CookedData.Height; y++)
                {
                    var alpha = source.CookedData[x, y].A;
                    var mask = source.Mask[x, y];

                    // If the alpha doesn't agree with the mask, we need to keep it.
                    if (alpha == 0 != mask)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsAnyPartialTransparency(Image<Rgba32> image)
        {
            for (var x = 0; x < image.Width; x++)
            {
                for (var y = 0; y < image.Height; y++)
                {
                    var alpha = image[x, y].A;
                    if (alpha != 0 && alpha != 255)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsAnyAlphaChannel(Image<Rgba32> image)
        {
            for (var x = 0; x < image.Width; x++)
            {
                for (var y = 0; y < image.Height; y++)
                {
                    var alpha = image[x, y].A;
                    if (alpha != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
