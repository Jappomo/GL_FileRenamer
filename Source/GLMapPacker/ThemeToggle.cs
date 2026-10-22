using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class ThemeToggle : Control, IThemeable
{
	public event EventHandler Toggled;

	public ThemeToggle()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		base.Size = new Size(58, 30);
	}

	protected override void OnClick(EventArgs e)
	{
		Theme.Toggle();
		if (this.Toggled != null)
		{
			this.Toggled(this, EventArgs.Empty);
		}
		base.OnClick(e);
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
		Rectangle r = new Rectangle(0, 2, base.Width - 1, base.Height - 5);
		using (GraphicsPath path = GfxUtil.RoundedRect(r, (base.Height - 5) / 2))
		{
			using (SolidBrush brush = new SolidBrush(current.SurfaceAlt))
			{
				graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(current.Border, 1f);
			graphics.DrawPath(pen, path);
		}
		int num = base.Height - 11;
		int y = r.Y + (r.Height - num) / 2;
		int x = ((!Theme.IsDark) ? (r.X + 4) : (r.Right - num - 4));
		using SolidBrush brush2 = new SolidBrush(current.Accent);
		graphics.FillEllipse(brush2, x, y, num, num);
	}
}
