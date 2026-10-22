using System;
using System.Drawing;
using System.Windows.Forms;

namespace GLMapPacker;

public class LabeledField : Panel, IThemeable
{
	private Label label;

	private TextBox box;

	public string LabelText
	{
		get
		{
			return label.Text;
		}
		set
		{
			label.Text = value;
		}
	}

	public string Value
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

	public TextBox Box => box;

	public event EventHandler ValueChanged;

	public LabeledField(string labelText)
	{
		SetStyle(ControlStyles.SupportsTransparentBackColor, value: true);
		BackColor = Color.Transparent;
		base.Height = 52;
		label = new Label
		{
			Text = labelText,
			AutoSize = true,
			Location = new Point(2, 0),
			Font = new Font("Segoe UI", 9f)
		};
		box = new TextBox
		{
			BorderStyle = BorderStyle.FixedSingle,
			Location = new Point(2, 22),
			Font = new Font("Segoe UI", 10f)
		};
		box.TextChanged += delegate
		{
			if (this.ValueChanged != null)
			{
				this.ValueChanged(this, EventArgs.Empty);
			}
		};
		base.Controls.Add(label);
		base.Controls.Add(box);
		base.Resize += delegate
		{
			box.Width = base.Width - 4;
		};
	}

	public void ApplyTheme(Palette p)
	{
		label.ForeColor = p.TextMuted;
		label.BackColor = Color.Transparent;
		box.BackColor = p.SurfaceAlt;
		box.ForeColor = p.Text;
		Invalidate();
	}
}
