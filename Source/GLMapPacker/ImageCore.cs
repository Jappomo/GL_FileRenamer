using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace GLMapPacker;

public static class ImageCore
{
	public enum ChannelPick
	{
		R = 2,
		G = 1,
		B = 0,
		A = 3,
		White = -1,
		Black = -2
	}

	public enum OutputFormat
	{
		Png,
		Jpeg,
		Bmp,
		Tiff,
		Tga
	}

	private struct Src
	{
		public byte[] buf;

		public int w;

		public int h;

		public int stride;

		public BitmapData data;

		public Bitmap bmp;

		public static Src Open(Bitmap s)
		{
			Src result = default(Src);
			if (s == null)
			{
				return result;
			}
			result.bmp = s;
			result.w = s.Width;
			result.h = s.Height;
			result.data = s.LockBits(new Rectangle(0, 0, result.w, result.h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			result.stride = result.data.Stride;
			int num = Math.Abs(result.stride) * result.h;
			result.buf = new byte[num];
			Marshal.Copy(result.data.Scan0, result.buf, 0, num);
			return result;
		}

		public byte Sample(int x, int y, int offset, byte fallback)
		{
			if (buf == null || x >= w || y >= h)
			{
				return fallback;
			}
			return buf[y * stride + x * 4 + offset];
		}

		public void Close()
		{
			if (bmp != null && data != null)
			{
				bmp.UnlockBits(data);
				data = null;
			}
		}
	}

	public static Bitmap Load(string path)
	{
		byte[] buffer = File.ReadAllBytes(path);
		using MemoryStream stream = new MemoryStream(buffer);
		using Bitmap source = new Bitmap(stream);
		return CloneArgb32(source);
	}

	public static Bitmap CloneArgb32(Bitmap source)
	{
		int width = source.Width;
		int height = source.Height;
		Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		Rectangle rect = new Rectangle(0, 0, width, height);
		BitmapData bitmapData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		BitmapData bitmapData2 = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		try
		{
			int num = width * 4;
			byte[] array = new byte[num];
			for (int i = 0; i < height; i++)
			{
				IntPtr source2 = (IntPtr)(bitmapData.Scan0.ToInt64() + (long)i * (long)bitmapData.Stride);
				IntPtr destination = (IntPtr)(bitmapData2.Scan0.ToInt64() + (long)i * (long)bitmapData2.Stride);
				Marshal.Copy(source2, array, 0, num);
				Marshal.Copy(array, 0, destination, num);
			}
		}
		finally
		{
			source.UnlockBits(bitmapData);
			bitmap.UnlockBits(bitmapData2);
		}
		return bitmap;
	}

	public static Bitmap Pack(Bitmap srcR, ChannelPick pickR, Bitmap srcG, ChannelPick pickG, Bitmap srcB, ChannelPick pickB, Bitmap srcA, ChannelPick pickA, IProgress<int> progress)
	{
		Bitmap bitmap = srcR ?? srcG ?? srcB ?? srcA;
		if (bitmap == null)
		{
			return null;
		}
		int width = bitmap.Width;
		int height = bitmap.Height;
		Bitmap bitmap2 = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		Src s = Src.Open(srcR);
		Src s2 = Src.Open(srcG);
		Src s3 = Src.Open(srcB);
		Src s4 = Src.Open(srcA);
		BitmapData bitmapData = bitmap2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		try
		{
			int stride = bitmapData.Stride;
			int num = Math.Abs(stride) * height;
			byte[] array = new byte[num];
			for (int i = 0; i < height; i++)
			{
				int num2 = i * stride;
				for (int j = 0; j < width; j++)
				{
					int num3 = num2 + j * 4;
					array[num3] = Resolve(s3, j, i, pickB, 0);
					array[num3 + 1] = Resolve(s2, j, i, pickG, 0);
					array[num3 + 2] = Resolve(s, j, i, pickR, 0);
					array[num3 + 3] = Resolve(s4, j, i, pickA, byte.MaxValue);
				}
				if (progress != null && (i & 0x1F) == 0)
				{
					progress.Report((i + 1) * 100 / height);
				}
			}
			Marshal.Copy(array, 0, bitmapData.Scan0, num);
		}
		finally
		{
			bitmap2.UnlockBits(bitmapData);
			s.Close();
			s2.Close();
			s3.Close();
			s4.Close();
		}
		progress?.Report(100);
		return bitmap2;
	}

	private static byte Resolve(Src s, int x, int y, ChannelPick pick, byte missingFallback)
	{
		return pick switch
		{
			ChannelPick.White => byte.MaxValue, 
			ChannelPick.Black => 0, 
			_ => s.Sample(x, y, (int)pick, missingFallback), 
		};
	}

	public static void Split(Bitmap bmp, bool forceOpaqueAlpha, out Bitmap outR, out Bitmap outG, out Bitmap outB, out Bitmap outA, IProgress<int> progress)
	{
		int width = bmp.Width;
		int height = bmp.Height;
		Rectangle rect = new Rectangle(0, 0, width, height);
		outR = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		outG = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		outB = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		outA = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		BitmapData bitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		BitmapData bitmapData2 = outR.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		BitmapData bitmapData3 = outG.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		BitmapData bitmapData4 = outB.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		BitmapData bitmapData5 = outA.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
		try
		{
			int stride = bitmapData.Stride;
			int num = Math.Abs(stride) * height;
			byte[] array = new byte[num];
			byte[] array2 = new byte[num];
			byte[] array3 = new byte[num];
			byte[] array4 = new byte[num];
			byte[] array5 = new byte[num];
			Marshal.Copy(bitmapData.Scan0, array, 0, num);
			byte b = (byte)(forceOpaqueAlpha ? byte.MaxValue : 0);
			for (int i = 0; i < height; i++)
			{
				int num2 = i * stride;
				for (int j = 0; j < width; j++)
				{
					int num3 = num2 + j * 4;
					byte b2 = array[num3];
					byte b3 = array[num3 + 1];
					byte b4 = array[num3 + 2];
					byte b5 = array[num3 + 3];
					array2[num3] = b4;
					array2[num3 + 1] = b4;
					array2[num3 + 2] = b4;
					array2[num3 + 3] = ((!forceOpaqueAlpha) ? b4 : byte.MaxValue);
					array3[num3] = b3;
					array3[num3 + 1] = b3;
					array3[num3 + 2] = b3;
					array3[num3 + 3] = ((!forceOpaqueAlpha) ? b3 : byte.MaxValue);
					array4[num3] = b2;
					array4[num3 + 1] = b2;
					array4[num3 + 2] = b2;
					array4[num3 + 3] = ((!forceOpaqueAlpha) ? b2 : byte.MaxValue);
					array5[num3] = b5;
					array5[num3 + 1] = b5;
					array5[num3 + 2] = b5;
					array5[num3 + 3] = ((!forceOpaqueAlpha) ? b5 : byte.MaxValue);
				}
				if (progress != null && (i & 0x1F) == 0)
				{
					progress.Report((i + 1) * 100 / height);
				}
			}
			Marshal.Copy(array2, 0, bitmapData2.Scan0, num);
			Marshal.Copy(array3, 0, bitmapData3.Scan0, num);
			Marshal.Copy(array4, 0, bitmapData4.Scan0, num);
			Marshal.Copy(array5, 0, bitmapData5.Scan0, num);
		}
		finally
		{
			bmp.UnlockBits(bitmapData);
			outR.UnlockBits(bitmapData2);
			outG.UnlockBits(bitmapData3);
			outB.UnlockBits(bitmapData4);
			outA.UnlockBits(bitmapData5);
		}
		progress?.Report(100);
	}

	public static Bitmap FlipGreen(Bitmap src)
	{
		Bitmap bitmap = CloneArgb32(src);
		Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
		BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
		try
		{
			int num = Math.Abs(bitmapData.Stride) * bitmap.Height;
			byte[] array = new byte[num];
			Marshal.Copy(bitmapData.Scan0, array, 0, num);
			for (int i = 1; i < num; i += 4)
			{
				array[i] = (byte)(255 - array[i]);
			}
			Marshal.Copy(array, 0, bitmapData.Scan0, num);
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
		return bitmap;
	}

	public static Bitmap FlipVertical(Bitmap src)
	{
		Bitmap bitmap = CloneArgb32(src);
		bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
		return bitmap;
	}

	public static Bitmap FlipHorizontal(Bitmap src)
	{
		Bitmap bitmap = CloneArgb32(src);
		bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
		return bitmap;
	}

	public static Bitmap InvertRgb(Bitmap src)
	{
		Bitmap bitmap = CloneArgb32(src);
		Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
		BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
		try
		{
			int num = Math.Abs(bitmapData.Stride) * bitmap.Height;
			byte[] array = new byte[num];
			Marshal.Copy(bitmapData.Scan0, array, 0, num);
			for (int i = 0; i < num; i += 4)
			{
				array[i] = (byte)(255 - array[i]);
				array[i + 1] = (byte)(255 - array[i + 1]);
				array[i + 2] = (byte)(255 - array[i + 2]);
			}
			Marshal.Copy(array, 0, bitmapData.Scan0, num);
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
		return bitmap;
	}

	public static string ExtensionFor(OutputFormat fmt)
	{
		return fmt switch
		{
			OutputFormat.Jpeg => ".jpg", 
			OutputFormat.Bmp => ".bmp", 
			OutputFormat.Tiff => ".tif", 
			OutputFormat.Tga => ".tga", 
			_ => ".png", 
		};
	}

	public static OutputFormat FormatFromExtension(string path, OutputFormat fallback)
	{
		switch ((Path.GetExtension(path) ?? string.Empty).ToLowerInvariant())
		{
		case ".jpg":
		case ".jpeg":
			return OutputFormat.Jpeg;
		case ".bmp":
			return OutputFormat.Bmp;
		case ".tif":
		case ".tiff":
			return OutputFormat.Tiff;
		case ".tga":
			return OutputFormat.Tga;
		case ".png":
			return OutputFormat.Png;
		default:
			return fallback;
		}
	}

	public static void Save(Bitmap image, string path, OutputFormat fmt)
	{
		switch (fmt)
		{
		case OutputFormat.Jpeg:
			image.Save(path, ImageFormat.Jpeg);
			break;
		case OutputFormat.Bmp:
			image.Save(path, ImageFormat.Bmp);
			break;
		case OutputFormat.Tiff:
			image.Save(path, ImageFormat.Tiff);
			break;
		case OutputFormat.Tga:
			SaveTga(image, path);
			break;
		default:
			image.Save(path, ImageFormat.Png);
			break;
		}
	}

	private static void SaveTga(Bitmap image, string path)
	{
		int width = image.Width;
		int height = image.Height;
		Bitmap bitmap = CloneArgb32(image);
		Rectangle rect = new Rectangle(0, 0, width, height);
		BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		int stride;
		byte[] array;
		try
		{
			stride = bitmapData.Stride;
			int num = Math.Abs(stride) * height;
			array = new byte[num];
			Marshal.Copy(bitmapData.Scan0, array, 0, num);
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
		using FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write);
		using BinaryWriter binaryWriter = new BinaryWriter(output);
		binaryWriter.Write((byte)0);
		binaryWriter.Write((byte)0);
		binaryWriter.Write((byte)2);
		binaryWriter.Write((short)0);
		binaryWriter.Write((short)0);
		binaryWriter.Write((byte)0);
		binaryWriter.Write((short)0);
		binaryWriter.Write((short)0);
		binaryWriter.Write((short)width);
		binaryWriter.Write((short)height);
		binaryWriter.Write((byte)32);
		binaryWriter.Write((byte)40);
		byte[] array2 = new byte[width * 4];
		for (int i = 0; i < height; i++)
		{
			Buffer.BlockCopy(array, i * stride, array2, 0, width * 4);
			binaryWriter.Write(array2);
		}
	}
}
