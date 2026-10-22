using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class ThemedCheckBox : CheckBox, IThemeable
{
	private readonly Tween check;

	public ThemedCheckBox()
	{
		check = new Tween(this, 140f);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Font = Ui.Body;
		base.Height = Ui.S(26);
	}

	protected override void OnCheckedChanged(EventArgs e)
	{
		if (IsHandleCreated && Visible)
		{
			check.To(base.Checked ? 1f : 0f);
		}
		else
		{
			check.Snap(base.Checked ? 1f : 0f);
		}
		base.OnCheckedChanged(e);
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
		int num = Ui.S(18);
		int y = (base.Height - num) / 2;
		Rectangle r = new Rectangle(0, y, num, num);
		float t = check.Value;
		GfxUtil.FillRounded(graphics, r, Ui.S(5), Theme.Mix(current.SurfaceAlt, current.Accent, t));
		GfxUtil.DrawRounded(graphics, r, Ui.S(5), Theme.Mix(current.Border, current.Accent, t), Ui.S(1.2f));
		if (t > 0.05f)
		{
			using Pen pen = new Pen(Theme.Mix(current.Accent, current.AccentText, t), Ui.S(2f));
			pen.StartCap = LineCap.Round;
			pen.EndCap = LineCap.Round;
			pen.LineJoin = LineJoin.Round;
			float s = num / 18f;
			graphics.DrawLines(pen, new PointF[3]
			{
				new PointF(r.Left + 4.5f * s, r.Top + 9.2f * s),
				new PointF(r.Left + 7.8f * s, r.Top + 12.4f * s),
				new PointF(r.Left + 13.5f * s, r.Top + 5.8f * s)
			});
		}
		Rectangle bounds = new Rectangle(num + Ui.S(10), 0, base.Width - num - Ui.S(10), base.Height);
		TextRenderer.DrawText(graphics, Text, Font, bounds, current.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
	}
}
