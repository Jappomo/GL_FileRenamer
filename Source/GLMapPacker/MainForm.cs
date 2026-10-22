using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GLMapPacker;

public class MainForm : Form
{
	private Panel header;

	private ThemeToggle toggle;

	private Label toggleHint;

	private TabStrip tabs;

	private Panel pageHost;

	private Panel pagePack;

	private Panel pageUnpack;

	private Panel pageUtility;

	private ThemedComboBox formatCombo;

	private ChannelSlot slotR;

	private ChannelSlot slotG;

	private ChannelSlot slotB;

	private ChannelSlot slotA;

	private PreviewBox packResult;

	private FlatButton packBtn;

	private FlatButton packSaveBtn;

	private ThemedComboBox presetCombo;

	private Bitmap packedImage;

	private ThemedProgress packProgress;

	private PreviewBox unpackSource;

	private FlatButton unpackLoadBtn;

	private FlatButton unpackSaveAllBtn;

	private ThemedCheckBox sameFolderChk;

	private ChannelView viewR;

	private ChannelView viewG;

	private ChannelView viewB;

	private ChannelView viewA;

	private string unpackPath;

	private ThemedProgress unpackProgress;

	private PreviewBox utilPreview;

	private FlatButton utilLoadBtn;

	private FlatButton utilApplyBtn;

	private FlatButton utilSaveBtn;

	private ThemedComboBox utilOpCombo;

	private ThemedComboBox utilFormatCombo;

	private Bitmap utilImage;

	private string utilPath;

	private Panel pageRenamer;

	private LabeledField dirField;

	private LabeledField searchField;

	private LabeledField replaceField;

	private LabeledField prefixField;

	private LabeledField suffixField;

	private LabeledField digitsField;

	private FlatButton browseBtn;

	private FlatButton renameBtn;

	private ThemedCheckBox recurseChk;

	private ThemedCheckBox keepExtChk;

	private RenamePreviewList renamePreview;

	private Label renameStatus;

	private Panel pageInjector;

	private Dictionary<MapType, MapSlotRow> slotRows;

	private ThemedComboBox shaderCombo;

	private LabeledField materialNameField;

	private FlatButton injectBtn;

	private FlatButton injectorClearBtn;

	private Label injectorStatus;

	private ThemedCheckBox showOptionalChk;

	private List<RenameEngine.Item> currentPlan;

	public MainForm()
	{
		Text = "GL Map Packer";
		MinimumSize = new Size(1000, 720);
		base.Size = new Size(1120, 800);
		base.StartPosition = FormStartPosition.CenterScreen;
		Font = new Font("Segoe UI", 9.5f);
		DoubleBuffered = true;
		BuildHeader();
		BuildPages();
		base.Controls.Add(pageHost);
		base.Controls.Add(header);
		Theme.ThemeChanged += delegate
		{
			ApplyTheme();
		};
		ApplyTheme();
		ShowPage(0);
	}

	private void BuildHeader()
	{
		header = new Panel
		{
			Dock = DockStyle.Top,
			Height = 62
		};
		toggleHint = new Label
		{
			AutoSize = true,
			Text = "Theme",
			Font = new Font("Segoe UI", 9f)
		};
		toggle = new ThemeToggle();
		toggle.Toggled += delegate
		{
			ApplyTheme();
		};
		tabs = new TabStrip
		{
			Location = new Point(24, 14),
			Size = new Size(680, 40),
			PillWidth = 100
		};
		tabs.Tabs = new string[5] { "Pack", "Unpack", "Utility", "Renamer", "Injector" };
		tabs.SelectedIndexChanged += delegate
		{
			ShowPage(tabs.SelectedIndex);
		};
		header.Controls.Add(toggleHint);
		header.Controls.Add(toggle);
		header.Controls.Add(tabs);
		header.Resize += delegate
		{
			LayoutHeader();
		};
	}

	private void LayoutHeader()
	{
		int num = header.Width - 28;
		toggle.Location = new Point(num - toggle.Width, (header.Height - toggle.Height) / 2);
		toggleHint.Location = new Point(toggle.Left - toggleHint.Width - 10, toggle.Top + (toggle.Height - toggleHint.Height) / 2);
	}

	private void BuildPages()
	{
		pageHost = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(24, 12, 24, 20)
		};
		BuildPackPage();
		BuildUnpackPage();
		BuildUtilityPage();
		BuildRenamerPage();
		BuildInjectorPage();
		pageHost.Controls.Add(pagePack);
		pageHost.Controls.Add(pageUnpack);
		pageHost.Controls.Add(pageUtility);
		pageHost.Controls.Add(pageRenamer);
		pageHost.Controls.Add(pageInjector);
	}

	private void ShowPage(int index)
	{
		pagePack.Visible = index == 0;
		pageUnpack.Visible = index == 1;
		pageUtility.Visible = index == 2;
		pageRenamer.Visible = index == 3;
		pageInjector.Visible = index == 4;
		if (tabs.SelectedIndex != index)
		{
			tabs.SelectedIndex = index;
		}
	}

	private void BuildPackPage()
	{
		pagePack = new Panel
		{
			Dock = DockStyle.Fill
		};
		slotR = new ChannelSlot("Channel R", Color.FromArgb(226, 75, 74));
		slotG = new ChannelSlot("Channel G", Color.FromArgb(120, 190, 70));
		slotB = new ChannelSlot("Channel B", Color.FromArgb(55, 138, 221));
		slotA = new ChannelSlot("Channel A", Color.FromArgb(150, 160, 170));
		ChannelSlot[] array = new ChannelSlot[4] { slotR, slotG, slotB, slotA };
		foreach (ChannelSlot channelSlot in array)
		{
			channelSlot.Changed += delegate
			{
				packedImage = null;
				packResult.Image = null;
				packSaveBtn.Enabled = false;
			};
			pagePack.Controls.Add(channelSlot);
		}
		packResult = new PreviewBox
		{
			EmptyText = "packed result appears here"
		};
		pagePack.Controls.Add(packResult);
		Label label = new Label();
		label.AutoSize = true;
		label.Text = "Preset";
		label.Font = new Font("Segoe UI", 9f);
		Label label2 = label;
		label2.Name = "presetLabel";
		presetCombo = new ThemedComboBox
		{
			Width = 260
		};
		presetCombo.Items.AddRange(new object[4] { "Custom", "Unreal ORM (R=AO, G=Rough, B=Metal)", "Unity HDRP mask (R=Metal, G=AO, B=Detail, A=Smooth)", "glTF metallic-roughness (G=Rough, B=Metal)" });
		presetCombo.SelectedIndex = 0;
		label = new Label();
		label.AutoSize = true;
		label.Text = "Format";
		label.Font = new Font("Segoe UI", 9f);
		Label label3 = label;
		label3.Name = "fmtLabel";
		formatCombo = new ThemedComboBox
		{
			Width = 150
		};
		formatCombo.Items.AddRange(new object[5] { "PNG", "TGA", "TIFF", "BMP", "JPEG" });
		formatCombo.SelectedIndex = 0;
		packProgress = new ThemedProgress();
		packBtn = new FlatButton
		{
			Text = "Pack now",
			Role = FlatButton.ButtonRole.Secondary,
			Width = 160
		};
		packBtn.Click += async delegate
		{
			await DoPack();
		};
		packSaveBtn = new FlatButton
		{
			Text = "Save packed map",
			Role = FlatButton.ButtonRole.Accent,
			Width = 200,
			Enabled = false
		};
		packSaveBtn.Click += delegate
		{
			SavePacked();
		};
		pagePack.Controls.Add(label2);
		pagePack.Controls.Add(presetCombo);
		pagePack.Controls.Add(label3);
		pagePack.Controls.Add(formatCombo);
		pagePack.Controls.Add(packProgress);
		pagePack.Controls.Add(packBtn);
		pagePack.Controls.Add(packSaveBtn);
		pagePack.Resize += delegate
		{
			LayoutPackPage();
		};
	}

	private void LayoutPackPage()
	{
		int width = pagePack.Width;
		int num = 14;
		int num2 = (width - num * 3) / 4;
		int num3 = 200;
		int num4 = 0;
		int num5 = 0;
		ChannelSlot[] array = new ChannelSlot[4] { slotR, slotG, slotB, slotA };
		for (int i = 0; i < 4; i++)
		{
			array[i].Location = new Point(num4, num5);
			array[i].Size = new Size(num2, num3);
			num4 += num2 + num;
		}
		int num6 = num5 + num3 + num;
		int num7 = 150;
		packResult.Location = new Point(0, num6);
		packResult.Size = new Size(width, pagePack.Height - num6 - num7);
		int num8 = pagePack.Height - num7 + 8;
		Control control = pagePack.Controls["presetLabel"];
		Control control2 = pagePack.Controls["fmtLabel"];
		if (control != null)
		{
			control.Location = new Point(2, num8);
		}
		presetCombo.Location = new Point(2, num8 + 20);
		if (control2 != null)
		{
			control2.Location = new Point(2 + presetCombo.Width + 24, num8);
		}
		formatCombo.Location = new Point(2 + presetCombo.Width + 24, num8 + 20);
		packProgress.Location = new Point(2, num8 + 62);
		packProgress.Size = new Size(width - 4, 8);
		packSaveBtn.Location = new Point(width - packSaveBtn.Width, num8 + 78);
		packBtn.Location = new Point(width - packSaveBtn.Width - packBtn.Width - 12, num8 + 78);
	}

	private async Task DoPack()
	{
		if (slotR.SourceImage == null && slotG.SourceImage == null && slotB.SourceImage == null && slotA.SourceImage == null)
		{
			MessageBox.Show("Load at least one channel image first.", "Nothing to pack", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		packBtn.Enabled = false;
		Progress<int> progress = new Progress<int>(delegate(int v)
		{
			packProgress.Value = v;
		});
		try
		{
			Bitmap result = await Task.Run(() => ImageCore.Pack(slotR.SourceImage, slotR.Pick, slotG.SourceImage, slotG.Pick, slotB.SourceImage, slotB.Pick, slotA.SourceImage, slotA.Pick, progress));
			packedImage = result;
			packResult.Image = result;
			packSaveBtn.Enabled = result != null;
		}
		catch (Exception ex)
		{
			MessageBox.Show("Packing failed:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			packBtn.Enabled = true;
			packProgress.Value = 0;
		}
	}

	private void SavePacked()
	{
		if (packedImage == null)
		{
			return;
		}
		ImageCore.OutputFormat outputFormat = SelectedFormat(formatCombo);
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		try
		{
			saveFileDialog.Filter = FilterFor(outputFormat);
			saveFileDialog.FileName = "packed" + ImageCore.ExtensionFor(outputFormat);
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				try
				{
					ImageCore.Save(packedImage, saveFileDialog.FileName, ImageCore.FormatFromExtension(saveFileDialog.FileName, outputFormat));
					return;
				}
				catch (Exception ex)
				{
					MessageBox.Show("Save failed:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void BuildUnpackPage()
	{
		pageUnpack = new Panel
		{
			Dock = DockStyle.Fill
		};
		unpackSource = new PreviewBox
		{
			EmptyText = "load a packed texture to split"
		};
		unpackLoadBtn = new FlatButton
		{
			Text = "Load texture",
			Role = FlatButton.ButtonRole.Accent,
			Width = 160
		};
		unpackLoadBtn.Click += delegate
		{
			LoadUnpack();
		};
		sameFolderChk = new ThemedCheckBox
		{
			Text = "Save channels in the source folder (no dialog)",
			Width = 380
		};
		unpackSaveAllBtn = new FlatButton
		{
			Text = "Save all channels",
			Role = FlatButton.ButtonRole.Accent,
			Width = 190
		};
		unpackSaveAllBtn.Click += delegate
		{
			SaveAllChannels();
		};
		viewR = new ChannelView("Channel R", Color.FromArgb(226, 75, 74), "_R");
		viewG = new ChannelView("Channel G", Color.FromArgb(120, 190, 70), "_G");
		viewB = new ChannelView("Channel B", Color.FromArgb(55, 138, 221), "_B");
		viewA = new ChannelView("Channel A", Color.FromArgb(150, 160, 170), "_A");
		unpackProgress = new ThemedProgress();
		pageUnpack.Controls.Add(unpackSource);
		pageUnpack.Controls.Add(unpackLoadBtn);
		pageUnpack.Controls.Add(sameFolderChk);
		pageUnpack.Controls.Add(unpackSaveAllBtn);
		pageUnpack.Controls.Add(unpackProgress);
		ChannelView[] array = new ChannelView[4] { viewR, viewG, viewB, viewA };
		foreach (ChannelView value in array)
		{
			pageUnpack.Controls.Add(value);
		}
		pageUnpack.AllowDrop = true;
		pageUnpack.DragEnter += delegate(object s, DragEventArgs e)
		{
			e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None);
		};
		pageUnpack.DragDrop += delegate(object s, DragEventArgs e)
		{
			string[] array2 = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (array2 != null && array2.Length > 0)
			{
				LoadUnpackFile(array2[0]);
			}
		};
		pageUnpack.Resize += delegate
		{
			LayoutUnpackPage();
		};
	}

	private void LayoutUnpackPage()
	{
		int width = pageUnpack.Width;
		int height = pageUnpack.Height;
		int num = 210;
		int num2 = 260;
		unpackSource.Location = new Point(0, 0);
		unpackSource.Size = new Size(num2, num);
		unpackLoadBtn.Location = new Point(num2 + 20, 8);
		unpackProgress.Location = new Point(num2 + 20, 8 + unpackLoadBtn.Height + 14);
		unpackProgress.Size = new Size(Math.Max(160, width - num2 - 20), 8);
		int num3 = 52;
		int num4 = height - num3;
		sameFolderChk.Location = new Point(2, num4 + (num3 - sameFolderChk.Height) / 2);
		unpackSaveAllBtn.Location = new Point(width - unpackSaveAllBtn.Width, num4 + (num3 - unpackSaveAllBtn.Height) / 2);
		int num5 = 14;
		int num6 = (width - num5 * 3) / 4;
		int num7 = num + 20;
		int val = num4 - num7 - 12;
		int height2 = Math.Min(val, 300);
		ChannelView[] array = new ChannelView[4] { viewR, viewG, viewB, viewA };
		int num8 = 0;
		for (int i = 0; i < 4; i++)
		{
			array[i].Location = new Point(num8, num7);
			array[i].Size = new Size(num6, height2);
			num8 += num6 + num5;
		}
	}

	private void LoadUnpack()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		try
		{
			openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.tga|All files|*.*";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				LoadUnpackFile(openFileDialog.FileName);
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private async void LoadUnpackFile(string path)
	{
		try
		{
			Bitmap src = ImageCore.Load(path);
			unpackPath = path;
			unpackSource.Image = src;
			Progress<int> progress = new Progress<int>(delegate(int v)
			{
				unpackProgress.Value = v;
			});
			Bitmap[] outs = await Task.Run(delegate
			{
				ImageCore.Split(src, forceOpaqueAlpha: true, out var outR, out var outG, out var outB, out var outA, progress);
				return new Bitmap[4] { outR, outG, outB, outA };
			});
			viewR.SetImage(outs[0], path);
			viewG.SetImage(outs[1], path);
			viewB.SetImage(outs[2], path);
			viewA.SetImage(outs[3], path);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Couldn't load/split:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			unpackProgress.Value = 0;
		}
	}

	private void SaveAllChannels()
	{
		bool autoSameFolder = sameFolderChk.Checked;
		ChannelView[] array = new ChannelView[4] { viewR, viewG, viewB, viewA };
		foreach (ChannelView channelView in array)
		{
			channelView.Save(autoSameFolder);
		}
	}

	private void BuildUtilityPage()
	{
		pageUtility = new Panel
		{
			Dock = DockStyle.Fill
		};
		utilPreview = new PreviewBox
		{
			EmptyText = "load a texture to process"
		};
		Label label = new Label();
		label.AutoSize = true;
		label.Text = "Operation";
		label.Font = new Font("Segoe UI", 9f);
		label.Name = "opLabel";
		Label value = label;
		utilOpCombo = new ThemedComboBox
		{
			Width = 320
		};
		utilOpCombo.Items.AddRange(new object[4] { "Flip Y (normal map: OpenGL <-> DirectX)", "Flip vertical (whole image)", "Flip horizontal (whole image)", "Invert RGB" });
		utilOpCombo.SelectedIndex = 0;
		label = new Label();
		label.AutoSize = true;
		label.Text = "Format";
		label.Font = new Font("Segoe UI", 9f);
		label.Name = "ufLabel";
		Label value2 = label;
		utilFormatCombo = new ThemedComboBox
		{
			Width = 150
		};
		utilFormatCombo.Items.AddRange(new object[5] { "PNG", "TGA", "TIFF", "BMP", "JPEG" });
		utilFormatCombo.SelectedIndex = 0;
		utilLoadBtn = new FlatButton
		{
			Text = "Load texture",
			Role = FlatButton.ButtonRole.Secondary,
			Width = 160
		};
		utilLoadBtn.Click += delegate
		{
			LoadUtil();
		};
		utilApplyBtn = new FlatButton
		{
			Text = "Apply",
			Role = FlatButton.ButtonRole.Secondary,
			Width = 130,
			Enabled = false
		};
		utilApplyBtn.Click += delegate
		{
			ApplyUtil();
		};
		utilSaveBtn = new FlatButton
		{
			Text = "Save result",
			Role = FlatButton.ButtonRole.Accent,
			Width = 180,
			Enabled = false
		};
		utilSaveBtn.Click += delegate
		{
			SaveUtil();
		};
		pageUtility.Controls.Add(utilPreview);
		pageUtility.Controls.Add(value);
		pageUtility.Controls.Add(utilOpCombo);
		pageUtility.Controls.Add(value2);
		pageUtility.Controls.Add(utilFormatCombo);
		pageUtility.Controls.Add(utilLoadBtn);
		pageUtility.Controls.Add(utilApplyBtn);
		pageUtility.Controls.Add(utilSaveBtn);
		pageUtility.AllowDrop = true;
		pageUtility.DragEnter += delegate(object s, DragEventArgs e)
		{
			e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None);
		};
		pageUtility.DragDrop += delegate(object s, DragEventArgs e)
		{
			string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (array != null && array.Length > 0)
			{
				LoadUtilFile(array[0]);
			}
		};
		pageUtility.Resize += delegate
		{
			LayoutUtilityPage();
		};
	}

	private void LayoutUtilityPage()
	{
		int width = pageUtility.Width;
		int num = 150;
		utilPreview.Location = new Point(0, 0);
		utilPreview.Size = new Size(width, pageUtility.Height - num);
		int num2 = pageUtility.Height - num + 12;
		Control control = pageUtility.Controls["opLabel"];
		Control control2 = pageUtility.Controls["ufLabel"];
		if (control != null)
		{
			control.Location = new Point(2, num2);
		}
		utilOpCombo.Location = new Point(2, num2 + 20);
		if (control2 != null)
		{
			control2.Location = new Point(2 + utilOpCombo.Width + 24, num2);
		}
		utilFormatCombo.Location = new Point(2 + utilOpCombo.Width + 24, num2 + 20);
		utilSaveBtn.Location = new Point(width - utilSaveBtn.Width, num2 + 60);
		utilApplyBtn.Location = new Point(width - utilSaveBtn.Width - utilApplyBtn.Width - 12, num2 + 60);
		utilLoadBtn.Location = new Point(width - utilSaveBtn.Width - utilApplyBtn.Width - utilLoadBtn.Width - 24, num2 + 60);
	}

	private void LoadUtil()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		try
		{
			openFileDialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.tga|All files|*.*";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				LoadUtilFile(openFileDialog.FileName);
			}
		}
		finally
		{
			((IDisposable)(object)openFileDialog)?.Dispose();
		}
	}

	private void LoadUtilFile(string path)
	{
		try
		{
			if (utilImage != null)
			{
				utilImage.Dispose();
			}
			utilImage = ImageCore.Load(path);
			utilPath = path;
			utilPreview.Image = utilImage;
			utilApplyBtn.Enabled = true;
			utilSaveBtn.Enabled = true;
		}
		catch (Exception ex)
		{
			MessageBox.Show("Couldn't load:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void ApplyUtil()
	{
		if (utilImage != null)
		{
			Bitmap bitmap = utilOpCombo.SelectedIndex switch
			{
				1 => ImageCore.FlipVertical(utilImage), 
				2 => ImageCore.FlipHorizontal(utilImage), 
				3 => ImageCore.InvertRgb(utilImage), 
				_ => ImageCore.FlipGreen(utilImage), 
			};
			utilImage.Dispose();
			utilImage = bitmap;
			utilPreview.Image = utilImage;
		}
	}

	private void SaveUtil()
	{
		if (utilImage == null)
		{
			return;
		}
		ImageCore.OutputFormat outputFormat = SelectedFormat(utilFormatCombo);
		SaveFileDialog saveFileDialog = new SaveFileDialog();
		try
		{
			saveFileDialog.Filter = FilterFor(outputFormat);
			string text = ((utilPath == null) ? "texture" : Path.GetFileNameWithoutExtension(utilPath));
			saveFileDialog.FileName = text + "_util" + ImageCore.ExtensionFor(outputFormat);
			if (utilPath != null)
			{
				saveFileDialog.InitialDirectory = Path.GetDirectoryName(utilPath);
			}
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				try
				{
					ImageCore.Save(utilImage, saveFileDialog.FileName, ImageCore.FormatFromExtension(saveFileDialog.FileName, outputFormat));
					return;
				}
				catch (Exception ex)
				{
					MessageBox.Show("Save failed:" + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
		}
		finally
		{
			((IDisposable)(object)saveFileDialog)?.Dispose();
		}
	}

	private void BuildRenamerPage()
	{
		pageRenamer = new Panel
		{
			Dock = DockStyle.Fill
		};
		dirField = new LabeledField("Folder");
		dirField.Box.ReadOnly = true;
		browseBtn = new FlatButton
		{
			Text = "Browse",
			Role = FlatButton.ButtonRole.Secondary,
			Width = 120
		};
		browseBtn.Click += delegate
		{
			BrowseFolder();
		};
		searchField = new LabeledField("Search");
		replaceField = new LabeledField("Replace with");
		prefixField = new LabeledField("Prefix");
		suffixField = new LabeledField("Suffix");
		digitsField = new LabeledField("Increment index (digits, 0 = off)");
		digitsField.Value = "0";
		recurseChk = new ThemedCheckBox
		{
			Text = "Include subfolders",
			Width = 220
		};
		keepExtChk = new ThemedCheckBox
		{
			Text = "Keep file extension",
			Width = 220,
			Checked = true
		};
		LabeledField[] array = new LabeledField[5] { searchField, replaceField, prefixField, suffixField, digitsField };
		foreach (LabeledField labeledField in array)
		{
			labeledField.ValueChanged += delegate
			{
				RefreshRenamePreview();
			};
		}
		recurseChk.CheckedChanged += delegate
		{
			RefreshRenamePreview();
		};
		keepExtChk.CheckedChanged += delegate
		{
			RefreshRenamePreview();
		};
		renamePreview = new RenamePreviewList();
		renameStatus = new Label
		{
			AutoSize = true,
			Text = "",
			Font = new Font("Segoe UI", 9f)
		};
		renameBtn = new FlatButton
		{
			Text = "Rename files",
			Role = FlatButton.ButtonRole.Accent,
			Width = 190
		};
		renameBtn.Click += delegate
		{
			DoRename();
		};
		pageRenamer.Controls.Add(dirField);
		pageRenamer.Controls.Add(browseBtn);
		pageRenamer.Controls.Add(searchField);
		pageRenamer.Controls.Add(replaceField);
		pageRenamer.Controls.Add(prefixField);
		pageRenamer.Controls.Add(suffixField);
		pageRenamer.Controls.Add(digitsField);
		pageRenamer.Controls.Add(recurseChk);
		pageRenamer.Controls.Add(keepExtChk);
		pageRenamer.Controls.Add(renamePreview);
		pageRenamer.Controls.Add(renameStatus);
		pageRenamer.Controls.Add(renameBtn);
		pageRenamer.Resize += delegate
		{
			LayoutRenamerPage();
		};
	}

	private void LayoutRenamerPage()
	{
		int width = pageRenamer.Width;
		int num = 340;
		int num2 = 24;
		int num3 = 0;
		int num4 = 0;
		dirField.Location = new Point(0, num4);
		dirField.Size = new Size(num - 130, 52);
		browseBtn.Location = new Point(num - 120, num4 + 20);
		num4 += 62;
		LabeledField[] array = new LabeledField[5] { searchField, replaceField, prefixField, suffixField, digitsField };
		foreach (LabeledField labeledField in array)
		{
			labeledField.Location = new Point(0, num4);
			labeledField.Size = new Size(num, 52);
			num4 += 60;
		}
		recurseChk.Location = new Point(2, num4 + 4);
		keepExtChk.Location = new Point(2, num4 + 34);
		renameBtn.Location = new Point(2, pageRenamer.Height - renameBtn.Height);
		renameStatus.Location = new Point(2, pageRenamer.Height - renameBtn.Height - 26);
		int num5 = num + num2;
		renamePreview.Location = new Point(num5, 0);
		renamePreview.Size = new Size(width - num5, pageRenamer.Height - 6);
	}

	private void BrowseFolder()
	{
		FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		try
		{
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				dirField.Value = folderBrowserDialog.SelectedPath;
				RefreshRenamePreview();
			}
		}
		finally
		{
			((IDisposable)(object)folderBrowserDialog)?.Dispose();
		}
	}

	private RenameEngine.Rule CurrentRule()
	{
		int.TryParse(digitsField.Value.Trim(), out var result);
		if (result < 0)
		{
			result = 0;
		}
		RenameEngine.Rule rule = new RenameEngine.Rule();
		rule.Search = searchField.Value;
		rule.Replace = replaceField.Value;
		rule.Prefix = prefixField.Value;
		rule.Suffix = suffixField.Value;
		rule.Digits = result;
		rule.KeepExtension = keepExtChk.Checked;
		rule.IncludeSubfolders = recurseChk.Checked;
		rule.SortByName = true;
		return rule;
	}

	private void RefreshRenamePreview()
	{
		string value = dirField.Value;
		if (string.IsNullOrEmpty(value) || !Directory.Exists(value))
		{
			renamePreview.SetRows(null);
			renameStatus.Text = "";
			currentPlan = null;
			return;
		}
		currentPlan = RenameEngine.Plan(value, CurrentRule());
		List<RenamePreviewList.Row> list = new List<RenamePreviewList.Row>();
		int num = 0;
		int num2 = 0;
		foreach (RenameEngine.Item item in currentPlan)
		{
			if (item.Changed)
			{
				num++;
			}
			if (!string.IsNullOrEmpty(item.Warning))
			{
				num2++;
			}
			list.Add(new RenamePreviewList.Row
			{
				Left = item.OldName,
				Right = ((!string.IsNullOrEmpty(item.Warning)) ? (item.NewName + "  (" + item.Warning + ")") : item.NewName),
				Warn = !string.IsNullOrEmpty(item.Warning),
				Changed = item.Changed
			});
		}
		renamePreview.SetRows(list);
		renameStatus.Text = currentPlan.Count + " files • " + num + " will change" + ((num2 <= 0) ? "" : (" • " + num2 + " warning" + ((num2 != 1) ? "s" : "")));
		renameStatus.ForeColor = ((num2 <= 0) ? Theme.Current.TextMuted : Color.FromArgb(226, 96, 90));
	}

	private void DoRename()
	{
		if (currentPlan == null || currentPlan.Count == 0)
		{
			MessageBox.Show("Pick a folder first.", "Nothing to rename", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		int count = currentPlan.FindAll((RenameEngine.Item i) => i.Changed).Count;
		if (count == 0)
		{
			MessageBox.Show("These settings don't change any file names.", "Nothing to do", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		int count2 = currentPlan.FindAll((RenameEngine.Item i) => !string.IsNullOrEmpty(i.Warning)).Count;
		string text = "Rename " + count + " file(s)?";
		if (count2 > 0)
		{
			string text2 = text;
			text = text2 + "\n\n" + count2 + " have warnings (name collisions) and will be skipped.";
		}
		if (MessageBox.Show(text, "Confirm rename", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
		{
			List<string> errors;
			int num = RenameEngine.Apply(currentPlan, force: false, out errors);
			RefreshRenamePreview();
			string text3 = num + " file(s) renamed.";
			if (errors.Count > 0)
			{
				string text2 = text3;
				text3 = text2 + "\n\n" + errors.Count + " error(s):\n" + string.Join("\n", errors.ToArray());
			}
			MessageBox.Show(text3, "Done", MessageBoxButtons.OK, (errors.Count <= 0) ? MessageBoxIcon.Asterisk : MessageBoxIcon.Exclamation);
		}
	}

	private void BuildInjectorPage()
	{
		pageInjector = new Panel
		{
			Dock = DockStyle.Fill
		};
		slotRows = new Dictionary<MapType, MapSlotRow>();
		MapType[] array = (MapType[])Enum.GetValues(typeof(MapType));
		foreach (MapType mapType in array)
		{
			MapSlotRow mapSlotRow = new MapSlotRow(mapType);
			mapSlotRow.Changed += delegate
			{
				OnInjectorChanged();
			};
			slotRows[mapType] = mapSlotRow;
			pageInjector.Controls.Add(mapSlotRow);
		}
		showOptionalChk = new ThemedCheckBox
		{
			Text = "Show optional maps (AO, Opacity, Emission)",
			Width = 360,
			Checked = false
		};
		showOptionalChk.CheckedChanged += delegate
		{
			LayoutInjectorPage();
		};
		pageInjector.Controls.Add(showOptionalChk);
		Label label = new Label();
		label.Name = "shaderLabel";
		label.AutoSize = true;
		label.Text = "Shader type";
		label.Font = new Font("Segoe UI", 9f);
		Label value = label;
		shaderCombo = new ThemedComboBox
		{
			Width = 260
		};
		shaderCombo.Items.AddRange(new object[2] { "OpenPBR", "USD" });
		shaderCombo.SelectedIndex = 0;
		pageInjector.Controls.Add(value);
		pageInjector.Controls.Add(shaderCombo);
		materialNameField = new LabeledField("Material name (derived)");
		materialNameField.Box.ReadOnly = true;
		materialNameField.Value = "M_material";
		pageInjector.Controls.Add(materialNameField);
		injectorClearBtn = new FlatButton
		{
			Text = "Clear",
			Role = FlatButton.ButtonRole.Secondary,
			Width = 130
		};
		injectorClearBtn.Click += delegate
		{
			ClearInjector();
		};
		injectBtn = new FlatButton
		{
			Text = "Export preset",
			Role = FlatButton.ButtonRole.Accent,
			Width = 190
		};
		injectBtn.Click += delegate
		{
			ExportPreset();
		};
		injectorStatus = new Label
		{
			AutoSize = false,
			Text = "",
			Font = new Font("Segoe UI", 9f),
			AutoEllipsis = true
		};
		pageInjector.Controls.Add(injectorClearBtn);
		pageInjector.Controls.Add(injectBtn);
		pageInjector.Controls.Add(injectorStatus);
		pageInjector.Resize += delegate
		{
			LayoutInjectorPage();
		};
	}

	private void LayoutInjectorPage()
	{
		if (slotRows == null)
		{
			return;
		}
		int width = pageInjector.Width;
		int num = Math.Min(width, 620);
		int num2 = 4;
		MapType[] array = (MapType[])Enum.GetValues(typeof(MapType));
		foreach (MapType mapType in array)
		{
			MapSlotRow mapSlotRow = slotRows[mapType];
			if (mapSlotRow.Visible = MaterialPreset.IsCore(mapType) || showOptionalChk.Checked)
			{
				mapSlotRow.Location = new Point(0, num2);
				mapSlotRow.Size = new Size(num, 46);
				num2 += 50;
			}
		}
		showOptionalChk.Location = new Point(2, num2 + 6);
		num2 += 42;
		Control control = pageInjector.Controls["shaderLabel"];
		if (control != null)
		{
			control.Location = new Point(2, num2);
		}
		shaderCombo.Location = new Point(2, num2 + 20);
		materialNameField.Location = new Point(2, num2 + 56);
		materialNameField.Size = new Size(Math.Min(num, 320), 52);
		int num3 = Math.Max(num2 + 120, pageInjector.Height - 78);
		injectBtn.Location = new Point(2, num3);
		injectorClearBtn.Location = new Point(injectBtn.Right + 12, num3);
		injectorStatus.Location = new Point(2, num3 + injectBtn.Height + 8);
		injectorStatus.Size = new Size(num, 20);
	}

	private Dictionary<MapType, string> CurrentMaps()
	{
		Dictionary<MapType, string> dictionary = new Dictionary<MapType, string>();
		foreach (KeyValuePair<MapType, MapSlotRow> slotRow in slotRows)
		{
			if (!string.IsNullOrEmpty(slotRow.Value.FilePath))
			{
				dictionary[slotRow.Key] = slotRow.Value.FilePath;
			}
		}
		return dictionary;
	}

	private void OnInjectorChanged()
	{
		materialNameField.Value = MaterialPreset.DeriveMaterialName(CurrentMaps());
	}

	private void ClearInjector()
	{
		foreach (KeyValuePair<MapType, MapSlotRow> slotRow in slotRows)
		{
			slotRow.Value.Clear();
		}
		shaderCombo.SelectedIndex = 0;
		materialNameField.Value = "M_material";
		injectorStatus.Text = "";
	}

	private void ExportPreset()
	{
		Dictionary<MapType, string> dictionary = CurrentMaps();
		if (dictionary.Count == 0)
		{
			MessageBox.Show("Assign at least one texture map first.", "Nothing to export", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		string shaderType = ((shaderCombo.SelectedIndex != 1) ? "openPBRSurface" : "usdPreviewSurface");
		string text = MaterialPreset.DeriveMaterialName(dictionary);
		string message = text + " — exported " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
		string json = MaterialPreset.BuildJson(message, text, shaderType, dictionary);
		try
		{
			string path = MaterialPreset.WriteAtomic(json);
			injectorStatus.ForeColor = Theme.Current.TextMuted;
			injectorStatus.Text = "Wrote " + MaterialPreset.ForwardSlashes(path);
		}
		catch (Exception ex)
		{
			injectorStatus.ForeColor = Color.FromArgb(226, 96, 90);
			injectorStatus.Text = "Export failed: " + ex.Message;
			MessageBox.Show("Couldn't write the preset:" + Environment.NewLine + ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private static ImageCore.OutputFormat SelectedFormat(ComboBox combo)
	{
		return combo.SelectedIndex switch
		{
			1 => ImageCore.OutputFormat.Tga, 
			2 => ImageCore.OutputFormat.Tiff, 
			3 => ImageCore.OutputFormat.Bmp, 
			4 => ImageCore.OutputFormat.Jpeg, 
			_ => ImageCore.OutputFormat.Png, 
		};
	}

	private static string FilterFor(ImageCore.OutputFormat fmt)
	{
		return fmt switch
		{
			ImageCore.OutputFormat.Tga => "Targa|*.tga", 
			ImageCore.OutputFormat.Tiff => "TIFF|*.tif", 
			ImageCore.OutputFormat.Bmp => "Bitmap|*.bmp", 
			ImageCore.OutputFormat.Jpeg => "JPEG|*.jpg", 
			_ => "PNG|*.png", 
		};
	}

	private void ApplyTheme()
	{
		Palette current = Theme.Current;
		BackColor = current.AppBackground;
		header.BackColor = current.Surface;
		toggleHint.ForeColor = current.TextMuted;
		pageHost.BackColor = current.AppBackground;
		Panel[] array = new Panel[5] { pagePack, pageUnpack, pageUtility, pageRenamer, pageInjector };
		foreach (Panel panel in array)
		{
			panel.BackColor = current.AppBackground;
		}
		StyleLabels(this, current);
		ThemeAll(this, current);
		LayoutHeader();
		LayoutPackPage();
		LayoutUnpackPage();
		LayoutUtilityPage();
		LayoutRenamerPage();
		LayoutInjectorPage();
		Invalidate(invalidateChildren: true);
	}

	private void StyleLabels(Control c, Palette p)
	{
		if (c is LabeledField)
		{
			return;
		}
		foreach (Control control in c.Controls)
		{
			if (control is Label label && !(control is LinkLabel))
			{
				if (label == toggleHint || label.Name == "presetLabel" || label.Name == "fmtLabel" || label.Name == "opLabel" || label.Name == "ufLabel" || label.Name == "shaderLabel" || label == injectorStatus || label == renameStatus)
				{
					label.ForeColor = p.TextMuted;
				}
				else
				{
					label.ForeColor = p.Text;
				}
				label.BackColor = Color.Transparent;
			}
			StyleLabels(control, p);
		}
	}

	private void ThemeAll(Control c, Palette p)
	{
		foreach (Control control in c.Controls)
		{
			if (control is IThemeable themeable)
			{
				themeable.ApplyTheme(p);
			}
			ThemeAll(control, p);
		}
	}
}
