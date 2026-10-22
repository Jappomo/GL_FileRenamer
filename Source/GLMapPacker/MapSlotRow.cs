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
		base.Height = 46;
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
			Size = new Size(26, 26),
			Font = new Font("Segoe UI", 12f),
			Visible = false
		};
		clearBtn.Click += delegate
		{
			Clear();
		};
		base.Controls.Add(clearBtn);
		base.Resize += delegate
		{
			clearBtn.Location = new Point(base.Width - clearBtn.Width - 8, (base.Height - clearBtn.Height) / 2);
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
		int num = 150;
		using (Font font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold))
		{
			TextRenderer.DrawText(graphics, MaterialPreset.Label(Slot), font, new Rectangle(2, 0, num - 8, base.Height), current.Text, TextFormatFlags.VerticalCenter);
		}
		int num2 = num;
		int num3 = base.Width - clearBtn.Width - 16;
		Rectangle r = new Rectangle(num2, 6, num3 - num2, base.Height - 12);
		bool flag = !string.IsNullOrEmpty(FilePath);
		using (GraphicsPath path = GfxUtil.RoundedRect(r, 8))
		{
			Color color = ((!dragHover) ? current.SurfaceAlt : Theme.Mix(current.SurfaceAlt, current.Accent, 0.28));
			using (SolidBrush brush = new SolidBrush(color))
			{
				graphics.FillPath(brush, path);
			}
			Color color2 = (dragHover ? current.Accent : ((!flag) ? Theme.Mix(current.Border, current.SurfaceAlt, 0.3) : current.Border));
			float width = ((!dragHover) ? 1f : 1.6f);
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
		Rectangle bounds = new Rectangle(r.X + 12, r.Y, r.Width - 20, r.Height);
		using Font font2 = new Font("Segoe UI", 9.5f, (!flag) ? FontStyle.Italic : FontStyle.Regular);
		TextRenderer.DrawText(graphics, text, font2, bounds, foreColor, TextFormatFlags.PathEllipsis | TextFormatFlags.VerticalCenter);
	}
}
