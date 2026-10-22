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

	private ThemedTextBox suffixBox;

	private IconButton saveBtn;

	private Bitmap image;

	private string sourcePath;

	public string ChannelName { get; set; }

	public bool HasImage => image != null;

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
		base.CornerRadius = Ui.S(12);
		base.Padding = new Padding(Ui.S(12));
		Build(defaultSuffix);
	}

	private void Build(string defaultSuffix)
	{
		dot = new Label
		{
			AutoSize = false,
			Size = Ui.S(12, 12),
			Location = new Point(Ui.S(16), Ui.S(17))
		};
		dot.Paint += delegate(object s, PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using SolidBrush brush = new SolidBrush(DotColor);
			e.Graphics.FillEllipse(brush, 0, 0, dot.Width - 1, dot.Height - 1);
		};
		nameLabel = new Label
		{
			AutoSize = true,
			Text = ChannelName,
			Location = new Point(Ui.S(34), Ui.S(13)),
			Font = Ui.Strong
		};
		preview = new PreviewBox
		{
			EmptyText = "",
			Interactive = false
		};
		suffixBox = new ThemedTextBox
		{
			Text = defaultSuffix
		};
		saveBtn = new IconButton
		{
			Text = "↓",
			Size = Ui.S(34, 34),
			Font = Ui.Font(11f)
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
		int num = Ui.S(12);
		int num2 = Ui.S(44);
		int num3 = suffixBox.Height;
		preview.Location = new Point(num, num2);
		preview.Size = new Size(base.Width - num * 2, Math.Max(Ui.S(40), base.Height - num2 - num3 - num - Ui.S(10)));
		int num4 = base.Height - num3 - num;
		saveBtn.Location = new Point(base.Width - saveBtn.Width - num, num4);
		suffixBox.Location = new Point(num, num4);
		suffixBox.Width = base.Width - num * 2 - Ui.S(8) - saveBtn.Width;
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
				Notifier.Error("Couldn't save " + text3 + ": " + ex.Message);
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
					Notifier.Success("Saved " + Path.GetFileName(saveFileDialog.FileName));
					return;
				}
				catch (Exception ex2)
				{
					Notifier.Error("Couldn't save: " + ex2.Message);
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
