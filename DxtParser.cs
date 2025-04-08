using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace ResourceExtractor; 

[SupportedOSPlatform("windows")]
internal static class DxtParser {
	internal static Bitmap Parse(BinaryReader reader, ImageHeader header, bool ignoreAlpha = false) {
		var format = (header.Type == ImageType.DXT2 || header.Type == ImageType.DXT4) ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb;
		var result = new Bitmap(header.Width, header.Height, format);
		try {
			var raw = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, result.PixelFormat);

			var texelBlockCount = header.Width * header.Height / (4 * 4);
			unsafe {
				var buffer = (byte*) raw.Scan0;

				for (var texel = 0; texel < texelBlockCount; ++texel) {
					var texelBlock = ReadTexelBlock(reader, header.Type);
					var pixelOffsetX = 4 * (texel % (header.Width / 4));
					var pixelOffsetY = 4 * (texel / (header.Width / 4)) * (raw.Stride / 4);

					var index = 4 * (pixelOffsetX + pixelOffsetY);
					for (var y = 0; y < 4; ++y, index += raw.Stride - 16) {
						for (var x = 0; x < 4; ++x, index += 4) {
							var lookup = x + 4 * y;
							buffer[index + 0] = texelBlock[lookup].B;
							buffer[index + 1] = texelBlock[lookup].G;
							buffer[index + 2] = texelBlock[lookup].R;
							buffer[index + 3] = ignoreAlpha ? (byte) 255 : texelBlock[lookup].A;
						}
					}
				}
			}

			result.UnlockBits(raw);
		} catch {
			result.Dispose();
			throw;
		}

		return result;
	}

	private static Color[] ReadTexelBlock(BinaryReader reader, ImageType type) {
		var alphaBlock = 0ul;

		if (type == ImageType.DXT2 || type == ImageType.DXT3 || type == ImageType.DXT4 || type == ImageType.DXT5) {
			alphaBlock = reader.ReadUInt64();
		} else if (type != ImageType.DXT1) {
			throw new InvalidDataException(FormattableString.Invariant($"Format {type} not recognized."));
		}

		var c0 = reader.ReadUInt16();
		var c1 = reader.ReadUInt16();
		var colors = new Color[4];
		colors[0] = DecodeRGB565(c0);
		colors[1] = DecodeRGB565(c1);
		if (c0 > c1 || type != ImageType.DXT1) { // opaque, 4-color
			colors[2] = Color.FromArgb((2 * colors[0].R + colors[1].R + 1) / 3, (2 * colors[0].G + colors[1].G + 1) / 3, (2 * colors[0].B + colors[1].B + 1) / 3);
			colors[3] = Color.FromArgb((2 * colors[1].R + colors[0].R + 1) / 3, (2 * colors[1].G + colors[0].G + 1) / 3, (2 * colors[1].B + colors[0].B + 1) / 3);
		} else { // 1-bit alpha, 3-color
			colors[2] = Color.FromArgb((colors[0].R + colors[1].R) / 2, (colors[0].G + colors[1].G) / 2, (colors[0].B + colors[1].B) / 2);
			colors[3] = Color.Transparent;
		}
		var CompressedColor = reader.ReadUInt32();
		var DecodedColors = new Color[16];
		for (var i = 0; i < 16; ++i) {
			if (type == ImageType.DXT2 || type == ImageType.DXT3 || type == ImageType.DXT4 || type == ImageType.DXT5) {
				int a;
				if (type == ImageType.DXT2 || type == ImageType.DXT3) {
					// Seems to be 8 maximum; so treat 8 as 255 and all other values as 3-bit alpha
					a = (int) ((alphaBlock >> (4 * i)) & 0xF);
					if (a >= 8) {
						a = 0xFF;
					} else {
						a <<= 5;
					}
				} else { // Interpolated alpha
					var alphas = new int[8];
					alphas[0] = (byte) ((alphaBlock >> 0) & 0xFF);
					alphas[1] = (byte) ((alphaBlock >> 8) & 0xFF);
					if (alphas[0] > alphas[1]) {
						alphas[2] = (alphas[0] * 6 + alphas[1] * 1 + 3) / 7;
						alphas[3] = (alphas[0] * 5 + alphas[1] * 2 + 3) / 7;
						alphas[4] = (alphas[0] * 4 + alphas[1] * 3 + 3) / 7;
						alphas[5] = (alphas[0] * 3 + alphas[1] * 4 + 3) / 7;
						alphas[6] = (alphas[0] * 2 + alphas[1] * 5 + 3) / 7;
						alphas[7] = (alphas[0] * 1 + alphas[1] * 6 + 3) / 7;
					} else {
						alphas[2] = (alphas[0] * 4 + alphas[1] * 1 + 2) / 5;
						alphas[3] = (alphas[0] * 3 + alphas[1] * 2 + 2) / 5;
						alphas[4] = (alphas[0] * 2 + alphas[1] * 3 + 2) / 5;
						alphas[5] = (alphas[0] * 1 + alphas[1] * 4 + 2) / 5;
						alphas[6] = 0;
						alphas[7] = 255;
					}
					var alphaMatrix = (alphaBlock >> 16) & 0xFFFFFFFFFFFFL;
					a = alphas[(alphaMatrix >> (3 * i)) & 0x7];
				}
				DecodedColors[i] = Color.FromArgb(a, colors[CompressedColor & 0x3]);
			} else {
				DecodedColors[i] = colors[CompressedColor & 0x3];
			}
			CompressedColor >>= 2;
		}
		return DecodedColors;
	}

	private static Color DecodeRGB565(ushort c) =>
		Color.FromArgb((c & 0xF800) >> 8, (c & 0x07E0) >> 3, (c & 0x001F) << 3);
}
