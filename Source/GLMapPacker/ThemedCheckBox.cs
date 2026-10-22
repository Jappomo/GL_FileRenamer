using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class ThemedCheckBox : CheckBox, IThemeable
{
	public ThemedCheckBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Font = new Font("Segoe UI", 10f);
		base.Height = 26;
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
		int num = 20;
		int y = (base.Height - num) / 2;
		Rectangle r = new Rectangle(0, y, num, num);
		using (GraphicsPath path = GfxUtil.RoundedRect(r, 5))
		{
			Color color = ((!base.Checked) ? current.SurfaceAlt : current.Accent);
			Color color2 = ((!base.Checked) ? current.Border : current.Accent);
			using (SolidBrush brush = new SolidBrush(color))
			{
				graphics.FillPath(brush, path);
			}
			using Pen pen = new Pen(color2, 1.4f);
			graphics.DrawPath(pen, path);
		}
		if (base.Checked)
		{
			using Pen pen2 = new Pen(current.AccentText, 2.2f);
			pen2.StartCap = LineCap.Round;
			pen2.EndCap = LineCap.Round;
			graphics.DrawLines(pen2, new Point[3]
			{
				new Point(r.Left + 5, r.Top + 10),
				new Point(r.Left + 9, r.Top + 14),
				new Point(r.Left + 15, r.Top + 6)
			});
		}
		TextRenderer.DrawText(bounds: new Rectangle(num + 10, 0, base.Width - num - 10, base.Height), dc: graphics, text: Text, font: Font, foreColor: current.Text, flags: TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
	}
}
