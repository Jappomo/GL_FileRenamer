using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class FlatButton : Button, IThemeable
{
	public enum ButtonRole
	{
		Accent,
		Secondary
	}

	private bool hover;

	private bool pressed;

	public ButtonRole Role { get; set; }

	public int CornerRadius { get; set; }

	public FlatButton()
	{
		Role = ButtonRole.Accent;
		CornerRadius = 10;
		base.FlatStyle = FlatStyle.Flat;
		base.FlatAppearance.BorderSize = 0;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
		base.Height = 42;
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
		using GraphicsPath path = GfxUtil.RoundedRect(rectangle, CornerRadius);
		Color color;
		Color foreColor;
		Color color2;
		if (Role == ButtonRole.Accent)
		{
			color = (pressed ? Theme.Mix(current.Accent, Color.Black, 0.12) : ((!hover) ? current.Accent : current.AccentHover));
			foreColor = current.AccentText;
			color2 = color;
		}
		else
		{
			color = (pressed ? current.SurfaceAlt : ((!hover) ? current.Surface : Theme.Mix(current.Surface, current.Accent, 0.08)));
			foreColor = ((!base.Enabled) ? current.TextMuted : current.Text);
			color2 = ((!hover) ? current.Border : current.Accent);
		}
		if (!base.Enabled)
		{
			color = Theme.Mix(color, current.AppBackground, 0.5);
			foreColor = current.TextMuted;
		}
		using (SolidBrush brush = new SolidBrush(color))
		{
			graphics.FillPath(brush, path);
		}
		using (Pen pen = new Pen(color2, (Role != ButtonRole.Secondary) ? 1f : 1.4f))
		{
			graphics.DrawPath(pen, path);
		}
		TextRenderer.DrawText(graphics, Text, Font, rectangle, foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
	}
}
