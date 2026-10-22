using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace GLMapPacker;

public class ChannelView : TitledCard
{
	private Label dot;

	private Label nameLabel;

	private PreviewBox preview;

	private TextBox suffixBox;

	private IconButton saveBtn;

	private Bitmap image;

	private string sourcePath;

	public string ChannelName { get; set; }

	public Color DotColor { get; set; }

	public string Suffix
	{
		get
		{
			return suffixBox.Text;
		}
		set
		{
			suffixBox.Text = value;
		}
	}

	public ChannelView(string channelName, Color dotColor, string defaultSuffix)
	{
		ChannelName = channelName;
		DotColor = dotColor;
		base.CornerRadius = 12;
		base.Padding = new Padding(12);
		Build(defaultSuffix);
	}

	private void Build(string defaultSuffix)
	{
		dot = new Label
		{
			AutoSize = false,
			Size = new Size(12, 12),
			Location = new Point(14, 15)
		};
		dot.Paint += delegate(object s, PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using SolidBrush brush = new SolidBrush(DotColor);
			e.Graphics.FillEllipse(brush, 0, 0, 11, 11);
		};
		nameLabel = new Label
		{
			AutoSize = true,
			Text = ChannelName,
			Location = new Point(32, 12),
			Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold)
		};
		preview = new PreviewBox
		{
			EmptyText = ""
		};
		suffixBox = new TextBox
		{
			Text = defaultSuffix,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 9f)
		};
		saveBtn = new IconButton
		{
			Text = "↓",
			Size = new Size(30, 26),
			Font = new Font("Segoe UI", 11f)
		};
		saveBtn.Click += delegate
		{
			Save(autoSameFolder: false);
		};
		base.Controls.Add(dot);
		base.Controls.Add(nameLabel);
		base.Controls.Add(preview);
		base.Controls.Add(suffixBox);
		base.Controls.Add(saveBtn);
		base.Resize += delegate
		{
			Relayout();
		};
		Relayout();
	}

	private void Relayout()
	{
		int num = 12;
		int num2 = 38;
		int num3 = 28;
		preview.Location = new Point(num, num2);
		preview.Size = new Size(base.Width - num * 2, base.Height - num2 - num3 - num - 6);
		int num4 = base.Height - num3 - num + 2;
		saveBtn.Location = new Point(base.Width - saveBtn.Width - num, num4);
		suffixBox.Location = new Point(num, num4 + 2);
		suffixBox.Size = new Size(base.Width - num * 3 - saveBtn.Width, num3);
	}

	public void SetImage(Bitmap bmp, string sourceFilePath)
	{
		if (image != null)
		{
			image.Dispose();
		}
		image = bmp;
		sourcePath = sourceFilePath;
		preview.Image = image;
	}

	public override void ApplyTheme(Palette p)
	{
		base.ApplyTheme(p);
		if (suffixBox != null)
		{
			suffixBox.BackColor = p.SurfaceAlt;
			suffixBox.ForeColor = p.Text;
		}
		if (nameLabel != null)
		{
			nameLabel.ForeColor = p.Text;
		}
	}

	public void Save(bool autoSameFolder)
	{
		if (image == null)
		{
			return;
		}
		string text = "channel";
		string text2 = null;
		if (!string.IsNullOrEmpty(sourcePath))
		{
			try
			{
				text = Path.GetFileNameWithoutExtension(sourcePath);
				text2 = Path.GetDirectoryName(sourcePath);
			}
			catch
			{
			}
		}
		string text3 = text + Suffix + ".png";
		if (autoSameFolder && !string.IsNullOrEmpty(text2) && Directory.Exists(text2))
		{
			try
			{
				ImageCore.Save(image, Path.Combine(text2, text3), ImageCore.OutputFormat.Png);
				return;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Save failed:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		try
		{
			saveFileDialog.Filter = "PNG|*.png|TGA|*.tga|TIFF|*.tif|Bitmap|*.bmp|JPEG|*.jpg";
			saveFileDialog.FileName = text3;
			if (!string.IsNullOrEmpty(text2) && Directory.Exists(text2))
			{
				saveFileDialog.InitialDirectory = text2;
			}
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				try
				{
					ImageCore.Save(image, saveFileDialog.FileName, ImageCore.FormatFromExtension(saveFileDialog.FileName, ImageCore.OutputFormat.Png));
					return;
				}
				catch (Exception ex2)
				{
					MessageBox.Show("Save failed:" + Environment.NewLine + ex2.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}
}
