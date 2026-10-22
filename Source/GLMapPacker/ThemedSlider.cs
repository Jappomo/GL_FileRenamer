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
		if (e.Button == MouseButtons.Left)
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
		TextRenderer.DrawText(graphics, text, Ui.Body, top, val != DefaultValue ? current.Accent : current.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
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
}
