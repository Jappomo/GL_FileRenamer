using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class IconButton : Button, IThemeable
{
	private readonly Tween hover;

	private bool pressed;

	public int CornerRadius { get; set; }

	// Colour behind the button when it sits on something other than its parent's background
	// (e.g. inside a painted field); null uses the parent's.
	public Color? Backdrop { get; set; }

	public IconButton()
	{
		CornerRadius = Ui.S(6);
		hover = new Tween(this, 120f);
		base.FlatStyle = FlatStyle.Flat;
		base.FlatAppearance.BorderSize = 0;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		hover.To(1f);
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		hover.To(0f);
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
		graphics.Clear(Backdrop ?? GfxUtil.ResolveBack(this));
		Rectangle rectangle = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		Color color = pressed ? current.Accent : Theme.Mix(current.SurfaceAlt, Theme.Mix(current.SurfaceAlt, current.Accent, 0.25), hover.Value);
		GfxUtil.FillRounded(graphics, rectangle, CornerRadius, color);
		if (base.Image != null)
		{
			int width = base.Image.Width;
			int height = base.Image.Height;
			graphics.DrawImage(base.Image, (base.Width - width) / 2, (base.Height - height) / 2, width, height);
		}
		else if (!string.IsNullOrEmpty(Text))
		{
			Color foreColor = pressed ? current.AccentText : Theme.Mix(current.TextMuted, current.Text, hover.Value);
			TextRenderer.DrawText(graphics, Text, Font, rectangle, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
		}
	}
}
