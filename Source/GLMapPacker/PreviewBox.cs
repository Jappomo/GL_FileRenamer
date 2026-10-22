using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class PreviewBox : Control, IThemeable
{
	private Image image;

	public Image Image
	{
		get
		{
			return image;
		}
		set
		{
			image = value;
			Invalidate();
		}
	}

	public string EmptyText { get; set; }

	public int CornerRadius { get; set; }

	public PreviewBox()
	{
		CornerRadius = 10;
		EmptyText = "drop or load an image";
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
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
		using GraphicsPath graphicsPath = GfxUtil.RoundedRect(rectangle, CornerRadius);
		using (SolidBrush brush = new SolidBrush(current.SurfaceAlt))
		{
			graphics.FillPath(brush, graphicsPath);
		}
		Region clip = graphics.Clip;
		graphics.SetClip(graphicsPath);
		if (image != null)
		{
			int num = 8;
			using (SolidBrush solidBrush = new SolidBrush(Theme.Mix(current.SurfaceAlt, current.Surface, 0.6)))
			{
				using SolidBrush solidBrush2 = new SolidBrush(current.SurfaceAlt);
				for (int i = 0; i < base.Height; i += num)
				{
					for (int j = 0; j < base.Width; j += num)
					{
						graphics.FillRectangle(((j / num + i / num) % 2 != 0) ? solidBrush2 : solidBrush, j, i, num, num);
					}
				}
			}
			float num2 = (float)image.Width / (float)image.Height;
			float num3 = (float)base.Width / (float)base.Height;
			int num4;
			int num5;
			if (num2 > num3)
			{
				num4 = base.Width - 8;
				num5 = (int)((float)num4 / num2);
			}
			else
			{
				num5 = base.Height - 8;
				num4 = (int)((float)num5 * num2);
			}
			int x = (base.Width - num4) / 2;
			int y = (base.Height - num5) / 2;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.DrawImage(image, x, y, num4, num5);
		}
		else
		{
			using Font font = new Font("Segoe UI", 9.5f);
			TextRenderer.DrawText(graphics, EmptyText, font, rectangle, current.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
		}
		graphics.Clip = clip;
		using Pen pen = new Pen(current.Border, 1f);
		graphics.DrawPath(pen, graphicsPath);
	}
}
