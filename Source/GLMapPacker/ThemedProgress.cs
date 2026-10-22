using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class ThemedProgress : Control, IThemeable
{
	private int val;

	public int Value
	{
		get
		{
			return val;
		}
		set
		{
			val = Math.Max(0, Math.Min(100, value));
			Invalidate();
		}
	}

	public ThemedProgress()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		base.Height = 8;
	}

	public void ApplyTheme(Palette p)
	{
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(GfxUtil.ResolveBack(this));
		Rectangle r = new Rectangle(0, (base.Height - 8) / 2, base.Width - 1, 7);
		using (GraphicsPath path = GfxUtil.RoundedRect(r, 3))
		{
			using SolidBrush brush = new SolidBrush(current.TrackBackground);
			graphics.FillPath(brush, path);
		}
		if (val <= 0)
		{
			return;
		}
		int num = (int)((double)(base.Width - 1) * ((double)val / 100.0));
		if (num < 6)
		{
			num = 6;
		}
		Rectangle r2 = new Rectangle(0, r.Y, num, 7);
		using GraphicsPath path2 = GfxUtil.RoundedRect(r2, 3);
		using SolidBrush brush2 = new SolidBrush(current.Accent);
		graphics.FillPath(brush2, path2);
	}
}
