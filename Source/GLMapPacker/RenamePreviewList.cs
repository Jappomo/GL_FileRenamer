using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class RenamePreviewList : Control, IThemeable
{
	public class Row
	{
		public string Left;

		public string Right;

		public bool Warn;

		public bool Changed = true;
	}

	private readonly List<Row> rows = new List<Row>();

	private int scroll;

	private static int RowH => Ui.S(28);

	private static int HeaderH => Ui.S(34);

	public int Count => rows.Count;

	public RenamePreviewList()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
	}

	public void SetRows(IEnumerable<Row> newRows)
	{
		rows.Clear();
		if (newRows != null)
		{
			rows.AddRange(newRows);
		}
		scroll = 0;
		Invalidate();
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		int val = Math.Max(0, rows.Count * RowH - (base.Height - HeaderH));
		scroll = Math.Max(0, Math.Min(val, scroll - e.Delta));
		Invalidate();
		base.OnMouseWheel(e);
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
		using GraphicsPath graphicsPath = GfxUtil.RoundedRect(rectangle, Ui.S(10));
		using (SolidBrush brush = new SolidBrush(current.SurfaceAlt))
		{
			graphics.FillPath(brush, graphicsPath);
		}
		Region clip = graphics.Clip;
		graphics.SetClip(graphicsPath);
		int num = base.Width / 2;
		using (SolidBrush brush2 = new SolidBrush(Theme.Mix(current.SurfaceAlt, current.Surface, 0.5)))
		{
			graphics.FillRectangle(brush2, 0, 0, base.Width, HeaderH);
		}
		int pad = Ui.S(14);
		TextRenderer.DrawText(graphics, "CURRENT NAME", Ui.Label, new Rectangle(pad, 0, num - pad, HeaderH), current.TextMuted, TextFormatFlags.VerticalCenter);
		TextRenderer.DrawText(graphics, "NEW NAME", Ui.Label, new Rectangle(num + pad, 0, num - pad, HeaderH), current.TextMuted, TextFormatFlags.VerticalCenter);
		Font font2 = Ui.Small;
		{
			int num2 = HeaderH - scroll;
			int num3 = 0;
			while (num3 < rows.Count)
			{
				if (num2 + RowH >= HeaderH && num2 <= base.Height)
				{
					Row row = rows[num3];
					if (num3 % 2 == 1)
					{
						using SolidBrush brush3 = new SolidBrush(Theme.Mix(current.SurfaceAlt, current.Surface, 0.25));
						graphics.FillRectangle(brush3, 0, num2, base.Width, RowH);
					}
					Color foreColor = ((!row.Changed) ? current.TextMuted : current.Text);
					Color foreColor2 = (row.Warn ? current.Danger : ((!row.Changed) ? current.TextMuted : current.Accent));
					TextRenderer.DrawText(graphics, row.Left, font2, new Rectangle(pad, num2, num - pad * 2, RowH), foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
					TextRenderer.DrawText(graphics, row.Right, font2, new Rectangle(num + pad, num2, num - pad * 2, RowH), foreColor2, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
				}
				num3++;
				num2 += RowH;
			}
			if (rows.Count == 0)
			{
				TextRenderer.DrawText(graphics, "Choose a folder to preview renames", font2, rectangle, current.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
			}
		}
		if (rows.Count > 0)
		{
			using Pen pen = new Pen(current.Border, 1f);
			graphics.DrawLine(pen, num, HeaderH, num, base.Height);
		}
		graphics.Clip = clip;
		using Pen pen2 = new Pen(current.Border, 1f);
		graphics.DrawPath(pen2, graphicsPath);
	}
}
