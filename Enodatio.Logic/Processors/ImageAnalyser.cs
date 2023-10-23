using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Enodatio.Logic.Models;

namespace Enodatio.Logic.Processors;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ImageAnalyser
{
    private const double Sin45 = 0.70710678118;
    private const double Cos45 = 0.70710678118;

    public Dictionary<Range, int> GetPixelCounts(Bitmap image, int pixelIntensitySpan)
    {
        var intervals = GetIntervals(pixelIntensitySpan);
        var pixelCounts = new Dictionary<Range, int>();

        foreach (var interval in intervals)
        {
            pixelCounts[interval] = 0;
        }

        for (var row = 0; row < image.Height; row++)
        {
            for (var column = 0; column < image.Width; column++)
            {
                var pixel = image.GetPixel(column, row);
                var pixelIntensity = (0.299 * pixel.R) + (0.587 * pixel.G) + (0.114 * pixel.B);

                foreach (var interval in intervals)
                {
                    if (pixelIntensity > interval.Start.Value && pixelIntensity <= interval.End.Value)
                    {
                        pixelCounts[interval] = pixelCounts[interval] + 1;
                    }
                }
            }
        }

        return pixelCounts;
    }

    public Dictionary<Range, int> GetPixelCounts(List<PixelInfo> pixels, int pixelIntensitySpan)
    {
        var intervals = GetIntervals(pixelIntensitySpan);
        var pixelCounts = new Dictionary<Range, int>();

        foreach (var interval in intervals)
        {
            pixelCounts[interval] = 0;
        }

        foreach (var pixel in pixels)
        {
            foreach (var interval in intervals)
            {
                if (pixel.GrayScaleIntensity > interval.Start.Value && pixel.GrayScaleIntensity <= interval.End.Value)
                {
                    pixelCounts[interval] = pixelCounts[interval] + 1;
                }
            }
        }

        return pixelCounts;
    }

    private static List<Range> GetIntervals(int span)
    {
        var intervals = new List<Range>();
        var start = 0;
        while (start < 255)
        {
            var end = start + span;
            if (end > 255)
            {
                end = 255;
            }

            intervals.Add(new Range(start, end));
            start += span;
        }

        return intervals;
    }

    public List<Beam> DrawBeamsFromCenter(Bitmap bitmap, int degreeStep, Color color)
    {
        var center = new Point(bitmap.Width / 2, bitmap.Height / 2);

        var endPoints = GetEndPoints(bitmap, degreeStep);

        var beams = new List<Beam>(endPoints.Length);
        foreach (var endPoint in endPoints)
        {
            var beam = new Beam()
            {
                Start = center,
                End = endPoint
            };

            var x = center.X;
            var y = center.Y;

            var w = endPoint.X - center.X;
            var h = endPoint.Y - center.Y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

            if (w < 0) dx1 = -1;
            else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1;
            else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1;
            else if (w > 0) dx2 = 1;

            var longest = Math.Abs(w);
            var shortest = Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1;
                else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            var numerator = longest >> 1;
            for (var i = 0; i <= longest; i++)
            {
                var currentPixel = bitmap.GetPixel(x, y);

                beam.Pixels.Add(new PixelInfo(x, y, Color.FromArgb(currentPixel.R, currentPixel.G, currentPixel.G)));

                bitmap.SetPixel(x, y, color);
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }

            beams.Add(beam);
        }

        return beams;
    }

    private static Point[] GetEndPoints(Bitmap bitmap, int degreeStep)
    {
        var points = new Point[360 / degreeStep];
        var degrees = 90;

        var index = 0;
        while (index < points.Length)
        {
            Point point;
            switch (degrees)
            {
                case 0:
                    point = new Point(bitmap.Width - 1, bitmap.Height / 2);
                    break;
                case 45:
                    point = new Point(bitmap.Width - 1, 1);
                    break;
                case 90:
                    point = new Point(bitmap.Width / 2, 1);
                    break;
                case 135:
                    point = new Point(1, 1);
                    break;
                case 180:
                    point = new Point(1, bitmap.Height / 2);
                    break;
                case 225:
                    point = new Point(1, bitmap.Height - 1);
                    break;
                case 270:
                    point = new Point(bitmap.Width / 2, bitmap.Height - 1);
                    break;
                case 315:
                    point = new Point(bitmap.Width - 1, bitmap.Height - 1);
                    break;
                case > 0 and < 45:
                    point = GetRightTopPoint(bitmap.Width, bitmap.Height, degrees);
                    break;
                case > 45 and < 90:
                    point = GetTopRightPoint(bitmap.Width, degrees);
                    break;
                case > 90 and < 135:
                    point = GetTopLeftPoint(bitmap.Height, degrees);
                    break;
                case > 135 and < 180:
                    point = GetLeftTopPoint(bitmap.Height, degrees);
                    break;
                case > 180 and < 225:
                    point = GetLeftBottomPoint(bitmap.Height, degrees);
                    break;
                case > 225 and < 270:
                    point = GetBottomLeftPoint(bitmap.Width, bitmap.Height, degrees);
                    break;
                case > 270 and < 315:
                    point = GetBottomRightPoint(bitmap.Width, bitmap.Height, degrees);
                    break;
                case > 315 and < 360:
                    point = GetRightBottomQuadrantPoint(bitmap.Width, bitmap.Height, degrees);
                    break;
                default:
                    throw new Exception($"Unable to define appropriate case for {degrees} degrees.");
            }

            points[index] = point;

            degrees = (degrees + degreeStep) % 360;
            index++;
        }

        return points;
    }

    private static Point GetTopRightPoint(int width, int degrees)
    {
        var y = 1;
        var radian = degrees * Math.PI / 180;

        var cos = Math.Cos(radian);

        var bound = (int) (Cos45 * width);
        var x = Utils.Map((int) (cos * width), 0, bound, width / 2, width - 1);

        return new Point(x, y);
    }

    private static Point GetTopLeftPoint(int width, int degrees)
    {
        var y = 1;
        var radian = degrees * Math.PI / 180;

        var cos = Math.Cos(radian);

        var bound = (int) (Cos45 * width);
        var x = Utils.Map((int) (cos * width), 0, -bound, width / 2, 1);

        return new Point(x, y);
    }

    private static Point GetLeftTopPoint(int height, int degrees)
    {
        var x = 1;
        var radian = degrees * Math.PI / 180;

        var sin = Math.Sin(radian);

        var bound = (int) (Sin45 * height);
        var y = Utils.Map((int) (sin * height), bound, 0, 1, height / 2);

        return new Point(x, y);
    }

    private static Point GetLeftBottomPoint(int height, int degrees)
    {
        var x = 1;
        var radian = degrees * Math.PI / 180;

        var sin = Math.Sin(radian);

        var bound = (int) (Sin45 * height);
        var y = Utils.Map((int) (sin * height), 0, -bound, height / 2, height - 1);

        return new Point(x, y);
    }

    private static Point GetBottomLeftPoint(int width, int height, int degrees)
    {
        var y = height - 1;
        var radian = degrees * Math.PI / 180;

        var cos = Math.Cos(radian);

        var bound = (int) (Cos45 * width);
        var x = Utils.Map((int) (cos * width), -bound, 0, 1, width / 2);

        return new Point(x, y);
    }

    private static Point GetBottomRightPoint(int width, int height, int degrees)
    {
        var y = height - 1;
        var radian = degrees * Math.PI / 180;

        var cos = Math.Cos(radian);

        var bound = (int) (Cos45 * width);
        var x = Utils.Map((int) (cos * width), 0, bound, width / 2, width - 1);

        return new Point(x, y);
    }

    private static Point GetRightBottomQuadrantPoint(int width, int height, int degrees)
    {
        var x = width - 1;
        var radian = degrees * Math.PI / 180;

        var sin = Math.Sin(radian);

        var bound = (int) (Sin45 * height);
        var y = Utils.Map((int) (sin * height), -bound, 0, height - 1, height / 2);

        return new Point(x, y);
    }

    private static Point GetRightTopPoint(int width, int height, int degrees)
    {
        var x = width - 1;
        var radian = degrees * Math.PI / 180;

        var sin = Math.Sin(radian);

        var bound = (int) (Sin45 * height);
        var y = Utils.Map((int) (sin * height), 0, bound, height / 2, 1);

        return new Point(x, y);
    }
}