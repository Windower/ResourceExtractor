using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ResourceExtractor; 

internal static class BitmapParser {
	internal static Bitmap Parse(BinaryReader reader, ImageHeader header, bool ignoreAlpha = false) {
		var Result = new Bitmap(header.Width, header.Height, PixelFormat.Format32bppArgb);
		try {
			var Raw = Result.LockBits(new Rectangle(0, 0, Result.Width, Result.Height), ImageLockMode.WriteOnly, Result.PixelFormat);

			var PixelCount = header.Height * header.Width;

			unsafe {
				var Buffer = (byte*) Raw.Scan0;

				Color[] Palette = null;
				byte[] BitFields = null;
				var EightCount = header.BitCount == 8;
				if (EightCount) { // 8-bit, with palette
					Palette = new Color[256];
					for (var i = 0; i < 256; ++i) {
						Palette[i] = ReadColor(reader, 32);
					}

					BitFields = reader.ReadBytes(PixelCount);
				}

				for (var Pixel = 0; Pixel < PixelCount; ++Pixel) {
					var Color = EightCount ? Palette[BitFields[Pixel]] : ReadColor(reader, header.BitCount);
					Buffer[4 * Pixel + 0] = Color.B;
					Buffer[4 * Pixel + 1] = Color.G;
					Buffer[4 * Pixel + 2] = Color.R;
					Buffer[4 * Pixel + 3] = ignoreAlpha ? (byte) 255 : Color.A;
				}
			}

			Result.UnlockBits(Raw);

			Result.RotateFlip(RotateFlipType.RotateNoneFlipY);
		} catch {
			Result.Dispose();
			throw;
		}

		return Result;
	}

	private static Color DecodeRGB565(ushort C) =>
		Color.FromArgb(((C & 0xF800) >> 11) * 0xFF / 0x1F, ((C & 0x07E0) >> 5) * 0xFF / 0x3F, (C & 0x001F) * 0xFF / 0x1F);

	public static Color ReadColor(BinaryReader reader, int bitDepth) {
		switch (bitDepth) {
			case 8:
				return reader.ReadByte().Let(gray => Color.FromArgb(gray, gray, gray));
			case 16:
				return DecodeRGB565(reader.ReadUInt16());
			case 24: {
					var b = reader.ReadByte();
					var g = reader.ReadByte();
					var r = reader.ReadByte();
					return Color.FromArgb(0xFF, r, g, b);
				}
			case 32: {
					var b = reader.ReadByte();
					var g = reader.ReadByte();
					var r = reader.ReadByte();
					var a = reader.ReadByte();
					return Color.FromArgb(a < 0x80 ? 2 * a : 0xFF, r, g, b);
				}
			default:
				return Color.HotPink;
		}
	}
}
