using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GLMapPacker;

// Float32 image with interleaved channels (1 = gray, 3 = RGB, 4 = RGBA), values nominally 0..1.
public sealed class FloatImage
{
	public readonly int Width;

	public readonly int Height;

	public readonly int Channels;

	public readonly float[] Data;

	// Bit depth of the file this image was loaded from (8 or 16); 0 when generated.
	public int SourceBitDepth { get; set; }

	public FloatImage(int width, int height, int channels)
	{
		Width = width;
		Height = height;
		Channels = channels;
		Data = new float[(long)width * height * channels];
	}

	public static FloatImage Load(string path)
	{
		if (string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase))
		{
			FloatImage png = PngIO.Read(path);
			if (png != null)
			{
				return png;
			}
		}
		using Bitmap bmp = ImageCore.Load(path);
		FloatImage img = FromBitmap(bmp);
		img.SourceBitDepth = 8;
		return img;
	}

	public static FloatImage FromBitmap(Bitmap bmp)
	{
		int w = bmp.Width;
		int h = bmp.Height;
		FloatImage img = new FloatImage(w, h, 4);
		BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			int stride = data.Stride;
			byte[] buf = new byte[Math.Abs(stride) * h];
			Marshal.Copy(data.Scan0, buf, 0, buf.Length);
			const float k = 1f / 255f;
			for (int y = 0; y < h; y++)
			{
				int s = y * stride;
				int d = y * w * 4;
				for (int x = 0; x < w; x++, s += 4, d += 4)
				{
					img.Data[d] = buf[s + 2] * k;
					img.Data[d + 1] = buf[s + 1] * k;
					img.Data[d + 2] = buf[s] * k;
					img.Data[d + 3] = buf[s + 3] * k;
				}
			}
		}
		finally
		{
			bmp.UnlockBits(data);
		}
		return img;
	}

	// 8-bit ARGB bitmap for on-screen preview.
	public Bitmap ToBitmap()
	{
		Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
		BitmapData data = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		try
		{
			int stride = data.Stride;
			byte[] buf = new byte[Math.Abs(stride) * Height];
			for (int y = 0; y < Height; y++)
			{
				int d = y * stride;
				int s = y * Width * Channels;
				for (int x = 0; x < Width; x++, d += 4, s += Channels)
				{
					byte r = ToByte(Data[s]);
					byte g = Channels >= 3 ? ToByte(Data[s + 1]) : r;
					byte b = Channels >= 3 ? ToByte(Data[s + 2]) : r;
					buf[d] = b;
					buf[d + 1] = g;
					buf[d + 2] = r;
					buf[d + 3] = Channels == 4 ? ToByte(Data[s + 3]) : byte.MaxValue;
				}
			}
			Marshal.Copy(buf, 0, data.Scan0, buf.Length);
		}
		finally
		{
			bmp.UnlockBits(data);
		}
		return bmp;
	}

	private static byte ToByte(float v)
	{
		if (v <= 0f)
		{
			return 0;
		}
		if (v >= 1f)
		{
			return byte.MaxValue;
		}
		return (byte)(v * 255f + 0.5f);
	}

	// Single-channel luminance copy (exact for grayscale sources).
	public FloatImage ToGray()
	{
		if (Channels == 1)
		{
			return Clone();
		}
		FloatImage g = new FloatImage(Width, Height, 1);
		int n = Width * Height;
		for (int i = 0; i < n; i++)
		{
			int s = i * Channels;
			g.Data[i] = Channels >= 3 ? (0.2126f * Data[s] + 0.7152f * Data[s + 1] + 0.0722f * Data[s + 2]) : Data[s];
		}
		return g;
	}

	public FloatImage Clone()
	{
		FloatImage c = new FloatImage(Width, Height, Channels);
		Array.Copy(Data, c.Data, Data.Length);
		c.SourceBitDepth = SourceBitDepth;
		return c;
	}

	// Size whose longest edge is maxSide, keeping aspect ratio (scales up or down). maxSide <= 0 keeps the size.
	public static Size FitSize(int w, int h, int maxSide)
	{
		if (maxSide <= 0 || Math.Max(w, h) == maxSide)
		{
			return new Size(w, h);
		}
		double s = (double)maxSide / Math.Max(w, h);
		return new Size(Math.Max(1, (int)Math.Round(w * s)), Math.Max(1, (int)Math.Round(h * s)));
	}

	// Separable tent-filter resample. The filter widens when downscaling so it averages instead of aliasing.
	public FloatImage Resize(int newW, int newH)
	{
		if (newW == Width && newH == Height)
		{
			return Clone();
		}
		float[] tmp = new float[(long)newW * Height * Channels];
		ResampleAxis(Data, Width, Height, newW, true, tmp);
		FloatImage dst = new FloatImage(newW, newH, Channels);
		ResampleAxis(tmp, newW, Height, newH, false, dst.Data);
		dst.SourceBitDepth = SourceBitDepth;
		return dst;
	}

	private void ResampleAxis(float[] src, int srcW, int srcH, int newLen, bool horizontal, float[] dst)
	{
		int c = Channels;
		int oldLen = horizontal ? srcW : srcH;
		double scale = (double)oldLen / newLen;
		double radius = Math.Max(1.0, scale);
		int[] start = new int[newLen];
		float[][] weights = new float[newLen][];
		for (int i = 0; i < newLen; i++)
		{
			double center = (i + 0.5) * scale - 0.5;
			int lo = (int)Math.Floor(center - radius) + 1;
			int hi = (int)Math.Ceiling(center + radius) - 1;
			float[] w = new float[hi - lo + 1];
			double sum = 0.0;
			for (int j = lo; j <= hi; j++)
			{
				double wt = Math.Max(0.0, 1.0 - Math.Abs(j - center) / radius);
				w[j - lo] = (float)wt;
				sum += wt;
			}
			for (int j = 0; j < w.Length; j++)
			{
				w[j] = (float)(w[j] / sum);
			}
			start[i] = lo;
			weights[i] = w;
		}
		int lines = horizontal ? srcH : srcW;
		Parallel.For(0, lines, line =>
		{
			float[] acc = new float[c];
			for (int i = 0; i < newLen; i++)
			{
				Array.Clear(acc, 0, c);
				float[] w = weights[i];
				for (int k = 0; k < w.Length; k++)
				{
					int j = Math.Min(oldLen - 1, Math.Max(0, start[i] + k));
					int s = horizontal ? (line * srcW + j) * c : (j * srcW + line) * c;
					for (int ch = 0; ch < c; ch++)
					{
						acc[ch] += src[s + ch] * w[k];
					}
				}
				int d = horizontal ? (line * newLen + i) * c : (i * srcW + line) * c;
				for (int ch = 0; ch < c; ch++)
				{
					dst[d + ch] = acc[ch];
				}
			}
		});
	}
}
