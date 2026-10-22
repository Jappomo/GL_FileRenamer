using System;
using System.Windows.Forms;

namespace GLMapPacker;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Theme.Set(dark: true);
		Application.Run(new MainForm());
	}
}
