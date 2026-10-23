using System;
using System.IO;

namespace GLMapPacker;

// Targa (.tga) decoder; Windows' own image library can't read TGA.
// Handles uncompressed and RLE images: true colour (15/16/24/32-bit), greyscale (8-bit) and
// colour-mapped (8-bit index), with any origin corner. Output is top-down BGRA, 8 bits per channel.
public static class TgaReader
{
	public static byte[] Decode(byte[] file, out int width, out int height)
	{
		if (file.Length < 18)
		{
			throw new InvalidDataException("Not a TGA file (too short).");
		}
		int idLength = file[0];
		int mapType = file[1];
		int imageType = file[2];
		int mapFirst = file[3] | (file[4] << 8);
		int mapLength = file[5] | (file[6] << 8);
		int mapBits = file[7];
		width = file[12] | (file[13] << 8);
		height = file[14] | (file[15] << 8);
		int bits = file[16];
		int descriptor = file[17];
		bool rle = imageType >= 9;
		int kind = imageType & 7;
		if (width <= 0 || height <= 0 || (kind != 1 && kind != 2 && kind != 3))
		{
			throw new InvalidDataException("Unsupported TGA image type " + imageType + ".");
		}
		int pos = 18 + idLength;
		byte[] palette = null;
		int mapBytes = (mapBits + 7) / 8;
		if (mapType == 1)
		{
			palette = new byte[(mapFirst + mapLength) * 4];
			for (int i = 0; i < mapLength; i++)
			{
				ReadColor(file, pos + i * mapBytes, mapBits, palette, (mapFirst + i) * 4);
			}
			pos += mapLength * mapBytes;
		}
		else if (kind == 1)
		{
			throw new InvalidDataException("Colour-mapped TGA without a colour map.");
		}
		int bpp = (bits + 7) / 8;
		int count = width * height;
		byte[] pixels = new byte[count * 4];
		int written = 0;
		byte[] px = new byte[4];
		while (written < count)
		{
			int run = 1;
			bool repeat = false;
			if (rle)
			{
				int header = file[pos++];
				run = (header & 0x7F) + 1;
				repeat = (header & 0x80) != 0;
			}
			for (int k = 0; k < run && written < count; k++)
			{
				if (k == 0 || !repeat)
				{
					if (pos + bpp > file.Length)
					{
						throw new InvalidDataException("TGA image data is truncated.");
					}
					Pixel(file, pos, kind, bits, palette, px);
					pos += bpp;
				}
				Buffer.BlockCopy(px, 0, pixels, written * 4, 4);
				written++;
			}
			if (!rle)
			{
				// Uncompressed: copy the rest of the image in one loop.
				while (written < count)
				{
					if (pos + bpp > file.Length)
					{
						throw new InvalidDataException("TGA image data is truncated.");
					}
					Pixel(file, pos, kind, bits, palette, px);
					pos += bpp;
					Buffer.BlockCopy(px, 0, pixels, written * 4, 4);
					written++;
				}
			}
		}
		// Bit 5 set: rows run top to bottom (otherwise bottom-up). Bit 4 set: columns right to left.
		bool topDown = (descriptor & 0x20) != 0;
		bool rightToLeft = (descriptor & 0x10) != 0;
		if (topDown && !rightToLeft)
		{
			return pixels;
		}
		byte[] outPx = new byte[pixels.Length];
		for (int y = 0; y < height; y++)
		{
			int sy = topDown ? y : height - 1 - y;
			for (int x = 0; x < width; x++)
			{
				int sx = rightToLeft ? width - 1 - x : x;
				Buffer.BlockCopy(pixels, (sy * width + sx) * 4, outPx, (y * width + x) * 4, 4);
			}
		}
		return outPx;
	}

	private static void Pixel(byte[] f, int p, int kind, int bits, byte[] palette, byte[] px)
	{
		if (kind == 1)
		{
			int index = bits == 16 ? f[p] | (f[p + 1] << 8) : f[p];
			int o = index * 4;
			if (palette == null || o + 3 >= palette.Length)
			{
				px[0] = px[1] = px[2] = 0;
				px[3] = byte.MaxValue;
				return;
			}
			Buffer.BlockCopy(palette, o, px, 0, 4);
			return;
		}
		if (kind == 3)
		{
			px[0] = px[1] = px[2] = f[p];
			px[3] = bits == 16 ? f[p + 1] : byte.MaxValue;
			return;
		}
		ReadColor(f, p, bits, px, 0);
	}

	// One BGR(A) colour as stored in true-colour pixels and colour maps.
	private static void ReadColor(byte[] f, int p, int bits, byte[] dst, int o)
	{
		switch (bits)
		{
		case 15:
		case 16:
		{
			int v = f[p] | (f[p + 1] << 8);
			dst[o] = (byte)((v & 0x1F) * 255 / 31);
			dst[o + 1] = (byte)(((v >> 5) & 0x1F) * 255 / 31);
			dst[o + 2] = (byte)(((v >> 10) & 0x1F) * 255 / 31);
			// The attribute bit is unreliable in 16-bit files, so treat them as opaque.
			dst[o + 3] = byte.MaxValue;
			break;
		}
		case 24:
			dst[o] = f[p];
			dst[o + 1] = f[p + 1];
			dst[o + 2] = f[p + 2];
			dst[o + 3] = byte.MaxValue;
			break;
		case 32:
			dst[o] = f[p];
			dst[o + 1] = f[p + 1];
			dst[o + 2] = f[p + 2];
			dst[o + 3] = f[p + 3];
			break;
		default:
			throw new InvalidDataException("Unsupported TGA pixel depth " + bits + ".");
		}
	}
}
