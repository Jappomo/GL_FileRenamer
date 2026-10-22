using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GLMapPacker;

// Shared UI metrics: DPI scaling, the font set, and the Windows title bar colours.
internal static class Ui
{
	private static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();

	// Screen DPI / 96. All layout numbers in the app are written at 96 DPI and go through S().
	public static float Scale { get; private set; } = 1f;

	public static void Init()
	{
		using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
		Scale = Math.Max(1f, g.DpiX / 96f);
	}

	public static int S(int v)
	{
		return (int)Math.Round(v * Scale);
	}

	public static float S(float v)
	{
		return v * Scale;
	}

	public static Size S(int w, int h)
	{
		return new Size(S(w), S(h));
	}

	// Fonts are in points, so Windows already scales them for DPI; cached so paint code doesn't allocate.
	public static Font Font(float size, bool semibold = false, FontStyle style = FontStyle.Regular)
	{
		string key = size + (semibold ? "sb" : "") + (int)style;
		if (!fonts.TryGetValue(key, out var f))
		{
			f = semibold ? new Font("Segoe UI Semibold", size, style | FontStyle.Bold) : new Font("Segoe UI", size, style);
			fonts[key] = f;
		}
		return f;
	}

	public static Font Body => Font(9.5f);

	public static Font Small => Font(9f);

	public static Font Label => Font(8.5f, semibold: true);

	public static Font Strong => Font(10f, semibold: true);

	public static Font Title => Font(10.5f, semibold: true);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

	private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

	private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	private const int DWMWA_BORDER_COLOR = 34;

	private const int DWMWA_CAPTION_COLOR = 35;

	private const int DWMWA_TEXT_COLOR = 36;

	// Dark title bar on Windows 10 1809+, and on Windows 11 a caption that matches the app header exactly.
	// Each call fails harmlessly on systems that don't support it.
	public static void ApplyWindowFrame(Form form, Palette p)
	{
		if (!form.IsHandleCreated || Environment.OSVersion.Platform != PlatformID.Win32NT)
		{
			return;
		}
		try
		{
			int dark = p.IsDark ? 1 : 0;
			if (DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, 4) != 0)
			{
				DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref dark, 4);
			}
			int caption = ColorRef(p.Surface);
			int text = ColorRef(p.Text);
			int border = ColorRef(p.Border);
			DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref caption, 4);
			DwmSetWindowAttribute(form.Handle, DWMWA_TEXT_COLOR, ref text, 4);
			DwmSetWindowAttribute(form.Handle, DWMWA_BORDER_COLOR, ref border, 4);
		}
		catch (DllNotFoundException)
		{
		}
		catch (EntryPointNotFoundException)
		{
		}
	}

	private static int ColorRef(Color c)
	{
		return c.R | (c.G << 8) | (c.B << 16);
	}
}

public enum NoticeKind
{
	Info,
	Success,
	Warning,
	Error
}

// App-wide notifications. Controls report here; the main window shows them as toasts and in the status bar.
public static class Notifier
{
	public static event Action<NoticeKind, string> Posted;

	public static void Post(NoticeKind kind, string message)
	{
		Posted?.Invoke(kind, message);
	}

	public static void Info(string message)
	{
		Post(NoticeKind.Info, message);
	}

	public static void Success(string message)
	{
		Post(NoticeKind.Success, message);
	}

	public static void Warning(string message)
	{
		Post(NoticeKind.Warning, message);
	}

	public static void Error(string message)
	{
		Post(NoticeKind.Error, message);
	}
}
