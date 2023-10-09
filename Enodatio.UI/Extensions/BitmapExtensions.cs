using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

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