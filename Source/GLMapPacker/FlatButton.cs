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

	private readonly Tween hover;

	private bool pressed;

	public ButtonRole Role { get; set; }

	public int CornerRadius { get; set; }

	public FlatButton()
	{
		Role = ButtonRole.Accent;
		CornerRadius = Ui.S(8);
		hover = new Tween(this, 140f);
		base.FlatStyle = FlatStyle.Flat;
		base.FlatAppearance.BorderSize = 0;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Font = Ui.Strong;
		base.Height = Ui.S(40);
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

	protected override void OnEnabledChanged(EventArgs e)
	{
		hover.Snap(0f);
		base.OnEnabledChanged(e);
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
		float h = hover.Value;
		Color fill;
		Color text;
		Color border;
		if (Role == ButtonRole.Accent)
		{
			fill = pressed ? Theme.Mix(current.Accent, Color.Black, 0.12) : Theme.Mix(current.Accent, current.AccentHover, h);
			text = current.AccentText;
			border = fill;
		}
		else
		{
			fill = pressed ? current.SurfaceAlt : Theme.Mix(current.SurfaceAlt, Theme.Mix(current.SurfaceAlt, current.Accent, 0.12), h);
			text = current.Text;
			border = Theme.Mix(current.Border, current.Accent, h);
		}
		if (!base.Enabled)
		{
			fill = Theme.Mix(Role == ButtonRole.Accent ? current.SurfaceAlt : fill, GfxUtil.ResolveBack(this), 0.3);
			border = Role == ButtonRole.Accent ? fill : current.Border;
			text = current.TextMuted;
		}
		GfxUtil.FillRounded(graphics, rectangle, CornerRadius, fill);
		GfxUtil.DrawRounded(graphics, rectangle, CornerRadius, border);
		TextRenderer.DrawText(graphics, Text, Font, rectangle, text, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
	}
}
