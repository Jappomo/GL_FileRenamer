using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

// Rounded surface panel with an optional title; used to group related controls.
public class TitledCard : Panel, IThemeable
{
	public string Title { get; set; }

	public int CornerRadius { get; set; }

	// Inner padding used by the layouts that place controls in a card.
	public static int Pad => Ui.S(16);

	// Y where content starts: below the title when there is one.
	public int ContentTop => string.IsNullOrEmpty(Title) ? Pad : Ui.S(46);

	public TitledCard()
	{
		CornerRadius = Ui.S(12);
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
	}

	public virtual void ApplyTheme(Palette p)
	{
		Invalidate();
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(GfxUtil.ResolveBack(this));
		Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		GfxUtil.FillRounded(graphics, r, CornerRadius, current.Surface);
		GfxUtil.DrawRounded(graphics, r, CornerRadius, current.Border);
		if (!string.IsNullOrEmpty(Title))
		{
			TextRenderer.DrawText(graphics, Title, Ui.Title, new Point(Pad, Ui.S(14)), current.Text);
		}
	}
}
