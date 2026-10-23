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

	private static int CvColumnWidth => SideWidth - TitledCard.Pad * 2;

	private static readonly int[] CvResolutions = new int[4] { 512, 1024, 2048, 4096 };

	private PreviewBox cvPreview;

	private TitledCard cvSettingsCard;

	private TitledCard cvOutputCard;

	private Label cvCapMode;

	private Label cvCapRes;

	private Label cvCapOut;

	private Bitmap cvSourceBitmap;

	private Bitmap cvResultBitmap;

	private ThemedComboBox cvModeCombo;

	private Panel cvPanelN2H;

	private Panel cvPanelH2N;

	private Panel cvPanelAO;

	private ThemedSlider cvIntensity;

	private ThemedSlider cvBaseline;

	private ThemedSlider cvContrast;

	private ThemedSlider cvBlur;

	private ThemedComboBox cvN2HFormatCombo;

	private ThemedSlider cvFlatten;

	private ThemedSlider cvDetailBoost;

	private ThemedComboBox cvIntegrationCombo;

	private Label cvCapN2HFormat;

	private Label cvCapIntegration;

	private Label cvCapSourceFile;

	// Island mask / ID map for the islands mode, and the low-poly model for the mesh-aware mode.
	private FileDropBox cvMaskBox;

	private FileDropBox cvMeshBox;

	private Label cvIntegrationHint;

	private FloatImage cvIslandMask;

	private UvMesh cvMesh;

	private ThemedSlider cvStrength;

	private ThemedSlider cvSmoothing;

	private ThemedComboBox cvH2NFormatCombo;

	private ThemedCheckBox cvInvertChk;

	private ThemedSlider cvAoRadius;

	private ThemedSlider cvAoDepth;

	private ThemedSlider cvAoIntensity;

	private ThemedSlider cvAoContrast;

	private ThemedComboBox cvAoQualityCombo;

	private ThemedComboBox cvAoSamplingCombo;

	private ThemedComboBox cvResolutionCombo;

	// 8-bit / 16-bit PNG output; follows the loaded file, click to override.
	private SegmentedToggle cvBitToggle;

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
		cvPreview = new PreviewBox
		{
			EmptyText = "Drop a normal or height map",
			EmptyHint = "The live preview compares source and result; drag the divider",
			AcceptsFiles = true,
			CompareLabel = "Source",
			ImageLabel = "Result"
		};
		cvPreview.FileDropped += LoadConvertFile;
		pageConvert.Controls.Add(cvPreview);
		cvSettingsCard = Card("Conversion");
		cvOutputCard = Card(null);
		pageConvert.Controls.Add(cvSettingsCard);
		pageConvert.Controls.Add(cvOutputCard);
		cvCapMode = CvLabel("Mode", "Mode");
		cvSettingsCard.Controls.Add(cvCapMode);
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
		cvSettingsCard.Controls.Add(cvModeCombo);
		cvIntensity = CvSlider("Height intensity", 0.0, 4.0, 1.0, 2);
		cvBaseline = CvSlider("Mid-point baseline", 0.0, 1.0, 0.5, 2);
		cvContrast = CvSlider("Contrast", -1.0, 1.0, 0.0, 2);
		cvBlur = CvSlider("Surface blur", 0.0, 20.0, 0.0, 1, " px");
		cvFlatten = CvSlider("Flatten broad", 0.0, 100.0, 0.0, 0, "%");
		cvDetailBoost = CvSlider("Detail boost", 1.0, 4.0, 1.0, 2, "×");
		cvN2HFormatCombo = CvYCombo();
		cvCapN2HFormat = CvLabel("N2HFmt", "Normal map format");
		cvCapIntegration = CvLabel("Integration", "Integration");
		cvIntegrationCombo = new ThemedComboBox();
		cvIntegrationCombo.Items.AddRange(new object[3] { "Tileable", "UV islands (mask)", "Mesh-aware (3D)" });
		cvIntegrationCombo.SelectedIndex = 0;
		cvIntegrationCombo.SelectedIndexChanged += delegate
		{
			LayoutN2HPanel();
			QueueConvertPreview();
		};
		cvCapSourceFile = CvLabel("SourceFile", "");
		cvMaskBox = new FileDropBox
		{
			Prompt = "Drop a UV snapshot or ID map",
			Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.tga|All files|*.*"
		};
		cvMaskBox.FileChanged += delegate
		{
			LoadIslandMask(cvMaskBox.FilePath);
		};
		cvMeshBox = new FileDropBox
		{
			Prompt = "Drop the low-poly model (.obj)",
			Filter = "Wavefront OBJ|*.obj|All files|*.*"
		};
		cvMeshBox.FileChanged += delegate
		{
			LoadMesh(cvMeshBox.FilePath);
		};
		cvIntegrationHint = new Label
		{
			Name = "capCvHint",
			AutoSize = false,
			Font = Ui.Small
		};
		cvPanelN2H = new Panel
		{
			BackColor = Color.Transparent
		};
		cvPanelN2H.Controls.AddRange(new Control[14] { cvIntensity, cvBaseline, cvContrast, cvBlur, cvFlatten, cvDetailBoost, cvCapN2HFormat, cvN2HFormatCombo, cvCapIntegration, cvIntegrationCombo, cvCapSourceFile, cvMaskBox, cvMeshBox, cvIntegrationHint });
		LayoutN2HPanel();
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
		cvPanelH2N = CvModePanel(cvStrength, cvSmoothing, CvLabel("H2NFmt", "Normal map format"), cvH2NFormatCombo, cvInvertChk);
		cvAoRadius = CvSlider("Radius", 1.0, 128.0, 32.0, 0, " px");
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
		cvAoSamplingCombo = new ThemedComboBox
		{
			Width = CvColumnWidth
		};
		cvAoSamplingCombo.Items.AddRange(new object[2] { "Detailed (dense near each pixel)", "Classic (even spacing)" });
		cvAoSamplingCombo.SelectedIndex = 0;
		cvAoSamplingCombo.SelectedIndexChanged += delegate
		{
			QueueConvertPreview();
		};
		cvPanelAO = CvModePanel(cvAoRadius, cvAoDepth, cvAoIntensity, cvAoContrast, CvLabel("AoS", "Sampling"), cvAoSamplingCombo, CvLabel("AoQ", "Quality"), cvAoQualityCombo);
		cvSettingsCard.Controls.Add(cvPanelN2H);
		cvSettingsCard.Controls.Add(cvPanelH2N);
		cvSettingsCard.Controls.Add(cvPanelAO);
		cvCapRes = CvLabel("Res", "Output resolution");
		cvOutputCard.Controls.Add(cvCapRes);
		cvResolutionCombo = new ThemedComboBox
		{
			Width = Ui.S(132)
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
		cvOutputCard.Controls.Add(cvResolutionCombo);
		cvCapOut = CvLabel("Out", "PNG bit depth");
		cvOutputCard.Controls.Add(cvCapOut);
		cvBitToggle = new SegmentedToggle
		{
			Options = new string[2] { "8-bit", "16-bit" },
			Width = Ui.S(122)
		};
		cvBitToggle.SelectedIndex = 1;
		cvBitToggle.SelectedIndexChanged += delegate
		{
			UpdateConvertStatus();
		};
		cvOutputCard.Controls.Add(cvBitToggle);
		cvStatus = new Label
		{
			AutoSize = false,
			Text = "No map loaded",
			Font = Ui.Small,
			AutoEllipsis = true
		};
		cvOutputCard.Controls.Add(cvStatus);
		cvProgress = new ThemedProgress();
		cvOutputCard.Controls.Add(cvProgress);
		cvLoadBtn = new FlatButton
		{
			Text = "Load map",
			Role = FlatButton.ButtonRole.Secondary,
			Width = Ui.S(130)
		};
		cvLoadBtn.Click += delegate
		{
			LoadConvert();
		};
		cvSaveBtn = new FlatButton
		{
			Text = "Save result",
			Role = FlatButton.ButtonRole.Accent,
			Width = Ui.S(150),
			Enabled = false
		};
		cvSaveBtn.Click += async delegate
		{
			await SaveConvert();
		};
		cvOutputCard.Controls.Add(cvLoadBtn);
		cvOutputCard.Controls.Add(cvSaveBtn);
		pageConvert.Resize += delegate
		{
			LayoutConvertPage();
		};
		ShowConvertMode();
	}

	// Muted caption (the "cap" name prefix is what StyleLabels keys on).
	private static Label CvLabel(string name, string text)
	{
		return new Label
		{
			Name = "capCv" + name,
			AutoSize = true,
			Text = text,
			Font = Ui.Small
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
	// The panel is transparent so it shows the card's surface behind it.
	private static Panel CvModePanel(params Control[] controls)
	{
		Panel panel = new Panel
		{
			BackColor = Color.Transparent
		};
		int y = 0;
		foreach (Control c in controls)
		{
			c.Location = new Point(0, y);
			panel.Controls.Add(c);
			y += (c is Label) ? Ui.S(20) : (c.Height + Ui.S(10));
		}
		panel.Size = new Size(CvColumnWidth, y);
		return panel;
	}

	// Normal -> Height settings: tone sliders, then format and integration side by side, then the
	// mask or model box when that integration needs one, and a one-line explanation.
	private void LayoutN2HPanel()
	{
		int width = cvPanelN2H.Width;
		int y = 0;
		foreach (ThemedSlider slider in new ThemedSlider[4] { cvIntensity, cvBaseline, cvContrast, cvBlur })
		{
			slider.Location = new Point(0, y);
			slider.Width = width;
			y += slider.Height + Ui.S(4);
		}
		// Broad shape and detail amounts share a row.
		int half = (width - Ui.S(16)) / 2;
		cvFlatten.Location = new Point(0, y);
		cvFlatten.Width = half;
		cvDetailBoost.Location = new Point(half + Ui.S(16), y);
		cvDetailBoost.Width = width - half - Ui.S(16);
		y += cvFlatten.Height + Ui.S(8);
		half = (width - Ui.S(8)) / 2;
		cvCapN2HFormat.Location = new Point(0, y);
		cvCapIntegration.Location = new Point(half + Ui.S(8), y);
		cvN2HFormatCombo.Location = new Point(0, y + Ui.S(20));
		cvN2HFormatCombo.Width = half;
		cvIntegrationCombo.Location = new Point(half + Ui.S(8), y + Ui.S(20));
		cvIntegrationCombo.Width = width - half - Ui.S(8);
		y = cvIntegrationCombo.Bottom + Ui.S(12);
		int mode = cvIntegrationCombo.SelectedIndex;
		cvMaskBox.Visible = mode == 1;
		cvMeshBox.Visible = mode == 2;
		cvCapSourceFile.Visible = mode != 0;
		if (mode != 0)
		{
			cvCapSourceFile.Text = mode == 1 ? "Island mask" : "3D model (advanced)";
			cvCapSourceFile.Location = new Point(0, y);
			FileDropBox box = mode == 1 ? cvMaskBox : cvMeshBox;
			box.Location = new Point(0, y + Ui.S(20));
			box.Width = width;
			y = box.Bottom + Ui.S(8);
		}
		cvIntegrationHint.Text = IntegrationHint(mode);
		cvIntegrationHint.Location = new Point(0, y);
		cvIntegrationHint.Size = new Size(width, Ui.S(34));
		cvPanelN2H.Height = cvIntegrationHint.Bottom;
	}

	private string IntegrationHint(int mode)
	{
		switch (mode)
		{
		case 1:
			return cvIslandMask == null ? "Add a UV snapshot (white on black) or an ID map. Until then the whole image is used." : "Each island keeps its own broad shape. Islands are levelled separately, so seams can differ.";
		case 2:
			return cvMesh == null ? "Add the low-poly OBJ with non-overlapping UVs. Until then the whole image is used." : "UV seams are stitched using the model, so heights meet across islands.";
		default:
			return "Treats the image as one continuous surface. Best for tiling textures.";
		}
	}

	private async void LoadIslandMask(string path)
	{
		cvIslandMask = null;
		if (!string.IsNullOrEmpty(path))
		{
			try
			{
				FloatImage mask = await Task.Run(() => FloatImage.Load(path));
				cvIslandMask = mask;
				cvMaskBox.Info = mask.Width + "×" + mask.Height;
			}
			catch (Exception ex)
			{
				Notifier.Error("Couldn't load the mask: " + ex.Message);
				cvMaskBox.FilePath = null;
				return;
			}
		}
		LayoutN2HPanel();
		QueueConvertPreview();
	}

	private async void LoadMesh(string path)
	{
		cvMesh = null;
		if (!string.IsNullOrEmpty(path))
		{
			try
			{
				UvMesh mesh = await Task.Run(() => UvMesh.LoadObj(path));
				int overlap = await Task.Run(() => mesh.Rasterize(512, 512).OverlapTexels);
				cvMesh = mesh;
				cvMeshBox.Info = mesh.TriangleCount.ToString("N0") + " tris • " + mesh.IslandCount + " islands";
				if (overlap > 512 * 512 / 200)
				{
					Notifier.Warning(mesh.Name + " has overlapping UVs. Mesh-aware mode needs each part of the model on its own UV space; overlapped areas may come out wrong.");
				}
				else
				{
					Notifier.Success("Loaded " + mesh.Name + ": " + mesh.IslandCount + " UV islands, " + mesh.Seams.Count + " seam edges.");
				}
			}
			catch (Exception ex)
			{
				Notifier.Error("Couldn't load the model: " + ex.Message);
				cvMeshBox.FilePath = null;
				return;
			}
		}
		LayoutN2HPanel();
		QueueConvertPreview();
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
		int side = SideWidth;
		int inner = side - Pad * 2;
		cvSettingsCard.Location = new Point(0, 0);
		cvSettingsCard.Size = new Size(side, height);
		int y = Stack(cvCapMode, cvModeCombo, Pad, cvSettingsCard.ContentTop, inner);
		Panel[] panels = new Panel[3] { cvPanelN2H, cvPanelH2N, cvPanelAO };
		cvPanelN2H.Location = new Point(Pad, y + Ui.S(4));
		cvPanelN2H.Width = inner;
		LayoutN2HPanel();
		foreach (Panel p in new Panel[2] { cvPanelH2N, cvPanelAO })
		{
			p.Location = new Point(Pad, y + Ui.S(4));
			p.Width = inner;
			foreach (Control c in p.Controls)
			{
				c.Width = inner;
			}
		}
		int px = side + Gap;
		int pw = width - px;
		int outH = Ui.S(84);
		cvOutputCard.Location = new Point(px, height - outH);
		cvOutputCard.Size = new Size(pw, outH);
		cvPreview.Location = new Point(px, 0);
		cvPreview.Size = new Size(pw, height - outH - Gap);
		int x = Pad;
		int cy = Ui.S(14);
		cvCapRes.Location = new Point(x, cy);
		cvResolutionCombo.Location = new Point(x, cy + Ui.S(20));
		x += cvResolutionCombo.Width + Ui.S(12);
		cvCapOut.Location = new Point(x, cy);
		cvBitToggle.Location = new Point(x, cy + Ui.S(20));
		x += cvBitToggle.Width + Ui.S(16);
		int bx = pw - Pad - cvSaveBtn.Width;
		int by = (outH - cvSaveBtn.Height) / 2;
		cvSaveBtn.Location = new Point(bx, by);
		cvLoadBtn.Location = new Point(bx - Ui.S(8) - cvLoadBtn.Width, by);
		int sw = Math.Max(Ui.S(60), cvLoadBtn.Left - Ui.S(16) - x);
		cvStatus.Location = new Point(x, cy + Ui.S(16));
		cvStatus.Size = new Size(sw, Ui.S(20));
		cvProgress.Location = new Point(x, cy + Ui.S(42));
		cvProgress.Width = sw;
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
			// Output bit depth starts at the source's; the toggle can still override it.
			cvBitToggle.SelectedIndex = img.SourceBitDepth >= 16 ? 1 : 0;
			cvProxy = null;
			cvProxySide = -1;
			cvSaveBtn.Enabled = true;
			GuessConvertMode(path);
			UpdateConvertStatus();
			RunConvertPreview();
		}
		catch (Exception ex)
		{
			UpdateConvertStatus();
			Notifier.Error("Couldn't load: " + ex.Message);
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

	private bool Convert16Bit => cvBitToggle.SelectedIndex == 1;

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
			cvStatus.Text = "No map loaded";
			return;
		}
		Size o = ConvertOutputSize();
		string bits = Convert16Bit ? "16-bit" : "8-bit";
		cvStatus.Text = "Saves " + o.Width + "×" + o.Height + " • " + bits;
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
		s.FlattenBroad = (float)(cvFlatten.Value / 100.0);
		s.DetailBoost = (float)cvDetailBoost.Value;
		s.Integration = cvIntegrationCombo.SelectedIndex switch
		{
			1 => ConvertCore.Integration.Islands,
			2 => ConvertCore.Integration.Mesh,
			_ => ConvertCore.Integration.Tileable,
		};
		s.IslandMask = cvIslandMask;
		s.Mesh = cvMesh;
		s.NormalStrength = (float)cvStrength.Value;
		s.NormalSmoothing = (float)cvSmoothing.Value;
		s.InvertHeight = cvInvertChk.Checked;
		s.DirectX = (s.Mode == ConvertCore.Mode.NormalToHeight ? cvN2HFormatCombo : cvH2NFormatCombo).SelectedIndex == 1;
		s.AoRadius = (float)cvAoRadius.Value;
		s.AoDepth = (float)cvAoDepth.Value;
		s.AoIntensity = (float)cvAoIntensity.Value;
		s.AoContrast = (float)cvAoContrast.Value;
		s.AoSampling = cvAoSamplingCombo.SelectedIndex == 1 ? ConvertCore.AoSampling.Classic : ConvertCore.AoSampling.Detailed;
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
				Bitmap old = cvSourceBitmap;
				cvSourceBitmap = proxy.ToBitmap();
				cvPreview.CompareImage = cvSourceBitmap;
				if (cvResultBitmap == null)
				{
					cvPreview.Image = cvSourceBitmap;
				}
				old?.Dispose();
				cvPreview.InfoText = source.Width + "×" + source.Height + " • " + source.SourceBitDepth + "-bit source";
				cvPreview.PixelRatio = (float)source.Width / proxy.Width;
			}
			FloatImage input = cvProxy;
			double pxScale = (double)input.Width / outSize.Width;
			FloatImage result = await Task.Run(() => ConvertCore.Run(input, settings, pxScale, null));
			if (version != cvPreviewVersion || source != cvSource)
			{
				return;
			}
			Bitmap oldResult = cvResultBitmap;
			cvResultBitmap = result.ToBitmap();
			cvPreview.Image = cvResultBitmap;
			oldResult?.Dispose();
		}
		catch (Exception ex)
		{
			Notifier.Error("Preview failed: " + ex.Message);
		}
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
		bool sixteen = Convert16Bit;
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
			Notifier.Success("Saved " + Path.GetFileName(path) + " (" + outSize.Width + "×" + outSize.Height + ", " + (sixteen ? "16" : "8") + "-bit)");
		}
		catch (Exception ex)
		{
			Notifier.Error("Save failed: " + ex.Message);
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
