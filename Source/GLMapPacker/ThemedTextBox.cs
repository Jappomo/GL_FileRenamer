using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

// Borderless TextBox inside a painted rounded field; the border turns accent while focused.
public class ThemedTextBox : Control, IThemeable
{
	private readonly TextBox box;

	public TextBox Box => box;

	public override string Text
	{
		get
		{
			return box.Text;
		}
		set
		{
			box.Text = value;
		}
	}

	public ThemedTextBox()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Color.Transparent;
		box = new TextBox
		{
			BorderStyle = BorderStyle.None,
			Font = Ui.Body
		};
		box.TextChanged += delegate
		{
			OnTextChanged(EventArgs.Empty);
		};
		box.GotFocus += delegate
		{
			Invalidate();
		};
		box.LostFocus += delegate
		{
			Invalidate();
		};
		base.Controls.Add(box);
		base.Height = Ui.S(34);
		Cursor = Cursors.IBeam;
	}

	protected override void OnClick(EventArgs e)
	{
		box.Focus();
		base.OnClick(e);
	}

	protected override void OnLayout(LayoutEventArgs levent)
	{
		int pad = Ui.S(10);
		box.Location = new Point(pad, (base.Height - box.Height) / 2);
		box.Width = Math.Max(10, base.Width - pad * 2);
		base.OnLayout(levent);
	}

	public void ApplyTheme(Palette p)
	{
		box.BackColor = p.SurfaceAlt;
		box.ForeColor = box.ReadOnly ? p.TextMuted : p.Text;
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Palette current = Theme.Current;
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.Clear(GfxUtil.ResolveBack(this));
		Rectangle r = new Rectangle(0, 0, base.Width - 1, base.Height - 1);
		GfxUtil.FillRounded(graphics, r, Ui.S(7), current.SurfaceAlt);
		GfxUtil.DrawRounded(graphics, r, Ui.S(7), box.Focused ? current.Accent : current.Border, box.Focused ? Ui.S(1.5f) : 1f);
	}
}
