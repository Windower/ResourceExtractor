using System;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;

namespace ResourceExtractor; 

[SupportedOSPlatform("windows")]
internal static class ImageParser {
	internal static Bitmap Parse(Stream stream, bool ignoreAlpha = false) {
		var flag = stream.Read<byte>(0x30);
		var header = stream.Read<ImageHeader>(0x41);

		using var reader = new BinaryReader(stream);
		switch (header.Type) {
			case ImageType.DXT1:
			case ImageType.DXT2:
			case ImageType.DXT3:
			case ImageType.DXT4:
			case ImageType.DXT5:
				stream.Seek(8, SeekOrigin.Current); // Unknown 8 bytes
				return DxtParser.Parse(reader, header, ignoreAlpha);
			case ImageType.Bitmap:
				return BitmapParser.Parse(reader, header, ignoreAlpha);
			default:
				throw new InvalidDataException(FormattableString.Invariant($"Image type {header.Type} not supported: {flag}"));
		}
	}
}
