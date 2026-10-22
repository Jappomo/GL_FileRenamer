using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class TitledCard : Panel, IThemeable
{
	public string Title { get; set; }

	public int CornerRadius { get; set; }

	public TitledCard()
	{
		CornerRadius = 12;
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
		using (GraphicsPath path = GfxUtil.RoundedRect(r, CornerRadius))
		{
			using (SolidBrush brush = new SolidBrush(current.Surface))
			{
				graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(current.Border, 1f);
			graphics.DrawPath(pen, path);
		}
		if (!string.IsNullOrEmpty(Title))
		{
			using (Font font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold))
			{
				TextRenderer.DrawText(graphics, Title, font, new Point(16, 12), current.Text);
			}
		}
	}
}
