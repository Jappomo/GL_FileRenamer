using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace GLMapPacker;

public class MapSlotRow : Control, IThemeable
{
	private IconButton clearBtn;

	private bool dragHover;

	public MapType Slot { get; private set; }

	public string FilePath { get; private set; }

	public event EventHandler Changed;

	public MapSlotRow(MapType slot)
	{
		Slot = slot;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		base.Height = Ui.S(46);
		AllowDrop = true;
		base.DragEnter += OnDragEnter;
		base.DragLeave += delegate
		{
			dragHover = false;
			Invalidate();
		};
		base.DragDrop += OnDragDrop;
		clearBtn = new IconButton
		{
			Text = "×",
			Size = Ui.S(26, 26),
			Font = Ui.Font(12f),
			Visible = false
		};
		clearBtn.Click += delegate
		{
			Clear();
		};
		base.Controls.Add(clearBtn);
		base.Resize += delegate
		{
			clearBtn.Location = new Point(base.Width - clearBtn.Width - Ui.S(10), (base.Height - clearBtn.Height) / 2);
		};
	}

	public void SetPath(string path)
	{
		FilePath = path;
		clearBtn.Visible = !string.IsNullOrEmpty(path);
		Invalidate();
		if (this.Changed != null)
		{
			this.Changed(this, EventArgs.Empty);
		}
	}

	public void Clear()
	{
		SetPath(null);
	}

	private void OnDragEnter(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			e.Effect = DragDropEffects.Copy;
			dragHover = true;
			Invalidate();
		}
		else
		{
			e.Effect = DragDropEffects.None;
		}
	}

	private void OnDragDrop(object sender, DragEventArgs e)
	{
		dragHover = false;
		if (e.Data.GetData(DataFormats.FileDrop) is string[] array && array.Length > 0)
		{
			SetPath(array[0]);
		}
		Invalidate();
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
		int num = Ui.S(130);
		TextRenderer.DrawText(graphics, MaterialPreset.Label(Slot), Ui.Font(9.5f, semibold: true), new Rectangle(0, 0, num - Ui.S(8), base.Height), current.Text, TextFormatFlags.VerticalCenter);
		int num2 = num;
		int num3 = base.Width;
		Rectangle r = new Rectangle(num2, Ui.S(4), num3 - num2 - 1, base.Height - Ui.S(8));
		bool flag = !string.IsNullOrEmpty(FilePath);
		using (GraphicsPath path = GfxUtil.RoundedRect(r, Ui.S(8)))
		{
			Color color = ((!dragHover) ? current.SurfaceAlt : Theme.Mix(current.SurfaceAlt, current.Accent, 0.28));
			using (SolidBrush brush = new SolidBrush(color))
			{
				graphics.FillPath(brush, path);
			}
			Color color2 = (dragHover ? current.Accent : ((!flag) ? Theme.Mix(current.Border, current.TextMuted, 0.3) : current.Border));
			float width = ((!dragHover) ? 1f : Ui.S(1.6f));
			if (!flag && !dragHover)
			{
				Pen pen = new Pen(color2, width);
				pen.DashStyle = DashStyle.Dash;
				using Pen pen2 = pen;
				graphics.DrawPath(pen2, path);
			}
			else
			{
				using Pen pen3 = new Pen(color2, width);
				graphics.DrawPath(pen3, path);
			}
		}
		string text = ((!flag) ? "drop a texture here" : Path.GetFileName(FilePath));
		Color foreColor = ((!flag) ? current.TextMuted : current.Text);
		Rectangle bounds = new Rectangle(r.X + Ui.S(12), r.Y, r.Width - Ui.S(24) - (flag ? clearBtn.Width : 0), r.Height);
		TextRenderer.DrawText(graphics, text, Ui.Body, bounds, foreColor, TextFormatFlags.PathEllipsis | TextFormatFlags.VerticalCenter);
	}
}
