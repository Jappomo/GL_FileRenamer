using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class IconButton : Button, IThemeable
{
	private bool hover;

	private bool pressed;

	public int CornerRadius { get; set; }

	public IconButton()
	{
		CornerRadius = 8;
		base.FlatStyle = FlatStyle.Flat;
		base.FlatAppearance.BorderSize = 0;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		hover = true;
		Invalidate();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		hover = false;
		pressed = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		pressed = true;
		Invalidate();
		base.OnMouseDown(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		pressed = false;
		Invalidate();
		base.OnMouseUp(e);
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
		Rectangle rectangle = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		Color color = (pressed ? current.Accent : ((!hover) ? current.SurfaceAlt : Theme.Mix(current.SurfaceAlt, current.Accent, 0.25)));
		using (GraphicsPath path = GfxUtil.RoundedRect(rectangle, CornerRadius))
		{
			using SolidBrush brush = new SolidBrush(color);
			graphics.FillPath(brush, path);
		}
		if (base.Image != null)
		{
			int width = base.Image.Width;
			int height = base.Image.Height;
			graphics.DrawImage(base.Image, (base.Width - width) / 2, (base.Height - height) / 2, width, height);
		}
		else if (!string.IsNullOrEmpty(Text))
		{
			Color foreColor = ((!pressed) ? current.Text : current.AccentText);
			TextRenderer.DrawText(graphics, Text, Font, rectangle, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
		}
	}
}
