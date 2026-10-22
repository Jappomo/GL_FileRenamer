using System;
using System.Diagnostics;
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
		num = Math.Min(num, Math.Min(r.Width, r.Height));
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	public static void FillRounded(Graphics g, Rectangle r, int radius, Color fill)
	{
		using GraphicsPath path = RoundedRect(r, radius);
		using SolidBrush brush = new SolidBrush(fill);
		g.FillPath(brush, path);
	}

	public static void DrawRounded(Graphics g, Rectangle r, int radius, Color color, float width = 1f, bool dashed = false)
	{
		using GraphicsPath path = RoundedRect(r, radius);
		using Pen pen = new Pen(color, width);
		if (dashed)
		{
			pen.DashPattern = new float[2] { 4f, 3f };
		}
		g.DrawPath(pen, path);
	}

	// Small "v" chevron centred at (cx, cy).
	public static void DrawChevron(Graphics g, Color color, float cx, float cy, float size)
	{
		using Pen pen = new Pen(color, Ui.S(1.6f));
		pen.StartCap = LineCap.Round;
		pen.EndCap = LineCap.Round;
		pen.LineJoin = LineJoin.Round;
		g.DrawLines(pen, new PointF[3]
		{
			new PointF(cx - size, cy - size / 2f),
			new PointF(cx, cy + size / 2f),
			new PointF(cx + size, cy - size / 2f)
		});
	}

	// Pill-shaped label, e.g. for overlays on previews.
	public static Size DrawChip(Graphics g, string text, Font font, Point at, Color back, Color fore)
	{
		Size ts = TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding);
		Rectangle r = new Rectangle(at.X, at.Y, ts.Width + Ui.S(16), ts.Height + Ui.S(8));
		FillRounded(g, r, r.Height / 2, back);
		TextRenderer.DrawText(g, text, font, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		return r.Size;
	}
}

// Eases a value toward a target over a short time and repaints its control each frame.
internal sealed class Tween
{
	private readonly Control owner;

	private readonly Timer timer;

	private readonly Stopwatch clock = new Stopwatch();

	private readonly float durationMs;

	private float from;

	private float to;

	public float Value { get; private set; }

	public Tween(Control owner, float durationMs, float initial = 0f)
	{
		this.owner = owner;
		this.durationMs = durationMs;
		Value = from = to = initial;
		timer = new Timer
		{
			Interval = 15
		};
		timer.Tick += delegate
		{
			Step();
		};
		owner.Disposed += delegate
		{
			timer.Dispose();
		};
	}

	public void To(float target)
	{
		if (target == to)
		{
			return;
		}
		from = Value;
		to = target;
		clock.Restart();
		timer.Start();
	}

	public void Snap(float value)
	{
		timer.Stop();
		Value = from = to = value;
		owner.Invalidate();
	}

	private void Step()
	{
		float t = (float)clock.Elapsed.TotalMilliseconds / durationMs;
		if (t >= 1f)
		{
			Value = to;
			timer.Stop();
		}
		else
		{
			float e = 1f - (1f - t) * (1f - t) * (1f - t);
			Value = from + (to - from) * e;
		}
		owner.Invalidate();
	}
}
