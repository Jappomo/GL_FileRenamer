using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

// Light/dark switch; the knob slides and shows a sun or a moon.
public class ThemeToggle : Control, IThemeable
{
	private readonly Tween knob;

	public event EventHandler Toggled;

	public ThemeToggle()
	{
		knob = new Tween(this, 200f, Theme.IsDark ? 1f : 0f);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		base.Size = Ui.S(60, 32);
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
		knob.To(p.IsDark ? 1f : 0f);
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(GfxUtil.ResolveBack(this));
		Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		GfxUtil.FillRounded(graphics, r, r.Height / 2, current.SurfaceAlt);
		GfxUtil.DrawRounded(graphics, r, r.Height / 2, current.Border);
		int pad = Ui.S(3);
		int d = r.Height - pad * 2;
		float x = r.X + pad + knob.Value * (r.Width - d - pad * 2);
		RectangleF k = new RectangleF(x, r.Y + pad, d, d);
		using (SolidBrush brush = new SolidBrush(current.Accent))
		{
			graphics.FillEllipse(brush, k);
		}
		float cx = k.X + d / 2f;
		float cy = k.Y + d / 2f;
		using SolidBrush glyph = new SolidBrush(current.AccentText);
		if (Theme.IsDark)
		{
			// Moon: a disc with a smaller disc cut out of its upper right.
			using GraphicsPath moon = new GraphicsPath();
			float mr = d * 0.27f;
			moon.AddEllipse(cx - mr, cy - mr, mr * 2f, mr * 2f);
			using Region region = new Region(moon);
			using GraphicsPath cut = new GraphicsPath();
			cut.AddEllipse(cx - mr * 0.35f, cy - mr * 1.25f, mr * 2f, mr * 2f);
			region.Exclude(cut);
			graphics.FillRegion(glyph, region);
		}
		else
		{
			float sr = d * 0.16f;
			graphics.FillEllipse(glyph, cx - sr, cy - sr, sr * 2f, sr * 2f);
			using Pen pen = new Pen(current.AccentText, Ui.S(1.4f));
			pen.StartCap = LineCap.Round;
			pen.EndCap = LineCap.Round;
			for (int i = 0; i < 8; i++)
			{
				double a = i * Math.PI / 4.0;
				float c = (float)Math.Cos(a);
				float s = (float)Math.Sin(a);
				graphics.DrawLine(pen, cx + c * sr * 1.6f, cy + s * sr * 1.6f, cx + c * sr * 2.3f, cy + s * sr * 2.3f);
			}
		}
	}
}
