using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class TabStrip : Control, IThemeable
{
	private string[] tabs = new string[0];

	private int selected;

	private int hoverIndex = -1;

	public string[] Tabs
	{
		get
		{
			return tabs;
		}
		set
		{
			tabs = value ?? new string[0];
			Invalidate();
		}
	}

	public int SelectedIndex
	{
		get
		{
			return selected;
		}
		set
		{
			if (value != selected)
			{
				selected = value;
				Invalidate();
				if (this.SelectedIndexChanged != null)
				{
					this.SelectedIndexChanged(this, EventArgs.Empty);
				}
			}
		}
	}

	public int PillWidth { get; set; }

	public event EventHandler SelectedIndexChanged;

	public TabStrip()
	{
		PillWidth = 110;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		base.Height = 40;
	}

	public void ApplyTheme(Palette p)
	{
		Invalidate();
	}

	private int IndexAt(int x)
	{
		int num = x / (PillWidth + 8);
		return (num < 0 || num >= tabs.Length) ? (-1) : num;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		int num = IndexAt(e.X);
		if (num != hoverIndex)
		{
			hoverIndex = num;
			Invalidate();
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		hoverIndex = -1;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		int num = IndexAt(e.X);
		if (num >= 0)
		{
			SelectedIndex = num;
		}
		base.OnMouseDown(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(GfxUtil.ResolveBack(this));
		using Font font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
		for (int i = 0; i < tabs.Length; i++)
		{
			Rectangle rectangle = new Rectangle(i * (PillWidth + 8), 4, PillWidth, base.Height - 10);
			if (i == selected)
			{
				using (GraphicsPath path = GfxUtil.RoundedRect(rectangle, 9))
				{
					using SolidBrush brush = new SolidBrush(current.Accent);
					graphics.FillPath(brush, path);
				}
				TextRenderer.DrawText(graphics, tabs[i], font, rectangle, current.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
				continue;
			}
			if (i == hoverIndex)
			{
				using GraphicsPath path2 = GfxUtil.RoundedRect(rectangle, 9);
				using SolidBrush brush2 = new SolidBrush(current.SurfaceAlt);
				graphics.FillPath(brush2, path2);
			}
			TextRenderer.DrawText(graphics, tabs[i], font, rectangle, current.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
		}
	}
}
