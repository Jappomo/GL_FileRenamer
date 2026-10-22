using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GLMapPacker;

public class ChannelSlot : TitledCard
{
	private PreviewBox preview;

	private ThemedComboBox pick;

	private Label dot;

	private Label nameLabel;

	private IconButton clearBtn;

	private Bitmap image;

	public Bitmap SourceImage => image;

	public string FilePath { get; private set; }

	public string ChannelName { get; set; }

	public Color DotColor { get; set; }

	public ImageCore.ChannelPick Pick => pick.SelectedIndex switch
	{
		1 => ImageCore.ChannelPick.G, 
		2 => ImageCore.ChannelPick.B, 
		3 => ImageCore.ChannelPick.A, 
		4 => ImageCore.ChannelPick.White, 
		5 => ImageCore.ChannelPick.Black, 
		_ => ImageCore.ChannelPick.R, 
	};

	public event EventHandler Changed;

	public ChannelSlot(string channelName, Color dotColor)
	{
		ChannelName = channelName;
		DotColor = dotColor;
		base.Title = null;
		base.CornerRadius = 12;
		base.Padding = new Padding(12);
		Build();
		AllowDrop = true;
		base.DragEnter += OnDragEnter;
		base.DragDrop += OnDragDrop;
	}

	private void Build()
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
		clearBtn = new IconButton
		{
			Text = "×",
			Size = new Size(24, 24),
			Font = new Font("Segoe UI", 12f)
		};
		clearBtn.Click += delegate
		{
			Clear();
		};
		preview = new PreviewBox
		{
			EmptyText = "drop image"
		};
		pick = new ThemedComboBox();
		pick.Items.AddRange(new object[6] { "Use red", "Use green", "Use blue", "Use alpha", "Constant white", "Constant black" });
		pick.SelectedIndex = 0;
		pick.SelectedIndexChanged += delegate
		{
			if (this.Changed != null)
			{
				this.Changed(this, EventArgs.Empty);
			}
		};
		base.Controls.Add(dot);
		base.Controls.Add(nameLabel);
		base.Controls.Add(clearBtn);
		base.Controls.Add(preview);
		base.Controls.Add(pick);
		base.Resize += delegate
		{
			Relayout();
		};
		base.DoubleClick += delegate
		{
			LoadDialog();
		};
		preview.DoubleClick += delegate
		{
			LoadDialog();
		};
		Relayout();
	}

	private void Relayout()
	{
		int num = 12;
		clearBtn.Location = new Point(base.Width - clearBtn.Width - num, 10);
		int num2 = 38;
		int num3 = 28;
		preview.Location = new Point(num, num2);
		preview.Size = new Size(base.Width - num * 2, base.Height - num2 - num3 - num - 6);
		pick.Location = new Point(num, base.Height - num3 - num + 2);
		pick.Size = new Size(base.Width - num * 2, num3);
	}

	public void LoadDialog()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		try
		{
			openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.tga|All files|*.*";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				LoadFile(openFileDialog.FileName);
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	public void LoadFile(string path)
	{
		try
		{
			Bitmap bitmap = ImageCore.Load(path);
			if (image != null)
			{
				image.Dispose();
			}
			image = bitmap;
			FilePath = path;
			preview.Image = image;
			if (this.Changed != null)
			{
				this.Changed(this, EventArgs.Empty);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Couldn't load that image:" + Environment.NewLine + ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	public void Clear()
	{
		if (image != null)
		{
			image.Dispose();
			image = null;
		}
		FilePath = null;
		preview.Image = null;
		if (this.Changed != null)
		{
			this.Changed(this, EventArgs.Empty);
		}
	}

	private void OnDragEnter(object sender, DragEventArgs e)
	{
		e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None);
	}

	private void OnDragDrop(object sender, DragEventArgs e)
	{
		string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
		if (array != null && array.Length > 0)
		{
			LoadFile(array[0]);
		}
	}
}
