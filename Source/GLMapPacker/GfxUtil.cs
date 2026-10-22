using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

internal static class GfxUtil
{
	public static Color ResolveBack(Control c)
	{
		for (Control control = c?.Parent; control != null; control = control.Parent)
		{
			if (control is TitledCard)
			{
				return Theme.Current.Surface;
			}
			if (control.BackColor.A == byte.MaxValue && control.BackColor != Color.Transparent)
			{
				return control.BackColor;
			}
		}
		return Theme.Current.AppBackground;
	}

	public static GraphicsPath RoundedRect(Rectangle r, int radius)
	{
		int num = radius * 2;
		GraphicsPath graphicsPath = new GraphicsPath();
		if (num <= 0)
		{
			graphicsPath.AddRectangle(r);
			graphicsPath.CloseFigure();
			return graphicsPath;
		}
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
