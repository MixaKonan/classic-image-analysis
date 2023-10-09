using System.Drawing;

namespace Enodatio.Logic.Models;

public class PixelInfo
{
    public const double RedGrayscaleWeight = 0.299;
    public const double GreenGrayscaleWeight = 0.587;
    public const double BlueGrayscaleWeight = 0.114;

    public int X { get; }

    public int Y { get; }

    public Color Color { get; }

    public PixelInfo(int x, int y, Color color)
    {
        X = x;
        Y = y;
        Color = color;
    }

    public double GetGrayscaleIntensity()
    {
        return (RedGrayscaleWeight * this.Color.R) + (GreenGrayscaleWeight * this.Color.G) + (BlueGrayscaleWeight * this.Color.B);
    }
}