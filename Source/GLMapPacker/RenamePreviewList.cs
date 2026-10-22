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

	private const int RowH = 26;

	private const int HeaderH = 30;

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
		int val = Math.Max(0, rows.Count * 26 - (base.Height - 30));
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
		using GraphicsPath graphicsPath = GfxUtil.RoundedRect(rectangle, 10);
		using (SolidBrush brush = new SolidBrush(current.SurfaceAlt))
		{
			graphics.FillPath(brush, graphicsPath);
		}
		Region clip = graphics.Clip;
		graphics.SetClip(graphicsPath);
		int num = base.Width / 2;
		using (SolidBrush brush2 = new SolidBrush(Theme.Mix(current.SurfaceAlt, current.Surface, 0.5)))
		{
			graphics.FillRectangle(brush2, 0, 0, base.Width, 30);
		}
		using (Font font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold))
		{
			TextRenderer.DrawText(graphics, "Current name", font, new Rectangle(12, 0, num - 16, 30), current.TextMuted, TextFormatFlags.VerticalCenter);
			TextRenderer.DrawText(graphics, "New name", font, new Rectangle(num + 4, 0, num - 16, 30), current.TextMuted, TextFormatFlags.VerticalCenter);
		}
		using (Font font2 = new Font("Segoe UI", 9f))
		{
			int num2 = 30 - scroll;
			int num3 = 0;
			while (num3 < rows.Count)
			{
				if (num2 + 26 >= 30 && num2 <= base.Height)
				{
					Row row = rows[num3];
					if (num3 % 2 == 1)
					{
						using SolidBrush brush3 = new SolidBrush(Theme.Mix(current.SurfaceAlt, current.Surface, 0.25));
						graphics.FillRectangle(brush3, 0, num2, base.Width, 26);
					}
					Color foreColor = ((!row.Changed) ? current.TextMuted : current.TextMuted);
					Color foreColor2 = (row.Warn ? Color.FromArgb(226, 96, 90) : ((!row.Changed) ? current.TextMuted : current.Accent));
					TextRenderer.DrawText(graphics, row.Left, font2, new Rectangle(12, num2, num - 20, 26), foreColor, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
					TextRenderer.DrawText(graphics, row.Right, font2, new Rectangle(num + 4, num2, num - 20, 26), foreColor2, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
				}
				num3++;
				num2 += 26;
			}
			if (rows.Count == 0)
			{
				TextRenderer.DrawText(graphics, "choose a folder to preview renames", font2, rectangle, current.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
			}
		}
		using (Pen pen = new Pen(current.Border, 1f))
		{
			graphics.DrawLine(pen, num, 30, num, base.Height);
		}
		graphics.Clip = clip;
		using Pen pen2 = new Pen(current.Border, 1f);
		graphics.DrawPath(pen2, graphicsPath);
	}
}
