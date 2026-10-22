using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

// Notification card that slides up in the bottom-right corner of its parent and hides itself.
public class Toast : Control, IThemeable
{
	private readonly Timer anim;

	private readonly Timer life;

	private readonly Stopwatch clock = new Stopwatch();

	private NoticeKind kind;

	private string message = "";

	private int targetTop;

	// Distance from the parent's bottom edge, so the toast sits above the status bar.
	// Toasts are centred horizontally so they don't cover the action buttons at the right.
	public int BottomOffset { get; set; }

	public Toast()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		Cursor = Cursors.Hand;
		Visible = false;
		anim = new Timer
		{
			Interval = 15
		};
		anim.Tick += delegate
		{
			float t = Math.Min(1f, (float)clock.Elapsed.TotalMilliseconds / 220f);
			float e = 1f - (1f - t) * (1f - t) * (1f - t);
			base.Top = targetTop + (int)((1f - e) * Ui.S(18));
			if (t >= 1f)
			{
				anim.Stop();
			}
		};
		life = new Timer();
		life.Tick += delegate
		{
			life.Stop();
			Visible = false;
		};
	}

	public void Show(NoticeKind kind, string text)
	{
		this.kind = kind;
		message = (text ?? "").Replace("\r", "").Replace("\n", "  ");
		int maxText = Ui.S(380);
		Size ts = TextRenderer.MeasureText(message, Ui.Body, new Size(maxText, int.MaxValue), TextFormatFlags.WordBreak);
		int w = Math.Min(maxText, ts.Width) + Ui.S(84);
		int h = Math.Max(Ui.S(52), Math.Min(ts.Height, Ui.S(60)) + Ui.S(28));
		base.Size = new Size(w, h);
		using (GraphicsPath path = GfxUtil.RoundedRect(new Rectangle(0, 0, w, h), Ui.S(10)))
		{
			Region?.Dispose();
			Region = new Region(path);
		}
		Reposition();
		base.Top = targetTop + Ui.S(18);
		Visible = true;
		BringToFront();
		Invalidate();
		clock.Restart();
		anim.Start();
		life.Stop();
		life.Interval = kind == NoticeKind.Error ? 7000 : 3800;
		life.Start();
	}

	public void Reposition()
	{
		if (base.Parent != null)
		{
			int m = Ui.S(20);
			base.Left = (base.Parent.ClientSize.Width - base.Width) / 2;
			targetTop = base.Parent.ClientSize.Height - BottomOffset - base.Height - m;
			if (!anim.Enabled)
			{
				base.Top = targetTop;
			}
		}
	}

	protected override void OnClick(EventArgs e)
	{
		life.Stop();
		Visible = false;
		base.OnClick(e);
	}

	public void ApplyTheme(Palette p)
	{
		Invalidate();
	}

	public static Color KindColor(NoticeKind kind, Palette p)
	{
		return kind switch
		{
			NoticeKind.Success => p.Success,
			NoticeKind.Warning => p.Warning,
			NoticeKind.Error => p.Danger,
			_ => p.Accent,
		};
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Color accent = KindColor(kind, current);
		graphics.Clear(Theme.Mix(current.Surface, accent, current.IsDark ? 0.10 : 0.06));
		Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		GfxUtil.DrawRounded(graphics, r, Ui.S(10), Theme.Mix(current.Border, accent, 0.45));
		using (SolidBrush bar = new SolidBrush(accent))
		{
			graphics.FillRectangle(bar, 0, 0, Ui.S(4), base.Height);
		}
		int d = Ui.S(22);
		Rectangle icon = new Rectangle(Ui.S(16), (base.Height - d) / 2, d, d);
		using (SolidBrush brush = new SolidBrush(accent))
		{
			graphics.FillEllipse(brush, icon);
		}
		using (Pen pen = new Pen(Color.White, Ui.S(1.8f)))
		{
			pen.StartCap = LineCap.Round;
			pen.EndCap = LineCap.Round;
			float cx = icon.X + d / 2f;
			float cy = icon.Y + d / 2f;
			float a = d * 0.22f;
			switch (kind)
			{
			case NoticeKind.Success:
				graphics.DrawLines(pen, new PointF[3]
				{
					new PointF(cx - a, cy),
					new PointF(cx - a * 0.25f, cy + a * 0.8f),
					new PointF(cx + a * 1.1f, cy - a * 0.8f)
				});
				break;
			case NoticeKind.Error:
				graphics.DrawLine(pen, cx - a, cy - a, cx + a, cy + a);
				graphics.DrawLine(pen, cx + a, cy - a, cx - a, cy + a);
				break;
			default:
				// "i" for info, "!" for warning.
				float top = kind == NoticeKind.Warning ? cy - a * 1.2f : cy - a * 0.3f;
				float bottom = kind == NoticeKind.Warning ? cy + a * 0.3f : cy + a * 1.2f;
				graphics.DrawLine(pen, cx, top, cx, bottom);
				float dotY = kind == NoticeKind.Warning ? cy + a * 1.15f : cy - a * 1.15f;
				graphics.FillEllipse(Brushes.White, cx - Ui.S(1.3f), dotY - Ui.S(1.3f), Ui.S(2.6f), Ui.S(2.6f));
				break;
			}
		}
		Rectangle text = new Rectangle(icon.Right + Ui.S(12), Ui.S(10), base.Width - icon.Right - Ui.S(46), base.Height - Ui.S(20));
		TextRenderer.DrawText(graphics, message, Ui.Body, text, current.Text, TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
		using Pen close = new Pen(current.TextMuted, Ui.S(1.4f));
		float x = base.Width - Ui.S(22);
		float y = base.Height / 2f;
		float c = Ui.S(4f);
		graphics.DrawLine(close, x - c, y - c, x + c, y + c);
		graphics.DrawLine(close, x + c, y - c, x - c, y + c);
	}
}

// Bottom bar: the latest notice on the left, app info on the right.
public class StatusBar : Control, IThemeable
{
	private string message = "Ready";

	private NoticeKind kind;

	public string RightText { get; set; }

	public StatusBar()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		base.Height = Ui.S(30);
	}

	public void SetMessage(NoticeKind kind, string text)
	{
		this.kind = kind;
		message = (text ?? "").Replace("\r", "").Replace("\n", "  ");
		Invalidate();
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
		graphics.Clear(current.Surface);
		using (Pen pen = new Pen(current.Border))
		{
			graphics.DrawLine(pen, 0, 0, base.Width, 0);
		}
		int pad = Ui.S(24);
		int d = Ui.S(7);
		using (SolidBrush dot = new SolidBrush(Toast.KindColor(kind, current)))
		{
			graphics.FillEllipse(dot, pad, (base.Height - d) / 2, d, d);
		}
		Size right = TextRenderer.MeasureText(RightText ?? "", Ui.Small);
		Rectangle left = new Rectangle(pad + d + Ui.S(8), 0, base.Width - pad * 2 - d - right.Width - Ui.S(24), base.Height);
		TextRenderer.DrawText(graphics, message, Ui.Small, left, current.TextMuted, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		TextRenderer.DrawText(graphics, RightText ?? "", Ui.Small, new Rectangle(0, 0, base.Width - pad, base.Height), current.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
	}
}
