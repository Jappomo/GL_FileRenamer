using System;
using System.Drawing;
using System.Windows.Forms;

namespace GLMapPacker;

// Small caption above a themed text input.
public class LabeledField : Panel, IThemeable
{
	private readonly Label label;

	private readonly ThemedTextBox input;

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
			return input.Text;
		}
		set
		{
			input.Text = value;
		}
	}

	public TextBox Box => input.Box;

	public event EventHandler ValueChanged;

	public LabeledField(string labelText)
	{
		SetStyle(ControlStyles.SupportsTransparentBackColor, value: true);
		BackColor = Color.Transparent;
		base.Height = Ui.S(56);
		label = new Label
		{
			Text = labelText,
			AutoSize = true,
			Location = new Point(0, 0),
			Font = Ui.Small
		};
		input = new ThemedTextBox
		{
			Location = new Point(0, Ui.S(20))
		};
		input.TextChanged += delegate
		{
			if (this.ValueChanged != null)
			{
				this.ValueChanged(this, EventArgs.Empty);
			}
		};
		base.Controls.Add(label);
		base.Controls.Add(input);
		base.Resize += delegate
		{
			input.Width = base.Width;
		};
	}

	public void ApplyTheme(Palette p)
	{
		label.ForeColor = p.TextMuted;
		label.BackColor = Color.Transparent;
		Invalidate();
	}
}
