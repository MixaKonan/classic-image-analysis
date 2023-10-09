using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Enodatio.Logic.Models;

namespace Enodatio.Logic.Processors;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class DifferenceProcessor
{
    public Bitmap GetDifference(Bitmap first, Bitmap second, int threshold)
    {
        if (first is null || second is null)
        {
            throw new ArgumentException("Null argument(s).");
        }

        if (first.Size != second.Size)
        {
            throw new Exception("Can't process images with different sizes.");
        }

        var differentPixels = new List<PixelInfo>();
        for (var row = 0; row < first.Height; row++)
        {
            for (var column = 0; column < first.Width; column++)
            {
                var firstImagePixel = first.GetPixel(column, row);
                var secondImagePixel = second.GetPixel(column, row);

                var firstImagePixelIntensity = (PixelInfo.RedGrayscaleWeight * firstImagePixel.R)
                                               + (PixelInfo.GreenGrayscaleWeight * firstImagePixel.G)
                                               + (PixelInfo.BlueGrayscaleWeight * firstImagePixel.B);

                var secondImagePixelIntensity = (PixelInfo.RedGrayscaleWeight * secondImagePixel.R)
                                                + (PixelInfo.GreenGrayscaleWeight * secondImagePixel.G)
                                                + (PixelInfo.BlueGrayscaleWeight * secondImagePixel.B);

                if (Math.Abs(firstImagePixelIntensity - secondImagePixelIntensity) > threshold)
                {
                    var isAdditionalPixelIntensityInFirstPicture = firstImagePixelIntensity < secondImagePixelIntensity;

                    var color = isAdditionalPixelIntensityInFirstPicture
                        ? Color.FromArgb(firstImagePixel.R, firstImagePixel.G, firstImagePixel.B)
                        : Color.FromArgb(secondImagePixel.R, secondImagePixel.G, secondImagePixel.B);

                    differentPixels.Add(new PixelInfo(column, row, color));
                }
            }
        }

        var differenceImage = GetDifference(first, differentPixels);

        return differenceImage;
    }

    private static Bitmap GetDifference(Bitmap image, List<PixelInfo> differenceCoordinates)
    {
        var clone = (Bitmap) image.Clone();

        using (var graphics = Graphics.FromImage(clone))
        {
            using (var solidBrush = new SolidBrush(Color.White))
            {
                graphics.FillRectangle(solidBrush, 0, 0, clone.Width, clone.Height);
            }

            foreach (var coordinatePair in differenceCoordinates)
            {
                clone.SetPixel(coordinatePair.X, coordinatePair.Y, coordinatePair.Color);
            }

            return clone;
        }
    }
}