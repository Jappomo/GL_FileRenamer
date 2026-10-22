using System.Drawing;
using System.Windows.Forms;

namespace GLMapPacker;

public static class ThemeManager
{
	public static void Apply(Control root)
	{
		Palette current = Theme.Current;
		ApplyRecursive(root, current);
		root.Invalidate(invalidateChildren: true);
	}

	private static void ApplyRecursive(Control c, Palette p)
	{
		if (c is IThemeable themeable)
		{
			themeable.ApplyTheme(p);
		}
		else
		{
			StyleStandardControl(c, p);
		}
		foreach (Control control in c.Controls)
		{
			ApplyRecursive(control, p);
		}
	}

	private static void StyleStandardControl(Control c, Palette p)
	{
		if (c is Form form)
		{
			form.BackColor = p.AppBackground;
			form.ForeColor = p.Text;
		}
		else if (c is Label)
		{
			c.ForeColor = ((!(c is LinkLabel)) ? p.Text : p.Accent);
			c.BackColor = Color.Transparent;
		}
		else if (c is Panel || c is FlowLayoutPanel || c is TableLayoutPanel)
		{
			if ("surface".Equals(c.Tag))
			{
				c.BackColor = p.Surface;
			}
			else if ("appbg".Equals(c.Tag))
			{
				c.BackColor = p.AppBackground;
			}
			c.ForeColor = p.Text;
		}
		else if (c is TextBox textBox)
		{
			textBox.BackColor = p.SurfaceAlt;
			textBox.ForeColor = p.Text;
			textBox.BorderStyle = BorderStyle.FixedSingle;
		}
		else if (c is ComboBox comboBox)
		{
			comboBox.FlatStyle = FlatStyle.Flat;
			comboBox.BackColor = p.SurfaceAlt;
			comboBox.ForeColor = p.Text;
		}
		else
		{
			c.ForeColor = p.Text;
		}
	}
}
