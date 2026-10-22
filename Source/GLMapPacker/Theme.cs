using System;
using System.Drawing;

namespace GLMapPacker;

public static class Theme
{
	// One blue accent in both themes; dark mode uses a slightly brighter blue to keep contrast.
	public static readonly Palette Light = new Palette
	{
		IsDark = false,
		AppBackground = FromHex("#F3F5F8"),
		Surface = FromHex("#FFFFFF"),
		SurfaceAlt = FromHex("#EEF1F5"),
		Border = FromHex("#DCE1E8"),
		Text = FromHex("#18202B"),
		TextMuted = FromHex("#5E6B7A"),
		Accent = FromHex("#2F6BFF"),
		AccentHover = FromHex("#4A7FFF"),
		AccentText = FromHex("#FFFFFF"),
		TrackBackground = FromHex("#E1E6ED"),
		Success = FromHex("#1F9D61"),
		Warning = FromHex("#C98A0C"),
		Danger = FromHex("#D9443F")
	};

	public static readonly Palette Dark = new Palette
	{
		IsDark = true,
		AppBackground = FromHex("#15171C"),
		Surface = FromHex("#1E2128"),
		SurfaceAlt = FromHex("#272B33"),
		Border = FromHex("#323845"),
		Text = FromHex("#ECEFF4"),
		TextMuted = FromHex("#98A2AF"),
		Accent = FromHex("#3D73FF"),
		AccentHover = FromHex("#5686FF"),
		AccentText = FromHex("#FFFFFF"),
		TrackBackground = FromHex("#2C313A"),
		Success = FromHex("#3CC285"),
		Warning = FromHex("#E8B33D"),
		Danger = FromHex("#F06560")
	};

	private static Palette current = Dark;

	public static Palette Current => current;

	public static bool IsDark => current.IsDark;

	public static event EventHandler ThemeChanged;

	public static void Set(bool dark)
	{
		Palette palette = ((!dark) ? Light : Dark);
		if (palette != current)
		{
			current = palette;
			if (Theme.ThemeChanged != null)
			{
				Theme.ThemeChanged(null, EventArgs.Empty);
			}
		}
	}

	public static void Toggle()
	{
		Set(!current.IsDark);
	}

	public static Color FromHex(string hex)
	{
		hex = hex.TrimStart('#');
		int red = Convert.ToInt32(hex.Substring(0, 2), 16);
		int green = Convert.ToInt32(hex.Substring(2, 2), 16);
		int blue = Convert.ToInt32(hex.Substring(4, 2), 16);
		return Color.FromArgb(red, green, blue);
	}

	public static Color Mix(Color a, Color b, double t)
	{
		int red = (int)((double)(int)a.R + (double)(b.R - a.R) * t);
		int green = (int)((double)(int)a.G + (double)(b.G - a.G) * t);
		int blue = (int)((double)(int)a.B + (double)(b.B - a.B) * t);
		return Color.FromArgb(red, green, blue);
	}
}
