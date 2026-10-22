using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

// Segmented tab control: the selected pill slides to the clicked tab.
public class TabStrip : Control, IThemeable
{
	private string[] tabs = new string[0];

	private int selected;

	private int hoverIndex = -1;

	private readonly Tween slide;

	private int Inset => Ui.S(4);

	public string[] Tabs
	{
		get
		{
			return tabs;
		}
		set
		{
			tabs = value ?? new string[0];
			base.Width = PreferredWidth;
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
				slide.To(value);
				if (this.SelectedIndexChanged != null)
				{
					this.SelectedIndexChanged(this, EventArgs.Empty);
				}
			}
		}
	}

	public int PillWidth { get; set; }

	public int PreferredWidth => tabs.Length * PillWidth + Inset * 2;

	public event EventHandler SelectedIndexChanged;

	public TabStrip()
	{
		PillWidth = Ui.S(96);
		slide = new Tween(this, 220f);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		base.Height = Ui.S(38);
	}

	public void ApplyTheme(Palette p)
	{
		Invalidate();
	}

	private int IndexAt(int x)
	{
		int num = (x - Inset) / PillWidth;
		return (x < Inset || num >= tabs.Length) ? (-1) : num;
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

	private Rectangle PillRect(float index)
	{
		return new Rectangle(Inset + (int)Math.Round(index * PillWidth), Inset, PillWidth, base.Height - Inset * 2 - 1);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(GfxUtil.ResolveBack(this));
		Rectangle track = new Rectangle(0, 0, PreferredWidth - 1, base.Height - 1);
		int radius = Ui.S(10);
		GfxUtil.FillRounded(graphics, track, radius, current.SurfaceAlt);
		GfxUtil.DrawRounded(graphics, track, radius, current.Border);
		if (hoverIndex >= 0 && hoverIndex != selected)
		{
			GfxUtil.FillRounded(graphics, PillRect(hoverIndex), radius - Inset, Theme.Mix(current.SurfaceAlt, current.Text, 0.06));
		}
		GfxUtil.FillRounded(graphics, PillRect(slide.Value), radius - Inset, current.Accent);
		Font font = Ui.Strong;
		for (int i = 0; i < tabs.Length; i++)
		{
			// Text turns white as the sliding pill passes underneath it.
			float under = Math.Max(0f, 1f - Math.Abs(slide.Value - i));
			Color color = Theme.Mix(i == hoverIndex ? current.Text : current.TextMuted, current.AccentText, under);
			TextRenderer.DrawText(graphics, tabs[i], font, PillRect(i), color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
		}
	}
}
