using System.Drawing.Drawing2D;

namespace ImageCP;

public class PixelBox : PictureBox
{
    protected override void OnPaint(PaintEventArgs pe)
    {
        pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        base.OnPaint(pe);
    }
}