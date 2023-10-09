using System.Drawing;

namespace Enodatio.Logic.Models;

public class Beam
{
    public Point Start { get; set; }

    public Point End { get; set; }

    public List<PixelInfo> Pixels { get; set; } = new List<PixelInfo>();
}