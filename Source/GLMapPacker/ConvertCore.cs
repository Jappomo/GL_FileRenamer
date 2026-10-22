using System;
using System.Threading;
using System.Threading.Tasks;

namespace GLMapPacker;

// Map conversions on Float32 data. Pixel-sized settings (blur, radius) are given at output resolution;
// pxScale shrinks them when working on a smaller preview so the preview matches the final result.
public static class ConvertCore
{
	public enum Mode
	{
		NormalToHeight,
		HeightToNormal,
		HeightToAO
	}

	public sealed class Settings
	{
		public Mode Mode;

		// Normal -> Height
		public float HeightIntensity = 1f;

		public float Baseline = 0.5f;

		public float Contrast = 0f;

		public float SurfaceBlur = 0f;

		// Height -> Normal
		public float NormalStrength = 2f;

		public float NormalSmoothing = 0f;

		public bool InvertHeight;

		// Shared by Normal -> Height and Height -> Normal
		public bool DirectX;

		// Height -> AO
		public float AoRadius = 24f;

		public float AoDepth = 3f;

		public float AoIntensity = 1f;

		public float AoContrast = 1f;

		public int AoDirections = 16;

		public Settings Clone()
		{
			return (Settings)MemberwiseClone();
		}
	}

	public static FloatImage Run(FloatImage src, Settings s, double pxScale, IProgress<int> progress)
	{
		return s.Mode switch
		{
			Mode.HeightToNormal => HeightToNormal(src, s, pxScale, progress),
			Mode.HeightToAO => HeightToAO(src, s, pxScale, progress),
			_ => NormalToHeight(src, s, pxScale, progress),
		};
	}

	// ---------------------------------------------------------------- Normal -> Height

	// Integrates the normal map's slopes with an FFT Poisson solve (Frankot-Chellappa),
	// which gives the least-squares height for the whole image at once instead of drifting like a line walk.
	public static FloatImage NormalToHeight(FloatImage src, Settings s, double pxScale, IProgress<int> progress)
	{
		int w = src.Width;
		int h = src.Height;
		int pw = NextPow2(w);
		int ph = NextPow2(h);
		float[] gxRe = new float[pw * ph];
		float[] gxIm = new float[pw * ph];
		float[] gyRe = new float[pw * ph];
		float[] gyIm = new float[pw * ph];
		int c = src.Channels;
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				int i = (y * w + x) * c;
				float nx = src.Data[i] * 2f - 1f;
				float ny = (c > 1 ? src.Data[i + 1] : 0.5f) * 2f - 1f;
				float nz = (c > 2 ? src.Data[i + 2] : 1f) * 2f - 1f;
				if (s.DirectX)
				{
					ny = -ny;
				}
				nz = Math.Max(nz, 0.05f);
				// Tangent-space Y points up the image, rows go down: dh/drow = +ny/nz.
				gxRe[y * pw + x] = -nx / nz;
				gyRe[y * pw + x] = ny / nz;
			}
		}
		Report(progress, 10);
		Fft2D(gxRe, gxIm, pw, ph, inverse: false);
		Report(progress, 30);
		Fft2D(gyRe, gyIm, pw, ph, inverse: false);
		Report(progress, 50);
		for (int v = 0; v < ph; v++)
		{
			double wy = 2.0 * Math.PI * (v < ph / 2 ? v : v - ph) / ph;
			for (int u = 0; u < pw; u++)
			{
				double wx = 2.0 * Math.PI * (u < pw / 2 ? u : u - pw) / pw;
				int i = v * pw + u;
				double d = wx * wx + wy * wy;
				if (d < 1e-12)
				{
					gxRe[i] = 0f;
					gxIm[i] = 0f;
					continue;
				}
				// H = -i (wx Gx + wy Gy) / (wx^2 + wy^2)
				double re = wx * gxRe[i] + wy * gyRe[i];
				double im = wx * gxIm[i] + wy * gyIm[i];
				gxRe[i] = (float)(im / d);
				gxIm[i] = (float)(-re / d);
			}
		}
		Fft2D(gxRe, gxIm, pw, ph, inverse: true);
		Report(progress, 75);
		FloatImage height = new FloatImage(w, h, 1);
		for (int y = 0; y < h; y++)
		{
			Array.Copy(gxRe, y * pw, height.Data, y * w, w);
		}
		float sigma = (float)(s.SurfaceBlur * pxScale);
		if (sigma > 0.05f)
		{
			GaussianBlur(height.Data, w, h, sigma);
		}
		Report(progress, 90);
		ShapeHeight(height.Data, s);
		Report(progress, 100);
		return height;
	}

	// Puts the median (the most common, usually flat, surface) on the baseline, normalises by a robust
	// range, then applies contrast and intensity.
	private static void ShapeHeight(float[] data, Settings s)
	{
		float[] sample = Sample(data);
		Array.Sort(sample);
		float median = sample[sample.Length / 2];
		for (int i = 0; i < sample.Length; i++)
		{
			sample[i] = Math.Abs(sample[i] - median);
		}
		Array.Sort(sample);
		// 99.7th percentile of the deviation, so a few spikes don't flatten everything else.
		float range = sample[Math.Min(sample.Length - 1, (int)(sample.Length * 0.997))];
		float inv = range > 1e-12f ? 1f / range : 0f;
		double p = Math.Pow(2.0, -s.Contrast * 1.5);
		float half = 0.5f * s.HeightIntensity;
		for (int i = 0; i < data.Length; i++)
		{
			float d = (data[i] - median) * inv;
			d = d < -1f ? -1f : (d > 1f ? 1f : d);
			if (s.Contrast != 0f)
			{
				d = Math.Sign(d) * (float)Math.Pow(Math.Abs(d), p);
			}
			float v = s.Baseline + half * d;
			data[i] = v < 0f ? 0f : (v > 1f ? 1f : v);
		}
	}

	// Evenly spaced subset of at most ~64K values.
	private static float[] Sample(float[] data)
	{
		int step = Math.Max(1, data.Length / 65536);
		float[] sample = new float[(data.Length + step - 1) / step];
		for (int i = 0, n = 0; i < data.Length; i += step, n++)
		{
			sample[n] = data[i];
		}
		return sample;
	}

	// ---------------------------------------------------------------- Height -> Normal

	public static FloatImage HeightToNormal(FloatImage src, Settings s, double pxScale, IProgress<int> progress)
	{
		FloatImage gray = src.ToGray();
		int w = gray.Width;
		int h = gray.Height;
		float[] ht = gray.Data;
		if (s.InvertHeight)
		{
			for (int i = 0; i < ht.Length; i++)
			{
				ht[i] = 1f - ht[i];
			}
		}
		float sigma = (float)(s.NormalSmoothing * pxScale);
		if (sigma > 0.05f)
		{
			GaussianBlur(ht, w, h, sigma);
		}
		Report(progress, 30);
		// Sobel slope is height per pixel; times width it becomes height per texture width, so the
		// result doesn't change with resolution. Strength 1 = full height range spans 1% of the width.
		float k = w * s.NormalStrength * 0.01f / 8f;
		float ySign = s.DirectX ? -1f : 1f;
		FloatImage dst = new FloatImage(w, h, 3);
		int done = 0;
		Parallel.For(0, h, y =>
		{
			int ym = (y - 1 + h) % h;
			int yp = (y + 1) % h;
			for (int x = 0; x < w; x++)
			{
				int xm = (x - 1 + w) % w;
				int xp = (x + 1) % w;
				float tl = ht[ym * w + xm];
				float t = ht[ym * w + x];
				float tr = ht[ym * w + xp];
				float l = ht[y * w + xm];
				float r = ht[y * w + xp];
				float bl = ht[yp * w + xm];
				float b = ht[yp * w + x];
				float br = ht[yp * w + xp];
				float gx = (tr + 2f * r + br) - (tl + 2f * l + bl);
				float gy = (bl + 2f * b + br) - (tl + 2f * t + tr);
				float nx = -gx * k;
				float ny = gy * k * ySign;
				float inv = 1f / (float)Math.Sqrt(nx * nx + ny * ny + 1f);
				int o = (y * w + x) * 3;
				dst.Data[o] = nx * inv * 0.5f + 0.5f;
				dst.Data[o + 1] = ny * inv * 0.5f + 0.5f;
				dst.Data[o + 2] = inv * 0.5f + 0.5f;
			}
			ReportRows(progress, ref done, h, 30, 100);
		});
		return dst;
	}

	// ---------------------------------------------------------------- Height -> AO

	// Horizon-based AO (HBAO) on the height field: for each pixel, march several directions, track the
	// highest horizon angle and accumulate sin(horizon) with a distance falloff. Per-pixel rotated
	// directions remove banding; a light blur then removes the rotation noise.
	public static FloatImage HeightToAO(FloatImage src, Settings s, double pxScale, IProgress<int> progress)
	{
		FloatImage gray = src.ToGray();
		int w = gray.Width;
		int h = gray.Height;
		float[] ht = gray.Data;
		float radius = Math.Max(1f, (float)(s.AoRadius * pxScale));
		// Height 1.0 rises AoDepth % of the texture width, in this image's pixels.
		float depth = s.AoDepth * 0.01f * w;
		int dirs = Math.Max(4, s.AoDirections);
		int steps = dirs >= 32 ? 16 : (dirs >= 16 ? 12 : 8);
		float[] ao = new float[w * h];
		int done = 0;
		Parallel.For(0, h, y =>
		{
			for (int x = 0; x < w; x++)
			{
				float h0 = ht[y * w + x] * depth;
				uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663);
				hash = (hash ^ (hash >> 13)) * 1274126177u;
				float jitterA = (hash & 0xFFFF) / 65536f;
				float jitterS = ((hash >> 16) & 0xFFFF) / 65536f;
				float occ = 0f;
				for (int d = 0; d < dirs; d++)
				{
					double ang = (d + jitterA) * (2.0 * Math.PI / dirs);
					float dx = (float)Math.Cos(ang);
					float dy = (float)Math.Sin(ang);
					float maxSin = 0f;
					float dirOcc = 0f;
					for (int st = 0; st < steps; st++)
					{
						float t = radius * (st + jitterS) / steps;
						if (t < 0.5f)
						{
							continue;
						}
						float hs = SampleWrap(ht, w, h, x + dx * t, y + dy * t) * depth;
						float dh = hs - h0;
						if (dh <= 0f)
						{
							continue;
						}
						float sin = dh / (float)Math.Sqrt(dh * dh + t * t);
						if (sin > maxSin)
						{
							float r = t / radius;
							dirOcc += (sin - maxSin) * (1f - r * r);
							maxSin = sin;
						}
					}
					occ += dirOcc;
				}
				ao[y * w + x] = occ / dirs;
			}
			ReportRows(progress, ref done, h, 0, 90);
		});
		float sigma = Math.Max(0.6f, (float)(0.8 * pxScale));
		GaussianBlur(ao, w, h, sigma);
		FloatImage dst = new FloatImage(w, h, 1);
		for (int i = 0; i < ao.Length; i++)
		{
			float v = 1f - ao[i] * s.AoIntensity;
			v = v < 0f ? 0f : (v > 1f ? 1f : v);
			dst.Data[i] = (float)Math.Pow(v, s.AoContrast);
		}
		Report(progress, 100);
		return dst;
	}

	// ---------------------------------------------------------------- helpers

	private static float SampleWrap(float[] img, int w, int h, float fx, float fy)
	{
		int x0 = (int)Math.Floor(fx);
		int y0 = (int)Math.Floor(fy);
		float tx = fx - x0;
		float ty = fy - y0;
		x0 %= w;
		if (x0 < 0)
		{
			x0 += w;
		}
		y0 %= h;
		if (y0 < 0)
		{
			y0 += h;
		}
		int x1 = x0 + 1 == w ? 0 : x0 + 1;
		int y1 = y0 + 1 == h ? 0 : y0 + 1;
		float a = img[y0 * w + x0];
		float b = img[y0 * w + x1];
		float c = img[y1 * w + x0];
		float d = img[y1 * w + x1];
		return a + (b - a) * tx + (c - a) * ty + (a - b - c + d) * tx * ty;
	}

	// Separable Gaussian with wrap-around edges (texture maps are usually tileable).
	public static void GaussianBlur(float[] data, int w, int h, float sigma)
	{
		int r = (int)Math.Ceiling(sigma * 3f);
		float[] kernel = new float[r * 2 + 1];
		float sum = 0f;
		for (int i = -r; i <= r; i++)
		{
			float v = (float)Math.Exp(-(i * i) / (2.0 * sigma * sigma));
			kernel[i + r] = v;
			sum += v;
		}
		for (int i = 0; i < kernel.Length; i++)
		{
			kernel[i] /= sum;
		}
		float[] tmp = new float[data.Length];
		Parallel.For(0, h, y =>
		{
			int row = y * w;
			for (int x = 0; x < w; x++)
			{
				float acc = 0f;
				for (int k = -r; k <= r; k++)
				{
					int xx = ((x + k) % w + w) % w;
					acc += data[row + xx] * kernel[k + r];
				}
				tmp[row + x] = acc;
			}
		});
		Parallel.For(0, h, y =>
		{
			for (int x = 0; x < w; x++)
			{
				float acc = 0f;
				for (int k = -r; k <= r; k++)
				{
					int yy = ((y + k) % h + h) % h;
					acc += tmp[yy * w + x] * kernel[k + r];
				}
				data[y * w + x] = acc;
			}
		});
	}

	private static int NextPow2(int v)
	{
		int p = 1;
		while (p < v)
		{
			p <<= 1;
		}
		return p;
	}

	private static void Fft2D(float[] re, float[] im, int w, int h, bool inverse)
	{
		Parallel.For(0, h, () => new double[w * 2], (y, _, buf) =>
		{
			for (int x = 0; x < w; x++)
			{
				buf[x * 2] = re[y * w + x];
				buf[x * 2 + 1] = im[y * w + x];
			}
			Fft1D(buf, w, inverse);
			for (int x = 0; x < w; x++)
			{
				re[y * w + x] = (float)buf[x * 2];
				im[y * w + x] = (float)buf[x * 2 + 1];
			}
			return buf;
		}, _ => { });
		Parallel.For(0, w, () => new double[h * 2], (x, _, buf) =>
		{
			for (int y = 0; y < h; y++)
			{
				buf[y * 2] = re[y * w + x];
				buf[y * 2 + 1] = im[y * w + x];
			}
			Fft1D(buf, h, inverse);
			for (int y = 0; y < h; y++)
			{
				re[y * w + x] = (float)buf[y * 2];
				im[y * w + x] = (float)buf[y * 2 + 1];
			}
			return buf;
		}, _ => { });
	}

	// In-place radix-2 FFT on interleaved complex data. The inverse is scaled by 1/n.
	private static void Fft1D(double[] a, int n, bool inverse)
	{
		for (int i = 1, j = 0; i < n; i++)
		{
			int bit = n >> 1;
			for (; (j & bit) != 0; bit >>= 1)
			{
				j ^= bit;
			}
			j ^= bit;
			if (i < j)
			{
				double tr = a[i * 2];
				a[i * 2] = a[j * 2];
				a[j * 2] = tr;
				double ti = a[i * 2 + 1];
				a[i * 2 + 1] = a[j * 2 + 1];
				a[j * 2 + 1] = ti;
			}
		}
		for (int len = 2; len <= n; len <<= 1)
		{
			double ang = 2.0 * Math.PI / len * (inverse ? 1 : -1);
			double wr = Math.Cos(ang);
			double wi = Math.Sin(ang);
			for (int i = 0; i < n; i += len)
			{
				double cr = 1.0;
				double ci = 0.0;
				for (int k = 0; k < len / 2; k++)
				{
					int p = (i + k) * 2;
					int q = (i + k + len / 2) * 2;
					double vr = a[q] * cr - a[q + 1] * ci;
					double vi = a[q] * ci + a[q + 1] * cr;
					a[q] = a[p] - vr;
					a[q + 1] = a[p + 1] - vi;
					a[p] += vr;
					a[p + 1] += vi;
					double ncr = cr * wr - ci * wi;
					ci = cr * wi + ci * wr;
					cr = ncr;
				}
			}
		}
		if (inverse)
		{
			for (int i = 0; i < n * 2; i++)
			{
				a[i] /= n;
			}
		}
	}

	private static void Report(IProgress<int> progress, int value)
	{
		progress?.Report(value);
	}

	private static void ReportRows(IProgress<int> progress, ref int done, int total, int from, int to)
	{
		int n = Interlocked.Increment(ref done);
		if (progress != null && (n & 0x1F) == 0)
		{
			progress.Report(from + (to - from) * n / total);
		}
	}
}
