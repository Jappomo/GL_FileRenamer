using System.Drawing;
using System.Windows.Forms;

namespace GLMapPacker;

public class ThemedComboBox : ComboBox, IThemeable
{
	public ThemedComboBox()
	{
		base.DrawMode = DrawMode.OwnerDrawFixed;
		base.DropDownStyle = ComboBoxStyle.DropDownList;
		base.FlatStyle = FlatStyle.Flat;
		base.ItemHeight = 24;
		Font = new Font("Segoe UI", 10f);
	}

	public void ApplyTheme(Palette p)
	{
		BackColor = p.SurfaceAlt;
		ForeColor = p.Text;
		Invalidate();
	}

	protected override void OnDrawItem(DrawItemEventArgs e)
	{
		Palette current = Theme.Current;
		if (e.Index >= 0)
		{
			bool flag = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
			using (SolidBrush brush = new SolidBrush((!flag) ? current.SurfaceAlt : current.Accent))
			{
				e.Graphics.FillRectangle(brush, e.Bounds);
			}
			Color foreColor = ((!flag) ? current.Text : current.AccentText);
			TextRenderer.DrawText(e.Graphics, base.Items[e.Index].ToString(), Font, new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height), foreColor, TextFormatFlags.VerticalCenter);
		}
	}
}
