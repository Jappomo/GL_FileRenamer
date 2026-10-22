using System;
using System.Drawing;

namespace GLMapPacker;

public static class Theme
{
	public static readonly Palette Light = new Palette
	{
		IsDark = false,
		AppBackground = FromHex("#F4F6F9"),
		Surface = FromHex("#FFFFFF"),
		SurfaceAlt = FromHex("#EDF1F6"),
		Border = FromHex("#D9DEE7"),
		Text = FromHex("#1B2430"),
		TextMuted = FromHex("#5C6B7A"),
		Accent = FromHex("#2F6BFF"),
		AccentHover = FromHex("#4C82FF"),
		AccentText = FromHex("#FFFFFF"),
		TrackBackground = FromHex("#E2E7EF")
	};

	public static readonly Palette Dark = new Palette
	{
		IsDark = true,
		AppBackground = FromHex("#1B1E24"),
		Surface = FromHex("#252A32"),
		SurfaceAlt = FromHex("#2E343D"),
		Border = FromHex("#3A414B"),
		Text = FromHex("#ECEFF3"),
		TextMuted = FromHex("#9AA5B1"),
		Accent = FromHex("#FF8A73"),
		AccentHover = FromHex("#FFA491"),
		AccentText = FromHex("#241009"),
		TrackBackground = FromHex("#31373F")
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
