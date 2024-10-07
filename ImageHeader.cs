using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ResourceExtractor; 

public enum ImageType {
	Unknown,
	Bitmap,
	DXT1,
	DXT2,
	DXT3,
	DXT4,
	DXT5,
}

[SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Messes up struct layout")]
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly struct ImageHeader {
	private readonly uint structLength;
	private readonly int width;
	private readonly int height;
	private readonly ushort planes;
	private readonly ushort bitCount;
	private readonly uint compression;
	private readonly uint imageSize;
	private readonly uint horizontalResolution;
	private readonly uint verticalResolution;
	private readonly uint usedColors;
	private readonly uint importantColors;
	private readonly uint type;

	public uint StructLength => structLength;
	public int Width => width;
	public int Height => height;
	public ushort Planes => planes;
	public ushort BitCount => bitCount;
	public uint Compression => compression;
	public uint ImageSize => imageSize;
	public uint HorizontalResolution => horizontalResolution;
	public uint VerticalResolution => verticalResolution;
	public uint UsedColors => usedColors;
	public uint ImportantColors => importantColors;

	public ImageType Type =>
		type switch {
			0x44585430 + 1 => ImageType.DXT1,
			0x44585430 + 2 => ImageType.DXT2,
			0x44585430 + 3 => ImageType.DXT3,
			0x44585430 + 4 => ImageType.DXT4,
			0x44585430 + 5 => ImageType.DXT5,
			0x0000000A => ImageType.Bitmap,
			_ => ImageType.Unknown,
		};
}
