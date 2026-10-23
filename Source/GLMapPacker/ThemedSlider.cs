using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace GLMapPacker;

// Labelled slider: caption and value on top, track below. Drag or scroll to change, double-click to reset.
public class ThemedSlider : Control, IThemeable
{
	private double min;

	private double max = 1.0;

	private double val;

	private bool dragging;

	private bool hover;

	private bool resetHover;

	// Where the reset icon was drawn (empty while the value is at its default).
	private Rectangle resetRect;

	public string Caption { get; set; }

	public string Suffix { get; set; }

	public int Decimals { get; set; }

	public double DefaultValue { get; set; }

	public double Minimum
	{
		get
		{
			return min;
		}
		set
		{
			min = value;
			Value = val;
		}
	}

	public double Maximum
	{
		get
		{
			return max;
		}
		set
		{
			max = value;
			Value = val;
		}
	}

	public double Value
	{
		get
		{
			return val;
		}
		set
		{
			double v = Math.Max(min, Math.Min(max, Math.Round(value, Decimals)));
			if (v != val)
			{
				val = v;
				Invalidate();
				if (this.ValueChanged != null)
				{
					this.ValueChanged(this, EventArgs.Empty);
				}
			}
		}
	}

	public event EventHandler ValueChanged;

	public ThemedSlider(string caption, double minimum, double maximum, double value, int decimals, string suffix = "")
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Caption = caption;
		Suffix = suffix;
		Decimals = decimals;
		min = minimum;
		max = maximum;
		val = Math.Max(min, Math.Min(max, value));
		DefaultValue = val;
		base.Height = Ui.S(46);
	}

	public void ApplyTheme(Palette p)
	{
		Invalidate();
	}

	private Rectangle TrackRect => new Rectangle(Ui.S(8), Ui.S(31), Math.Max(1, base.Width - Ui.S(17)), Ui.S(4));

	private void SetFromX(int x)
	{
		Rectangle t = TrackRect;
		double f = Math.Max(0.0, Math.Min(1.0, (double)(x - t.Left) / t.Width));
		Value = min + f * (max - min);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && !resetRect.IsEmpty && resetRect.Contains(e.Location))
		{
			Value = DefaultValue;
			resetHover = false;
		}
		else if (e.Button == MouseButtons.Left)
		{
			dragging = true;
			base.Capture = true;
			SetFromX(e.X);
		}
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (dragging)
		{
			SetFromX(e.X);
		}
		bool overReset = !dragging && !resetRect.IsEmpty && resetRect.Contains(e.Location);
		if (overReset != resetHover)
		{
			resetHover = overReset;
			Invalidate();
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		dragging = false;
		base.Capture = false;
		Invalidate();
		base.OnMouseUp(e);
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
		resetHover = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDoubleClick(MouseEventArgs e)
	{
		Value = DefaultValue;
		base.OnMouseDoubleClick(e);
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		double step = Math.Max(Math.Pow(10.0, -Decimals), (max - min) / 100.0);
		Value = val + Math.Sign(e.Delta) * step;
		base.OnMouseWheel(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(GfxUtil.ResolveBack(this));
		Rectangle top = new Rectangle(0, 0, base.Width, Ui.S(22));
		TextRenderer.DrawText(graphics, Caption, Ui.Body, top, current.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		string text = val.ToString("F" + Decimals, CultureInfo.InvariantCulture) + Suffix;
		bool changed = val != DefaultValue;
		TextRenderer.DrawText(graphics, text, Ui.Body, top, changed ? current.Accent : current.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
		resetRect = Rectangle.Empty;
		if (changed)
		{
			int textW = TextRenderer.MeasureText(graphics, text, Ui.Body, top.Size, TextFormatFlags.NoPadding).Width;
			int size = Ui.S(18);
			resetRect = new Rectangle(base.Width - textW - Ui.S(10) - size, (top.Height - size) / 2, size, size);
			DrawResetIcon(graphics, resetRect, resetHover ? current.Accent : current.TextMuted, resetHover ? Theme.Mix(GfxUtil.ResolveBack(this), current.Accent, 0.15) : Color.Empty);
		}
		Rectangle t = TrackRect;
		using (GraphicsPath path = GfxUtil.RoundedRect(t, t.Height / 2))
		{
			using SolidBrush brush = new SolidBrush(current.TrackBackground);
			graphics.FillPath(brush, path);
		}
		double f = max > min ? (val - min) / (max - min) : 0.0;
		int thumbX = t.Left + (int)Math.Round(f * t.Width);
		if (thumbX > t.Left)
		{
			using GraphicsPath path2 = GfxUtil.RoundedRect(new Rectangle(t.Left, t.Top, thumbX - t.Left, t.Height), t.Height / 2);
			using SolidBrush brush2 = new SolidBrush(current.Accent);
			graphics.FillPath(brush2, path2);
		}
		int r = Ui.S((dragging || hover) ? 8 : 7);
		Rectangle knob = new Rectangle(thumbX - r, t.Top + t.Height / 2 - r, r * 2, r * 2);
		using (SolidBrush brush3 = new SolidBrush(current.Surface))
		{
			graphics.FillEllipse(brush3, knob);
		}
		using Pen pen = new Pen(current.Accent, Ui.S(2f));
		graphics.DrawEllipse(pen, knob);
	}

	// Circular "reset" arrow: an open ring with an arrowhead at its upper end.
	private static void DrawResetIcon(Graphics g, Rectangle r, Color color, Color back)
	{
		if (back != Color.Empty)
		{
			using SolidBrush b = new SolidBrush(back);
			g.FillEllipse(b, r);
		}
		float cx = r.X + r.Width / 2f;
		float cy = r.Y + r.Height / 2f;
		float rad = r.Width * 0.27f;
		using Pen pen = new Pen(color, Ui.S(1.5f));
		pen.StartCap = LineCap.Round;
		pen.EndCap = LineCap.Round;
		g.DrawArc(pen, cx - rad, cy - rad, rad * 2f, rad * 2f, -60f, 290f);
		// Arrowhead at the arc's start (pointing clockwise, top right).
		double a = -60.0 * Math.PI / 180.0;
		float ex = cx + rad * (float)Math.Cos(a);
		float ey = cy + rad * (float)Math.Sin(a);
		float h = r.Width * 0.2f;
		using SolidBrush fill = new SolidBrush(color);
		g.FillPolygon(fill, new PointF[3]
		{
			new PointF(ex + h * 0.9f, ey - h * 0.3f),
			new PointF(ex - h * 0.45f, ey - h * 0.95f),
			new PointF(ex - h * 0.1f, ey + h * 0.55f)
		});
	}
}
