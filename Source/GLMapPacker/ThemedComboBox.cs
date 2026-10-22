using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

// Drop-down list whose closed box is painted by us (rounded field + chevron) instead of the native frame.
public class ThemedComboBox : ComboBox, IThemeable
{
	private const int WM_PAINT = 0x000F;

	private bool hover;

	public ThemedComboBox()
	{
		base.DrawMode = DrawMode.OwnerDrawFixed;
		base.DropDownStyle = ComboBoxStyle.DropDownList;
		base.FlatStyle = FlatStyle.Flat;
		base.ItemHeight = Ui.S(26);
		Font = Ui.Body;
		Cursor = Cursors.Hand;
	}

	public void ApplyTheme(Palette p)
	{
		BackColor = p.SurfaceAlt;
		ForeColor = p.Text;
		Invalidate();
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

	protected override void OnSelectedIndexChanged(EventArgs e)
	{
		Invalidate();
		base.OnSelectedIndexChanged(e);
	}

	protected override void OnDropDownClosed(EventArgs e)
	{
		Invalidate();
		base.OnDropDownClosed(e);
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

	protected override void WndProc(ref Message m)
	{
		base.WndProc(ref m);
		if (m.Msg == WM_PAINT && IsHandleCreated)
		{
			using Graphics g = Graphics.FromHwnd(base.Handle);
			PaintFace(g);
		}
	}

	private void PaintFace(Graphics g)
	{
		Palette current = Theme.Current;
		Rectangle client = base.ClientRectangle;
		using Bitmap buffer = new Bitmap(Math.Max(1, client.Width), Math.Max(1, client.Height));
		using (Graphics bg = Graphics.FromImage(buffer))
		{
			bg.SmoothingMode = SmoothingMode.AntiAlias;
			bg.Clear(GfxUtil.ResolveBack(this));
			Rectangle r = new Rectangle(0, 0, client.Width - 1, client.Height - 1);
			int radius = Ui.S(7);
			bool active = Focused || DroppedDown;
			Color fill = hover ? Theme.Mix(current.SurfaceAlt, current.Accent, 0.07) : current.SurfaceAlt;
			Color border = active ? current.Accent : (hover ? Theme.Mix(current.Border, current.Accent, 0.5) : current.Border);
			GfxUtil.FillRounded(bg, r, radius, fill);
			GfxUtil.DrawRounded(bg, r, radius, border);
			string text = SelectedIndex >= 0 ? GetItemText(SelectedItem) : "";
			Rectangle tr = new Rectangle(Ui.S(10), 0, client.Width - Ui.S(36), client.Height);
			TextRenderer.DrawText(bg, text, Font, tr, Enabled ? current.Text : current.TextMuted, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
			GfxUtil.DrawChevron(bg, current.TextMuted, client.Width - Ui.S(16), client.Height / 2f, Ui.S(4f));
		}
		g.DrawImageUnscaled(buffer, 0, 0);
	}

	protected override void OnDrawItem(DrawItemEventArgs e)
	{
		Palette current = Theme.Current;
		if (e.Index < 0)
		{
			return;
		}
		// The closed box is drawn by PaintFace; this only draws rows in the open list.
		if ((e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit)
		{
			return;
		}
		bool flag = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
		using (SolidBrush brush = new SolidBrush(flag ? current.Accent : current.Surface))
		{
			e.Graphics.FillRectangle(brush, e.Bounds);
		}
		Color foreColor = flag ? current.AccentText : current.Text;
		TextRenderer.DrawText(e.Graphics, base.Items[e.Index].ToString(), Font, new Rectangle(e.Bounds.X + Ui.S(10), e.Bounds.Y, e.Bounds.Width - Ui.S(10), e.Bounds.Height), foreColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
	}
}
