using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

// Compact pill with a few options, styled like the preview's RGB/R/G/B/A switch.
public class SegmentedToggle : Control, IThemeable
{
	private string[] options = new string[0];

	private int selected;

	private int hoverIndex = -1;

	public string[] Options
	{
		get
		{
			return options;
		}
		set
		{
			options = value ?? new string[0];
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
			if (value != selected && value >= 0 && value < options.Length)
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

	public event EventHandler SelectedIndexChanged;

	public SegmentedToggle()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		base.Height = Ui.S(32);
	}

	public void ApplyTheme(Palette p)
	{
		Invalidate();
	}

	private Rectangle SegmentRect(int i)
	{
		int inset = Ui.S(3);
		int w = (base.Width - inset * 2) / Math.Max(1, options.Length);
		return new Rectangle(inset + i * w, inset, w, base.Height - inset * 2 - 1);
	}

	private int IndexAt(Point p)
	{
		for (int i = 0; i < options.Length; i++)
		{
			if (SegmentRect(i).Contains(p))
			{
				return i;
			}
		}
		return -1;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		int i = IndexAt(e.Location);
		if (i != hoverIndex)
		{
			hoverIndex = i;
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
		int i = IndexAt(e.Location);
		if (i >= 0)
		{
			SelectedIndex = i;
		}
		base.OnMouseDown(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(GfxUtil.ResolveBack(this));
		Rectangle track = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		GfxUtil.FillRounded(graphics, track, track.Height / 2, current.SurfaceAlt);
		GfxUtil.DrawRounded(graphics, track, track.Height / 2, current.Border);
		for (int i = 0; i < options.Length; i++)
		{
			Rectangle r = SegmentRect(i);
			if (i == selected)
			{
				GfxUtil.FillRounded(graphics, r, r.Height / 2, current.Accent);
			}
			else if (i == hoverIndex)
			{
				GfxUtil.FillRounded(graphics, r, r.Height / 2, Theme.Mix(current.SurfaceAlt, current.Text, 0.07));
			}
			Color fore = i == selected ? current.AccentText : (i == hoverIndex ? current.Text : current.TextMuted);
			TextRenderer.DrawText(graphics, options[i], Ui.Label, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		}
	}
}
