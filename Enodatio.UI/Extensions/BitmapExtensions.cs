using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Enodatio.UI.Extensions;

public static class BitmapExtensions
{
    public static BitmapImage ToBitmapImage(this Bitmap bitmap, ImageFormat? imageFormat = null)
    {
        using var memory = new MemoryStream();

        var format = imageFormat ?? (Equals(bitmap.RawFormat, ImageFormat.MemoryBmp) ? ImageFormat.Bmp : bitmap.RawFormat);
        bitmap.Save(memory, format);
        memory.Position = 0;
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    public static BitmapImage ToGrayScaleBitmapImage(this Bitmap bitmap)
    {
        var result = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format8bppIndexed);

        var resultPalette = result.Palette;

        for (var i = 0; i < 256; i++)
        {
            resultPalette.Entries[i] = Color.FromArgb(255, i, i, i);
        }

        result.Palette = resultPalette;

        var data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

        var bytes = new byte[data.Height * data.Stride];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var c = bitmap.GetPixel(x, y);
                var rgb = (byte) ((c.R + c.G + c.B) / 3);

                bytes[y * data.Stride + x] = rgb;
            }
        }

        Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);

        result.UnlockBits(data);

        return result.ToBitmapImage();
    }
}

public static class BitmapImageExtensions
{
    public static Bitmap ToBitmap(this BitmapImage bitmapImage)
    {
        using var memoryStream = new MemoryStream();
        var bitmapEncoder = new BmpBitmapEncoder();
        bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapImage));
        bitmapEncoder.Save(memoryStream);

        var bitmap = new Bitmap(memoryStream);
        return bitmap;
    }
}