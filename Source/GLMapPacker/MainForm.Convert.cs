using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GLMapPacker;

// "Convert" tab: Normal -> Height, Height -> Normal and Height -> AO, with a live preview and 16-bit PNG output.
public partial class MainForm
{
	private const int CvPreviewSide = 512;

	private const int CvColumnWidth = 320;

	private static readonly int[] CvResolutions = new int[4] { 512, 1024, 2048, 4096 };

	private PreviewBox cvSourcePreview;

	private PreviewBox cvResultPreview;

	private ThemedComboBox cvModeCombo;

	private Panel cvPanelN2H;

	private Panel cvPanelH2N;

	private Panel cvPanelAO;

	private ThemedSlider cvIntensity;

	private ThemedSlider cvBaseline;

	private ThemedSlider cvContrast;

	private ThemedSlider cvBlur;

	private ThemedComboBox cvN2HFormatCombo;

	private ThemedSlider cvStrength;

	private ThemedSlider cvSmoothing;

	private ThemedComboBox cvH2NFormatCombo;

	private ThemedCheckBox cvInvertChk;

	private ThemedSlider cvAoRadius;

	private ThemedSlider cvAoDepth;

	private ThemedSlider cvAoIntensity;

	private ThemedSlider cvAoContrast;

	private ThemedComboBox cvAoQualityCombo;

	private ThemedComboBox cvResolutionCombo;

	private ThemedComboBox cvOutputCombo;

	private FlatButton cvLoadBtn;

	private FlatButton cvSaveBtn;

	private ThemedProgress cvProgress;

	private Label cvStatus;

	private Timer cvPreviewTimer;

	private FloatImage cvSource;

	private FloatImage cvProxy;

	private int cvProxySide = -1;

	private string cvPath;

	private int cvPreviewVersion;

	private bool cvSaving;

	private void BuildConvertPage()
	{
		pageConvert = new Panel
		{
			Dock = DockStyle.Fill
		};
		cvPreviewTimer = new Timer
		{
			Interval = 140
		};
		cvPreviewTimer.Tick += delegate
		{
			cvPreviewTimer.Stop();
			RunConvertPreview();
		};
		cvSourcePreview = new PreviewBox
		{
			EmptyText = "drop or load a normal / height map"
		};
		cvResultPreview = new PreviewBox
		{
			EmptyText = "converted result appears here"
		};
		pageConvert.Controls.Add(CvLabel("cvLabelSource", "Source"));
		pageConvert.Controls.Add(CvLabel("cvLabelResult", "Result"));
		pageConvert.Controls.Add(cvSourcePreview);
		pageConvert.Controls.Add(cvResultPreview);
		pageConvert.Controls.Add(CvLabel("cvLabelMode", "Conversion"));
		cvModeCombo = new ThemedComboBox
		{
			Width = CvColumnWidth
		};
		cvModeCombo.Items.AddRange(new object[3] { "Normal → Height", "Height → Normal (Sobel)", "Height → Ambient Occlusion (HBAO)" });
		cvModeCombo.SelectedIndex = 0;
		cvModeCombo.SelectedIndexChanged += delegate
		{
			ShowConvertMode();
			QueueConvertPreview();
		};
		pageConvert.Controls.Add(cvModeCombo);
		cvIntensity = CvSlider("Height intensity", 0.0, 4.0, 1.0, 2);
		cvBaseline = CvSlider("Mid-point baseline", 0.0, 1.0, 0.5, 2);
		cvContrast = CvSlider("Contrast", -1.0, 1.0, 0.0, 2);
		cvBlur = CvSlider("Surface blur", 0.0, 20.0, 0.0, 1, " px");
		cvN2HFormatCombo = CvYCombo();
		cvPanelN2H = CvModePanel(cvIntensity, cvBaseline, cvContrast, cvBlur, CvLabel("cvLabelN2HFmt", "Normal map format"), cvN2HFormatCombo);
		cvStrength = CvSlider("Strength", 0.0, 20.0, 2.0, 2);
		cvSmoothing = CvSlider("Smoothing", 0.0, 10.0, 0.0, 1, " px");
		cvH2NFormatCombo = CvYCombo();
		cvInvertChk = new ThemedCheckBox
		{
			Text = "Invert height",
			Width = CvColumnWidth
		};
		cvInvertChk.CheckedChanged += delegate
		{
			QueueConvertPreview();
		};
		cvPanelH2N = CvModePanel(cvStrength, cvSmoothing, CvLabel("cvLabelH2NFmt", "Output format"), cvH2NFormatCombo, cvInvertChk);
		cvAoRadius = CvSlider("Radius", 1.0, 128.0, 24.0, 0, " px");
		cvAoDepth = CvSlider("Height depth", 0.0, 20.0, 3.0, 2);
		cvAoIntensity = CvSlider("Intensity", 0.0, 3.0, 1.0, 2);
		cvAoContrast = CvSlider("Contrast", 0.2, 4.0, 1.0, 2);
		cvAoQualityCombo = new ThemedComboBox
		{
			Width = CvColumnWidth
		};
		cvAoQualityCombo.Items.AddRange(new object[3] { "Draft (8 directions)", "Standard (16 directions)", "High (32 directions)" });
		cvAoQualityCombo.SelectedIndex = 1;
		cvAoQualityCombo.SelectedIndexChanged += delegate
		{
			QueueConvertPreview();
		};
		cvPanelAO = CvModePanel(cvAoRadius, cvAoDepth, cvAoIntensity, cvAoContrast, CvLabel("cvLabelAoQ", "Quality"), cvAoQualityCombo);
		pageConvert.Controls.Add(cvPanelN2H);
		pageConvert.Controls.Add(cvPanelH2N);
		pageConvert.Controls.Add(cvPanelAO);
		pageConvert.Controls.Add(CvLabel("cvLabelRes", "Output resolution"));
		cvResolutionCombo = new ThemedComboBox
		{
			Width = 150
		};
		cvResolutionCombo.Items.Add("Source size");
		foreach (int r in CvResolutions)
		{
			cvResolutionCombo.Items.Add(r + " px");
		}
		cvResolutionCombo.SelectedIndex = 0;
		cvResolutionCombo.SelectedIndexChanged += delegate
		{
			UpdateConvertStatus();
			QueueConvertPreview();
		};
		pageConvert.Controls.Add(cvResolutionCombo);
		pageConvert.Controls.Add(CvLabel("cvLabelOut", "File format"));
		cvOutputCombo = new ThemedComboBox
		{
			Width = 150
		};
		cvOutputCombo.Items.AddRange(new object[2] { "PNG 16-bit", "PNG 8-bit" });
		cvOutputCombo.SelectedIndex = 0;
		cvOutputCombo.SelectedIndexChanged += delegate
		{
			UpdateConvertStatus();
		};
		pageConvert.Controls.Add(cvOutputCombo);
		cvStatus = new Label
		{
			AutoSize = false,
			Text = "",
			Font = new Font("Segoe UI", 9f),
			AutoEllipsis = true
		};
		pageConvert.Controls.Add(cvStatus);
		cvProgress = new ThemedProgress();
		pageConvert.Controls.Add(cvProgress);
		cvLoadBtn = new FlatButton
		{
			Text = "Load map",
			Role = FlatButton.ButtonRole.Secondary,
			Width = 150
		};
		cvLoadBtn.Click += delegate
		{
			LoadConvert();
		};
		cvSaveBtn = new FlatButton
		{
			Text = "Save result",
			Role = FlatButton.ButtonRole.Accent,
			Width = 158,
			Enabled = false
		};
		cvSaveBtn.Click += async delegate
		{
			await SaveConvert();
		};
		pageConvert.Controls.Add(cvLoadBtn);
		pageConvert.Controls.Add(cvSaveBtn);
		pageConvert.AllowDrop = true;
		pageConvert.DragEnter += delegate(object s, DragEventArgs e)
		{
			e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None);
		};
		pageConvert.DragDrop += delegate(object s, DragEventArgs e)
		{
			string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (array != null && array.Length > 0)
			{
				LoadConvertFile(array[0]);
			}
		};
		pageConvert.Resize += delegate
		{
			LayoutConvertPage();
		};
		ShowConvertMode();
	}

	private Label CvLabel(string name, string text)
	{
		return new Label
		{
			Name = name,
			AutoSize = true,
			Text = text,
			Font = new Font("Segoe UI", 9f)
		};
	}

	private ThemedSlider CvSlider(string caption, double min, double max, double value, int decimals, string suffix = "")
	{
		ThemedSlider slider = new ThemedSlider(caption, min, max, value, decimals, suffix)
		{
			Width = CvColumnWidth
		};
		slider.ValueChanged += delegate
		{
			QueueConvertPreview();
		};
		return slider;
	}

	private ThemedComboBox CvYCombo()
	{
		ThemedComboBox combo = new ThemedComboBox
		{
			Width = CvColumnWidth
		};
		combo.Items.AddRange(new object[2] { "OpenGL (Y+)", "DirectX (Y-)" });
		combo.SelectedIndex = 0;
		combo.SelectedIndexChanged += delegate
		{
			QueueConvertPreview();
		};
		return combo;
	}

	// Stacks the given controls top to bottom; a label sits tight above the control after it.
	private static Panel CvModePanel(params Control[] controls)
	{
		Panel panel = new Panel();
		int y = 0;
		foreach (Control c in controls)
		{
			c.Location = new Point(0, y);
			panel.Controls.Add(c);
			y += (c is Label) ? 20 : ((c is ComboBox) ? 42 : (c.Height + 10));
		}
		panel.Size = new Size(CvColumnWidth, y);
		return panel;
	}

	private void ShowConvertMode()
	{
		int mode = cvModeCombo.SelectedIndex;
		cvPanelN2H.Visible = mode == 0;
		cvPanelH2N.Visible = mode == 1;
		cvPanelAO.Visible = mode == 2;
	}

	private void LayoutConvertPage()
	{
		if (pageConvert == null)
		{
			return;
		}
		int width = pageConvert.Width;
		int height = pageConvert.Height;
		int col = CvColumnWidth;
		pageConvert.Controls["cvLabelMode"].Location = new Point(0, 0);
		cvModeCombo.Location = new Point(0, 20);
		int y = 20 + cvModeCombo.Height + 18;
		int panelH = Math.Max(cvPanelN2H.Height, Math.Max(cvPanelH2N.Height, cvPanelAO.Height));
		Panel[] panels = new Panel[3] { cvPanelN2H, cvPanelH2N, cvPanelAO };
		foreach (Panel p in panels)
		{
			p.Location = new Point(0, y);
		}
		y += panelH + 8;
		pageConvert.Controls["cvLabelRes"].Location = new Point(0, y);
		pageConvert.Controls["cvLabelOut"].Location = new Point(col - cvOutputCombo.Width, y);
		cvResolutionCombo.Location = new Point(0, y + 20);
		cvOutputCombo.Location = new Point(col - cvOutputCombo.Width, y + 20);
		int by = height - cvSaveBtn.Height;
		cvLoadBtn.Location = new Point(0, by);
		cvSaveBtn.Location = new Point(col - cvSaveBtn.Width, by);
		cvProgress.Location = new Point(0, by - 20);
		cvProgress.Size = new Size(col, 8);
		cvStatus.Location = new Point(0, by - 46);
		cvStatus.Size = new Size(col, 20);
		int px = col + 24;
		int gap = 14;
		int pw = Math.Max(80, (width - px - gap) / 2);
		pageConvert.Controls["cvLabelSource"].Location = new Point(px, 0);
		pageConvert.Controls["cvLabelResult"].Location = new Point(px + pw + gap, 0);
		cvSourcePreview.Location = new Point(px, 22);
		cvSourcePreview.Size = new Size(pw, Math.Max(80, height - 22));
		cvResultPreview.Location = new Point(px + pw + gap, 22);
		cvResultPreview.Size = new Size(pw, Math.Max(80, height - 22));
	}

	private void LoadConvert()
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.tga|All files|*.*";
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			LoadConvertFile(openFileDialog.FileName);
		}
	}

	private async void LoadConvertFile(string path)
	{
		try
		{
			cvLoadBtn.Enabled = false;
			cvStatus.Text = "Loading " + Path.GetFileName(path) + "…";
			FloatImage img = await Task.Run(() => FloatImage.Load(path));
			cvSource = img;
			cvPath = path;
			cvProxy = null;
			cvProxySide = -1;
			cvSaveBtn.Enabled = true;
			GuessConvertMode(path);
			UpdateConvertStatus();
			RunConvertPreview();
		}
		catch (Exception ex)
		{
			cvStatus.Text = "";
			MessageBox.Show("Couldn't load:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			cvLoadBtn.Enabled = true;
		}
	}

	// Picks a sensible conversion from the file name: "_normal" / "_nrm" maps start as Normal -> Height.
	private void GuessConvertMode(string path)
	{
		string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
		bool looksNormal = name.Contains("normal") || name.EndsWith("_n") || name.EndsWith("_nrm") || name.EndsWith("_nor");
		if (looksNormal && cvModeCombo.SelectedIndex != 0)
		{
			cvModeCombo.SelectedIndex = 0;
		}
		else if (!looksNormal && cvModeCombo.SelectedIndex == 0)
		{
			cvModeCombo.SelectedIndex = 1;
		}
	}

	private Size ConvertOutputSize()
	{
		int idx = cvResolutionCombo.SelectedIndex;
		int side = idx <= 0 ? 0 : CvResolutions[idx - 1];
		return FloatImage.FitSize(cvSource.Width, cvSource.Height, side);
	}

	private void UpdateConvertStatus()
	{
		if (cvSource == null)
		{
			cvStatus.Text = "";
			return;
		}
		Size o = ConvertOutputSize();
		string bits = cvOutputCombo.SelectedIndex == 0 ? "16-bit" : "8-bit";
		cvStatus.Text = cvSource.Width + "×" + cvSource.Height + " " + cvSource.SourceBitDepth + "-bit  →  " + o.Width + "×" + o.Height + " " + bits + " PNG";
	}

	private ConvertCore.Settings CurrentConvertSettings()
	{
		ConvertCore.Settings s = new ConvertCore.Settings();
		s.Mode = cvModeCombo.SelectedIndex switch
		{
			1 => ConvertCore.Mode.HeightToNormal,
			2 => ConvertCore.Mode.HeightToAO,
			_ => ConvertCore.Mode.NormalToHeight,
		};
		s.HeightIntensity = (float)cvIntensity.Value;
		s.Baseline = (float)cvBaseline.Value;
		s.Contrast = (float)cvContrast.Value;
		s.SurfaceBlur = (float)cvBlur.Value;
		s.NormalStrength = (float)cvStrength.Value;
		s.NormalSmoothing = (float)cvSmoothing.Value;
		s.InvertHeight = cvInvertChk.Checked;
		s.DirectX = (s.Mode == ConvertCore.Mode.NormalToHeight ? cvN2HFormatCombo : cvH2NFormatCombo).SelectedIndex == 1;
		s.AoRadius = (float)cvAoRadius.Value;
		s.AoDepth = (float)cvAoDepth.Value;
		s.AoIntensity = (float)cvAoIntensity.Value;
		s.AoContrast = (float)cvAoContrast.Value;
		s.AoDirections = cvAoQualityCombo.SelectedIndex switch
		{
			0 => 8,
			2 => 32,
			_ => 16,
		};
		return s;
	}

	private void QueueConvertPreview()
	{
		if (cvSource != null)
		{
			cvPreviewTimer.Stop();
			cvPreviewTimer.Start();
		}
	}

	// Renders the current settings on a <=512 px copy; newer requests win over older ones still running.
	private async void RunConvertPreview()
	{
		if (cvSource == null)
		{
			return;
		}
		int version = ++cvPreviewVersion;
		FloatImage source = cvSource;
		Size outSize = ConvertOutputSize();
		int proxySide = Math.Min(CvPreviewSide, Math.Max(outSize.Width, outSize.Height));
		ConvertCore.Settings settings = CurrentConvertSettings();
		try
		{
			if (cvProxy == null || cvProxySide != proxySide)
			{
				Size ps = FloatImage.FitSize(source.Width, source.Height, proxySide);
				FloatImage proxy = await Task.Run(() => source.Resize(ps.Width, ps.Height));
				if (version != cvPreviewVersion || source != cvSource)
				{
					return;
				}
				cvProxy = proxy;
				cvProxySide = proxySide;
				SetPreviewImage(cvSourcePreview, proxy.ToBitmap());
			}
			FloatImage input = cvProxy;
			double pxScale = (double)input.Width / outSize.Width;
			FloatImage result = await Task.Run(() => ConvertCore.Run(input, settings, pxScale, null));
			if (version != cvPreviewVersion || source != cvSource)
			{
				return;
			}
			SetPreviewImage(cvResultPreview, result.ToBitmap());
		}
		catch (Exception ex)
		{
			cvStatus.Text = "Preview failed: " + ex.Message;
		}
	}

	private static void SetPreviewImage(PreviewBox box, Bitmap bmp)
	{
		Image old = box.Image;
		box.Image = bmp;
		old?.Dispose();
	}

	private async Task SaveConvert()
	{
		if (cvSource == null || cvSaving)
		{
			return;
		}
		ConvertCore.Settings settings = CurrentConvertSettings();
		string suffix = settings.Mode switch
		{
			ConvertCore.Mode.HeightToNormal => "_normal",
			ConvertCore.Mode.HeightToAO => "_ao",
			_ => "_height",
		};
		string path;
		using (SaveFileDialog saveFileDialog = new SaveFileDialog())
		{
			saveFileDialog.Filter = "PNG|*.png";
			saveFileDialog.FileName = Path.GetFileNameWithoutExtension(cvPath) + suffix + ".png";
			saveFileDialog.InitialDirectory = Path.GetDirectoryName(cvPath);
			if (saveFileDialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			path = saveFileDialog.FileName;
		}
		bool sixteen = cvOutputCombo.SelectedIndex == 0;
		FloatImage source = cvSource;
		Size outSize = ConvertOutputSize();
		Progress<int> progress = new Progress<int>(delegate(int v)
		{
			cvProgress.Value = v;
		});
		cvSaving = true;
		cvSaveBtn.Enabled = false;
		cvLoadBtn.Enabled = false;
		try
		{
			await Task.Run(delegate
			{
				FloatImage input = source.Resize(outSize.Width, outSize.Height);
				FloatImage result = ConvertCore.Run(input, settings, 1.0, progress);
				PngIO.Write(path, result, sixteen);
			});
			cvStatus.Text = "Saved " + Path.GetFileName(path);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Save failed:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			cvSaving = false;
			cvSaveBtn.Enabled = true;
			cvLoadBtn.Enabled = true;
			cvProgress.Value = 0;
		}
	}
}
