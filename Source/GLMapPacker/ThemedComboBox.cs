using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

// Fully custom drop-down list. Unlike a native ComboBox, Windows never paints any part of it, so
// there is no flash of the system style on hover, focus or open. The list opens in a themed popup.
public class ThemedComboBox : Control, IThemeable
{
	private readonly List<object> items = new List<object>();

	private int selected = -1;

	private bool hover;

	private Popup popup;

	private readonly Stopwatch sinceClosed = Stopwatch.StartNew();

	public List<object> Items => items;

	public int SelectedIndex
	{
		get
		{
			return selected;
		}
		set
		{
			int v = (value < -1 || value >= items.Count) ? -1 : value;
			if (v != selected)
			{
				selected = v;
				Invalidate();
				if (this.SelectedIndexChanged != null)
				{
					this.SelectedIndexChanged(this, EventArgs.Empty);
				}
			}
		}
	}

	public object SelectedItem => selected >= 0 ? items[selected] : null;

	public override string Text
	{
		get
		{
			return selected >= 0 ? GetItemText(items[selected]) : "";
		}
		set
		{
			int i = items.FindIndex(o => GetItemText(o) == value);
			if (i >= 0)
			{
				SelectedIndex = i;
			}
		}
	}

	public bool DroppedDown => popup != null && popup.Visible;

	// Visible rows before the list scrolls.
	public int MaxDropDownItems { get; set; }

	public event EventHandler SelectedIndexChanged;

	public ThemedComboBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, value: true);
		BackColor = Color.Transparent;
		Cursor = Cursors.Hand;
		Font = Ui.Body;
		TabStop = true;
		MaxDropDownItems = 10;
		base.Height = Ui.S(34);
	}

	public string GetItemText(object item)
	{
		return item?.ToString() ?? "";
	}

	public void ApplyTheme(Palette p)
	{
		Invalidate();
		popup?.Invalidate(invalidateChildren: true);
	}

	// ---------------------------------------------------------------- open / close

	public void ShowDropDown()
	{
		if (items.Count == 0 || DroppedDown)
		{
			return;
		}
		if (popup == null)
		{
			popup = new Popup(this);
			popup.Closed += delegate
			{
				sinceClosed.Restart();
				Invalidate();
			};
		}
		popup.Open();
		Invalidate();
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left)
		{
			Focus();
			// A click on the box while the list is open first closes it (as a click outside the popup);
			// don't reopen it straight away.
			if (sinceClosed.ElapsedMilliseconds > 200)
			{
				ShowDropDown();
			}
		}
		base.OnMouseDown(e);
	}

	protected override bool IsInputKey(Keys keyData)
	{
		Keys key = keyData & Keys.KeyCode;
		return key == Keys.Up || key == Keys.Down || base.IsInputKey(keyData);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		switch (e.KeyCode)
		{
		case Keys.Down:
			if (e.Alt)
			{
				ShowDropDown();
			}
			else
			{
				SelectedIndex = Math.Min(items.Count - 1, selected + 1);
			}
			e.Handled = true;
			break;
		case Keys.Up:
			SelectedIndex = Math.Max(0, selected - 1);
			e.Handled = true;
			break;
		case Keys.F4:
		case Keys.Enter:
		case Keys.Space:
			ShowDropDown();
			e.Handled = true;
			break;
		}
		base.OnKeyDown(e);
	}

	protected override void OnMouseEnter(EventArgs e)
	{
		hover = true;
		Invalidate();
		base.OnMouseEnter(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		hover = false;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnGotFocus(EventArgs e)
	{
		Invalidate();
		base.OnGotFocus(e);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		Invalidate();
		base.OnLostFocus(e);
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		Invalidate();
		base.OnEnabledChanged(e);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			popup?.Dispose();
		}
		base.Dispose(disposing);
	}

	// ---------------------------------------------------------------- painting

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics g = e.Graphics;
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.Clear(GfxUtil.ResolveBack(this));
		Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		int radius = Ui.S(7);
		bool active = Focused || DroppedDown;
		Color fill = hover && Enabled ? Theme.Mix(current.SurfaceAlt, current.Accent, 0.07) : current.SurfaceAlt;
		Color border = active ? current.Accent : (hover && Enabled ? Theme.Mix(current.Border, current.Accent, 0.5) : current.Border);
		GfxUtil.FillRounded(g, r, radius, fill);
		GfxUtil.DrawRounded(g, r, radius, border, active ? Ui.S(1.4f) : 1f);
		Rectangle tr = new Rectangle(Ui.S(10), 0, base.Width - Ui.S(36), base.Height);
		TextRenderer.DrawText(g, Text, Font, tr, Enabled ? current.Text : current.TextMuted, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
		float cx = base.Width - Ui.S(16);
		float cy = base.Height / 2f;
		float s = Ui.S(4f);
		if (DroppedDown)
		{
			// Chevron points up while the list is open.
			using Pen pen = new Pen(current.Accent, Ui.S(1.6f));
			pen.StartCap = LineCap.Round;
			pen.EndCap = LineCap.Round;
			g.DrawLines(pen, new PointF[3]
			{
				new PointF(cx - s, cy + s / 2f),
				new PointF(cx, cy - s / 2f),
				new PointF(cx + s, cy + s / 2f)
			});
		}
		else
		{
			GfxUtil.DrawChevron(g, current.TextMuted, cx, cy, s);
		}
	}

	// ---------------------------------------------------------------- popup

	private sealed class Popup : ToolStripDropDown
	{
		private readonly ThemedComboBox owner;

		private readonly ItemList list;

		private readonly ToolStripControlHost host;

		public Popup(ThemedComboBox owner)
		{
			this.owner = owner;
			AutoSize = false;
			Padding = Padding.Empty;
			Margin = Padding.Empty;
			DropShadowEnabled = false;
			list = new ItemList(owner, this);
			host = new ToolStripControlHost(list)
			{
				AutoSize = false,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			Items.Add(host);
		}

		public void Open()
		{
			int rowH = ItemList.RowHeight;
			int visible = Math.Min(owner.items.Count, Math.Max(1, owner.MaxDropDownItems));
			int pad = Ui.S(4);
			Size size = new Size(owner.Width, visible * rowH + pad * 2);
			list.Size = size;
			host.Size = size;
			base.Size = size;
			using (GraphicsPath path = GfxUtil.RoundedRect(new Rectangle(0, 0, size.Width, size.Height), Ui.S(8)))
			{
				Region?.Dispose();
				Region = new Region(path);
			}
			list.Prepare();
			// Open below the box, or above it when there isn't room on screen.
			Point below = owner.PointToScreen(new Point(0, owner.Height + Ui.S(4)));
			Rectangle screen = Screen.FromControl(owner).WorkingArea;
			if (below.Y + size.Height > screen.Bottom)
			{
				Show(owner, new Point(0, -size.Height - Ui.S(4)));
			}
			else
			{
				Show(owner, new Point(0, owner.Height + Ui.S(4)));
			}
			list.Bounds = base.ClientRectangle;
			list.Prepare();
			list.Focus();
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			e.Graphics.Clear(Theme.Current.Surface);
		}

		// Some ToolStrip layouts offset or centre the hosted control; keep the list filling the popup.
		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);
			if (list != null && list.Bounds != base.ClientRectangle)
			{
				list.Bounds = base.ClientRectangle;
			}
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (list.HandleKey(keyData))
			{
				return true;
			}
			return base.ProcessDialogKey(keyData);
		}
	}

	private sealed class ItemList : Control
	{
		private readonly ThemedComboBox owner;

		private readonly Popup popup;

		private int hoverIndex = -1;

		private int scroll;

		public static int RowHeight => Ui.S(30);

		private int Pad => Ui.S(4);

		public ItemList(ThemedComboBox owner, Popup popup)
		{
			this.owner = owner;
			this.popup = popup;
			SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, value: true);
			Cursor = Cursors.Hand;
		}

		private int VisibleRows => Math.Max(1, (base.Height - Pad * 2) / RowHeight);

		private int MaxScroll => Math.Max(0, owner.items.Count - VisibleRows);

		public void Prepare()
		{
			hoverIndex = owner.selected;
			scroll = Math.Max(0, Math.Min(MaxScroll, owner.selected - VisibleRows / 2));
			Invalidate();
		}

		private int IndexAt(int y)
		{
			int i = (y - Pad) / RowHeight + scroll;
			return (y < Pad || i < 0 || i >= owner.items.Count) ? (-1) : i;
		}

		private void Commit(int index)
		{
			if (index >= 0)
			{
				owner.SelectedIndex = index;
			}
			popup.Close(ToolStripDropDownCloseReason.ItemClicked);
		}

		public bool HandleKey(Keys keyData)
		{
			switch (keyData & Keys.KeyCode)
			{
			case Keys.Down:
				MoveHover(1);
				return true;
			case Keys.Up:
				MoveHover(-1);
				return true;
			case Keys.Enter:
			case Keys.Space:
				Commit(hoverIndex);
				return true;
			case Keys.Escape:
			case Keys.F4:
				popup.Close(ToolStripDropDownCloseReason.Keyboard);
				return true;
			default:
				return false;
			}
		}

		private void MoveHover(int delta)
		{
			hoverIndex = Math.Max(0, Math.Min(owner.items.Count - 1, (hoverIndex < 0 ? owner.selected : hoverIndex) + delta));
			if (hoverIndex < scroll)
			{
				scroll = hoverIndex;
			}
			else if (hoverIndex >= scroll + VisibleRows)
			{
				scroll = hoverIndex - VisibleRows + 1;
			}
			Invalidate();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			int i = IndexAt(e.Y);
			if (i != hoverIndex)
			{
				hoverIndex = i;
				Invalidate();
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				Commit(IndexAt(e.Y));
			}
			base.OnMouseUp(e);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			scroll = Math.Max(0, Math.Min(MaxScroll, scroll - Math.Sign(e.Delta)));
			hoverIndex = IndexAt(e.Y);
			Invalidate();
			base.OnMouseWheel(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Palette current = Theme.Current;
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.Clear(current.Surface);
			Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
			GfxUtil.DrawRounded(g, r, Ui.S(8), current.Border);
			int rowH = RowHeight;
			int pad = Pad;
			int last = Math.Min(owner.items.Count, scroll + VisibleRows);
			for (int i = scroll; i < last; i++)
			{
				Rectangle row = new Rectangle(pad, pad + (i - scroll) * rowH, base.Width - pad * 2 - 1, rowH);
				bool isSel = i == owner.selected;
				bool isHover = i == hoverIndex;
				if (isHover)
				{
					GfxUtil.FillRounded(g, row, Ui.S(6), Theme.Mix(current.Surface, current.Accent, current.IsDark ? 0.22 : 0.12));
				}
				else if (isSel)
				{
					GfxUtil.FillRounded(g, row, Ui.S(6), Theme.Mix(current.Surface, current.Accent, 0.08));
				}
				Rectangle text = new Rectangle(row.X + Ui.S(10), row.Y, row.Width - Ui.S(36), row.Height);
				TextRenderer.DrawText(g, owner.GetItemText(owner.items[i]), owner.Font, text, isSel ? current.Accent : current.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
				if (isSel)
				{
					// Check mark on the current choice.
					using Pen pen = new Pen(current.Accent, Ui.S(1.8f));
					pen.StartCap = LineCap.Round;
					pen.EndCap = LineCap.Round;
					pen.LineJoin = LineJoin.Round;
					float cx = row.Right - Ui.S(16);
					float cy = row.Y + row.Height / 2f;
					float s = Ui.S(4f);
					g.DrawLines(pen, new PointF[3]
					{
						new PointF(cx - s, cy),
						new PointF(cx - s * 0.3f, cy + s * 0.75f),
						new PointF(cx + s, cy - s * 0.75f)
					});
				}
			}
			if (MaxScroll > 0)
			{
				// Thin scroll indicator on the right.
				int trackH = base.Height - pad * 4;
				int thumbH = Math.Max(Ui.S(16), trackH * VisibleRows / owner.items.Count);
				int thumbY = pad * 2 + (trackH - thumbH) * scroll / MaxScroll;
				GfxUtil.FillRounded(g, new Rectangle(base.Width - Ui.S(6), thumbY, Ui.S(3), thumbH), Ui.S(1), Theme.Mix(current.Border, current.TextMuted, 0.4));
			}
		}
	}
}
